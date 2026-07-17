using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DCView.Hackathon.Application.DTOs.Hackathon;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Shared.Helpers;

namespace DCView.Hackathon.Application.Services;

public class HackathonService : IHackathonService
{
    private readonly ISessionRepository _sessionRepo;
    private readonly IHackathonConfigRepository _configRepo;
    private readonly IExecutionHistoryRepository _historyRepo;
    private readonly IUserRepository _userRepo;
    private readonly IScaffoldScriptRepository _scaffoldRepo;
    private readonly IFileRepository _fileRepo;
    private readonly IFolderRepository _folderRepo;
    private readonly IConfiguration _config;

    public HackathonService(
        ISessionRepository sessionRepo,
        IHackathonConfigRepository configRepo,
        IExecutionHistoryRepository historyRepo,
        IUserRepository userRepo,
        IScaffoldScriptRepository scaffoldRepo,
        IFileRepository fileRepo,
        IFolderRepository folderRepo,
        IConfiguration config)
    {
        _sessionRepo = sessionRepo;
        _configRepo = configRepo;
        _historyRepo = historyRepo;
        _userRepo = userRepo;
        _scaffoldRepo = scaffoldRepo;
        _fileRepo = fileRepo;
        _folderRepo = folderRepo;
        _config = config;
    }

    public async Task<SessionStatusDto> GetSessionStatusAsync(int userId)
    {
        var session = await _sessionRepo.GetByUserIdAsync(userId);
        if (session == null)
            return new SessionStatusDto { IsActive = false, IsExpired = false, DatabaseCreated = false };

        bool isExpired = session.ExpiresAt.HasValue && session.ExpiresAt < DateTimeHelper.Now;

        int? remaining = null;
        if (session.ExpiresAt.HasValue)
        {
            remaining = Math.Max(0, (int)(session.ExpiresAt.Value - DateTimeHelper.Now).TotalMinutes);
        }

        return new SessionStatusDto
        {
            IsActive = session.IsActive && !isExpired,
            IsExpired = session.IsActive && isExpired,
            DatabaseCreated = session.DatabaseCreated,
            IsSubmitted = session.IsSubmitted,
            SubmittedAt = session.SubmittedAt,
            DatabaseName = session.DatabaseName,
            ExpiresAt = session.ExpiresAt,
            RemainingMinutes = remaining
        };
    }

    public async Task<CreateDatabaseResultDto> CreateDatabaseAsync(int userId)
    {
        var session = await _sessionRepo.GetByUserIdAsync(userId);
        if (session == null || !session.IsActive)
            throw new InvalidOperationException("Session is not active.");

        if (session.DatabaseCreated)
            throw new InvalidOperationException("Database already created.");

        var hackConfig = await _configRepo.GetActiveConfigAsync()
            ?? throw new InvalidOperationException("Hackathon server is not configured.");

        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        string encKey = _config["Encryption:Key"]!;
        string adminPassword = EncryptionHelper.Decrypt(hackConfig.AdminPasswordEncrypted, encKey);
        string dbName = $"{hackConfig.DbPrefix}{user.UserID}";
        string loginName = $"hack_{user.UserID}";
        string loginPassword = GenerateRandomPassword();

        string adminConnStr = $"Server={hackConfig.ServerName};Database=master;User Id={hackConfig.AdminUserId};Password={adminPassword};TrustServerCertificate=True;Connection Timeout=30;";

        using var conn = new SqlConnection(adminConnStr);
        await conn.OpenAsync();

        // Create database
        using var createDbCmd = conn.CreateCommand();
        createDbCmd.CommandText = $"CREATE DATABASE [{dbName}]";
        createDbCmd.CommandTimeout = 60;
        await createDbCmd.ExecuteNonQueryAsync();

        // Create login
        using var createLoginCmd = conn.CreateCommand();
        createLoginCmd.CommandText = $"CREATE LOGIN [{loginName}] WITH PASSWORD = '{loginPassword.Replace("'", "''")}', DEFAULT_DATABASE = [{dbName}]";
        await createLoginCmd.ExecuteNonQueryAsync();

        // Switch to user DB and create user with db_owner
        using var userDbConn = new SqlConnection($"Server={hackConfig.ServerName};Database={dbName};User Id={hackConfig.AdminUserId};Password={adminPassword};TrustServerCertificate=True;Connection Timeout=30;");
        await userDbConn.OpenAsync();

        using var createUserCmd = userDbConn.CreateCommand();
        createUserCmd.CommandText = $@"
            CREATE USER [{loginName}] FOR LOGIN [{loginName}];
            ALTER ROLE db_owner ADD MEMBER [{loginName}];";
        await createUserCmd.ExecuteNonQueryAsync();

        // Update session
        session.DatabaseCreated = true;
        session.DatabaseName = dbName;
        session.DbLoginPassword = EncryptionHelper.Encrypt(loginPassword, encKey);
        await _sessionRepo.UpdateAsync(session);

        // ─── Execute Scaffold Scripts & Copy to File Manager ─────────
        var scaffoldScripts = (await _scaffoldRepo.GetAllActiveAsync()).ToList();
        if (scaffoldScripts.Count > 0)
        {
            // Execute scripts against the new DB using admin connection
            using var scaffoldConn = new SqlConnection($"Server={hackConfig.ServerName};Database={dbName};User Id={hackConfig.AdminUserId};Password={adminPassword};TrustServerCertificate=True;Connection Timeout=60;");
            await scaffoldConn.OpenAsync();

            foreach (var script in scaffoldScripts)
            {
                try
                {
                    var batches = SqlBatchParser.SplitBatches(script.SqlContent);
                    foreach (var batch in batches)
                    {
                        using var cmd = scaffoldConn.CreateCommand();
                        cmd.CommandText = batch;
                        cmd.CommandTimeout = 60;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch
                {
                    // Best-effort: continue with next script if one fails
                }
            }

            // Create "Starter Scripts" folder in participant's file manager
            var folder = new UserFolder
            {
                UserId = userId,
                FolderName = "Starter Scripts",
                ParentFolderId = null,
                CreatedDate = DateTimeHelper.Now
            };
            await _folderRepo.CreateAsync(folder);

            // Copy each scaffold script as a file in the folder
            foreach (var script in scaffoldScripts)
            {
                var file = new UserFile
                {
                    UserId = userId,
                    FolderId = folder.FolderId,
                    FileName = script.FileName,
                    FileType = "Script",
                    Content = script.SqlContent,
                    CreatedDate = DateTimeHelper.Now
                };
                await _fileRepo.CreateAsync(file);
            }
        }

        return new CreateDatabaseResultDto
        {
            DatabaseName = dbName,
            Message = $"Database '{dbName}' created successfully. You can now start working."
        };
    }

    public async Task<ExecuteResultDto> ExecuteAsync(int userId, ExecuteRequestDto request)
    {
        var session = await _sessionRepo.GetByUserIdAsync(userId);
        if (session == null || !session.IsActive || !session.DatabaseCreated)
            throw new InvalidOperationException("Session is not active or database not created.");

        if (session.ExpiresAt.HasValue && session.ExpiresAt < DateTimeHelper.Now)
            throw new InvalidOperationException("Session has expired.");

        var hackConfig = await _configRepo.GetActiveConfigAsync()
            ?? throw new InvalidOperationException("Hackathon server not configured.");

        string encKey = _config["Encryption:Key"]!;
        string loginPassword = EncryptionHelper.Decrypt(session.DbLoginPassword!, encKey);
        var user = await _userRepo.GetByIdAsync(userId);
        string loginName = $"hack_{user!.UserID}";

        string connStr = $"Server={hackConfig.ServerName};Database={session.DatabaseName};User Id={loginName};Password={loginPassword};TrustServerCertificate=True;Connection Timeout={hackConfig.MaxQueryTimeoutSeconds};";

        var batches = SqlBatchParser.SplitBatches(request.Sql);
        var result = new ExecuteResultDto
        {
            TotalBatches = batches.Count,
            ExecutedBatches = 0
        };

        // Use a SINGLE connection for all batches (like SSMS) so SET options persist across GO batches
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];

            // ─── SQL Guardrail: check for blocked patterns ───────────
            var violation = SqlBatchParser.ValidateBatchSafety(batch);
            if (violation != null)
            {
                result.Results.Add(new BatchResultDto
                {
                    BatchIndex = i,
                    Type = "ERROR",
                    Error = $"Blocked: {violation}"
                });
                result.ExecutedBatches++;

                await _historyRepo.CreateAsync(new ExecutionHistory
                {
                    UserId = userId,
                    DatabaseName = session.DatabaseName,
                    QueryText = batch.Length > 4000 ? batch[..4000] : batch,
                    QueryType = SqlBatchParser.DetectQueryType(batch),
                    Status = "Blocked",
                    ErrorMessage = violation,
                    DurationMs = 0,
                    ExecutedAt = DateTimeHelper.Now
                });
                break; // Stop execution on blocked batch
            }

            var queryType = SqlBatchParser.DetectQueryType(batch);
            var sw = Stopwatch.StartNew();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = batch;
                cmd.CommandTimeout = hackConfig.MaxQueryTimeoutSeconds;

                if (SqlBatchParser.IsSelectBatch(batch))
                {
                    // Execute as reader with pagination
                    var batchResult = await ExecuteSelectBatchAsync(cmd, request.Page, request.PageSize);
                    sw.Stop();
                    batchResult.BatchIndex = i;
                    batchResult.DurationMs = (int)sw.ElapsedMilliseconds;
                    result.Results.Add(batchResult);
                }
                else
                {
                    int affected = await cmd.ExecuteNonQueryAsync();
                    sw.Stop();
                    result.Results.Add(new BatchResultDto
                    {
                        BatchIndex = i,
                        Type = queryType == "SELECT" ? "DML" : (queryType is "INSERT" or "UPDATE" or "DELETE" ? "DML" : "DDL"),
                        Message = affected >= 0 ? $"{affected} row(s) affected" : "Command completed successfully",
                        RowsAffected = affected >= 0 ? affected : null,
                        DurationMs = (int)sw.ElapsedMilliseconds
                    });
                }

                result.ExecutedBatches++;

                // Log history
                await _historyRepo.CreateAsync(new ExecutionHistory
                {
                    UserId = userId,
                    DatabaseName = session.DatabaseName,
                    QueryText = batch.Length > 4000 ? batch[..4000] : batch,
                    QueryType = queryType,
                    Status = "Success",
                    RowsAffected = result.Results.Last().RowsAffected,
                    DurationMs = (int)sw.ElapsedMilliseconds,
                    ExecutedAt = DateTimeHelper.Now
                });
            }
            catch (SqlException ex) when (ex.Message.Contains("Timeout"))
            {
                sw.Stop();
                result.Results.Add(new BatchResultDto
                {
                    BatchIndex = i,
                    Type = "ERROR",
                    Error = "Query timed out (30 second limit exceeded)",
                    DurationMs = (int)sw.ElapsedMilliseconds
                });
                result.ExecutedBatches++;

                await _historyRepo.CreateAsync(new ExecutionHistory
                {
                    UserId = userId,
                    DatabaseName = session.DatabaseName,
                    QueryText = batch.Length > 4000 ? batch[..4000] : batch,
                    QueryType = queryType,
                    Status = "Timeout",
                    ErrorMessage = "Query timed out",
                    DurationMs = (int)sw.ElapsedMilliseconds,
                    ExecutedAt = DateTimeHelper.Now
                });
                break;
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.Results.Add(new BatchResultDto
                {
                    BatchIndex = i,
                    Type = "ERROR",
                    Error = ex.Message,
                    DurationMs = (int)sw.ElapsedMilliseconds
                });
                result.ExecutedBatches++;

                await _historyRepo.CreateAsync(new ExecutionHistory
                {
                    UserId = userId,
                    DatabaseName = session.DatabaseName,
                    QueryText = batch.Length > 4000 ? batch[..4000] : batch,
                    QueryType = queryType,
                    Status = "Failed",
                    ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message,
                    DurationMs = (int)sw.ElapsedMilliseconds,
                    ExecutedAt = DateTimeHelper.Now
                });
                break; // Stop on error
            }
        }

        return result;
    }

    private static async Task<BatchResultDto> ExecuteSelectBatchAsync(SqlCommand cmd, int page, int pageSize)
    {
        using var reader = await cmd.ExecuteReaderAsync();

        var columns = new List<string>();
        for (int c = 0; c < reader.FieldCount; c++)
            columns.Add(reader.GetName(c));

        var allRows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int c = 0; c < reader.FieldCount; c++)
            {
                row[columns[c]] = reader.IsDBNull(c) ? null : reader.GetValue(c);
            }
            allRows.Add(row);
        }

        int totalRows = allRows.Count;
        var pagedRows = allRows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new BatchResultDto
        {
            Type = "SELECT",
            Columns = columns,
            Rows = pagedRows,
            TotalRows = totalRows,
            Page = page,
            PageSize = pageSize,
            Message = $"{totalRows} row(s) returned"
        };
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Range(0, 20).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}

