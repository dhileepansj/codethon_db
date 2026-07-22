using DCView.Hackathon.Shared.Helpers;
using DCView.Hackathon.Application.DTOs.Admin;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;
using DCView.Hackathon.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;

namespace DCView.Hackathon.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly IHackathonConfigRepository _configRepo;
    private readonly IFileRepository _fileRepo;
    private readonly IFolderRepository _folderRepo;
    private readonly IExecutionHistoryRepository _historyRepo;
    private readonly IConfiguration _config;
    private readonly DbContext _dbContext;

    public UserService(
        IUserRepository userRepo,
        ISessionRepository sessionRepo,
        IHackathonConfigRepository configRepo,
        IFileRepository fileRepo,
        IFolderRepository folderRepo,
        IExecutionHistoryRepository historyRepo,
        IConfiguration config,
        DbContext dbContext)
    {
        _userRepo = userRepo;
        _sessionRepo = sessionRepo;
        _configRepo = configRepo;
        _fileRepo = fileRepo;
        _folderRepo = folderRepo;
        _historyRepo = historyRepo;
        _config = config;
        _dbContext = dbContext;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto request, string createdBy)
    {
        if (await _userRepo.ExistsAsync(request.UserID))
            throw new InvalidOperationException($"User '{request.UserID}' already exists.");

        var user = new User
        {
            UserID = request.UserID,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            FullName = request.FullName,
            Email = request.Email,
            Role = "Participant",
            IsActive = true,
            DbEnginePreference = ParseEnginePreference(request.DbEnginePreference),
            AssessmentId = request.AssessmentId,
            CreatedBy = createdBy,
            CreatedDate = DateTimeHelper.Now
        };

        await _userRepo.CreateAsync(user);
        return MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> BulkCreateUsersAsync(IEnumerable<CreateUserDto> requests, string createdBy)
    {
        var results = new List<UserDto>();
        foreach (var req in requests)
        {
            try
            {
                var dto = await CreateUserAsync(req, createdBy);
                results.Add(dto);
            }
            catch
            {
                // Skip duplicates in bulk creation
            }
        }
        return results;
    }

    public async Task<IEnumerable<UserDto>> GetAllParticipantsAsync()
    {
        var users = await _userRepo.GetAllParticipantsAsync();
        var userIds = users.Select(u => u.Id).ToList();

        // Bulk load MCQ test data for all participants
        var mcqTests = await _dbContext.Set<McqTest>()
            .Where(t => userIds.Contains(t.UserId))
            .GroupBy(t => t.UserId)
            .Select(g => g.OrderByDescending(t => t.StartedAt).First())
            .ToListAsync();

        var mcqTestMap = mcqTests.ToDictionary(t => t.UserId);

        return users.Select(u => MapToDto(u, mcqTestMap.GetValueOrDefault(u.Id)));
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return null;

        var mcqTest = await _dbContext.Set<McqTest>()
            .Where(t => t.UserId == user.Id)
            .OrderByDescending(t => t.StartedAt)
            .FirstOrDefaultAsync();

        return MapToDto(user, mcqTest);
    }

    public async Task<bool> UpdateUserAsync(string userId, UpdateUserDto request, string modifiedBy)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return false;

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Email != null) user.Email = request.Email;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.DbEnginePreference != null) user.DbEnginePreference = ParseEnginePreference(request.DbEnginePreference);
        user.ModifiedBy = modifiedBy;
        user.ModifiedDate = DateTimeHelper.Now;

        await _userRepo.UpdateAsync(user);
        return true;
    }

    public async Task<bool> DeactivateUserAsync(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return false;

        user.IsActive = false;
        user.ModifiedDate = DateTimeHelper.Now;
        await _userRepo.UpdateAsync(user);
        return true;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return false;

        var session = await _sessionRepo.GetByUserIdAsync(user.Id);

        // ─── 1. Drop external database / schema if created ───────────
        if (session != null && session.DatabaseCreated && !string.IsNullOrEmpty(session.DatabaseName))
        {
            // Resolve the server config — try by user's engine preference, fall back to any active config
            var hackConfig = await _configRepo.GetActiveConfigAsync(user.DbEnginePreference)
                          ?? await _configRepo.GetActiveConfigAsync();

            if (hackConfig == null)
                throw new InvalidOperationException(
                    $"Cannot delete user '{userId}': no server configuration found to drop their database '{session.DatabaseName}'. " +
                    "Please configure the server first, or manually drop the database.");

            string encKey = _config["Encryption:Key"]!;
            string adminPassword = EncryptionHelper.Decrypt(hackConfig.AdminPasswordEncrypted, encKey);
            string dbName = session.DatabaseName!;

            if (hackConfig.DbEngineType == DbEngineType.Oracle)
            {
                int port = hackConfig.Port ?? 1521;
                string serviceName = hackConfig.OracleServiceName ?? "XEPDB1";
                string oraConnStr = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={hackConfig.ServerName})(PORT={port}))(CONNECT_DATA=(SERVICE_NAME={serviceName})));User Id=\"{hackConfig.AdminUserId}\";Password=\"{adminPassword}\";Connection Timeout=30;";

                using var oraConn = new OracleConnection(oraConnStr);
                await oraConn.OpenAsync();

                // Kill active sessions
                try
                {
                    using var killCmd = oraConn.CreateCommand();
                    killCmd.CommandText = $@"
                        BEGIN
                            FOR s IN (SELECT SID, SERIAL# FROM V$SESSION WHERE USERNAME = '{dbName}')
                            LOOP
                                EXECUTE IMMEDIATE 'ALTER SYSTEM KILL SESSION ''' || s.SID || ',' || s.SERIAL# || ''' IMMEDIATE';
                            END LOOP;
                        END;";
                    await killCmd.ExecuteNonQueryAsync();
                }
                catch { /* Best effort */ }

                using var dropCmd = oraConn.CreateCommand();
                dropCmd.CommandText = $"DROP USER \"{dbName}\" CASCADE";
                dropCmd.CommandTimeout = 60;
                await dropCmd.ExecuteNonQueryAsync();
            }
            else
            {
                string loginName = $"hack_{user.UserID}";
                string adminConnStr = $"Server={hackConfig.ServerName};Database=master;User Id={hackConfig.AdminUserId};Password={adminPassword};TrustServerCertificate=True;Connection Timeout=30;";

                using var conn = new SqlConnection(adminConnStr);
                await conn.OpenAsync();

                // Force close all connections to the target DB before dropping
                using var killCmd = conn.CreateCommand();
                killCmd.CommandText = $@"
                    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{dbName}')
                    BEGIN
                        ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    END";
                killCmd.CommandTimeout = 30;
                await killCmd.ExecuteNonQueryAsync();

                using var dropCmd = conn.CreateCommand();
                dropCmd.CommandText = $"DROP DATABASE [{dbName}]";
                dropCmd.CommandTimeout = 60;
                await dropCmd.ExecuteNonQueryAsync();

                using var dropLoginCmd = conn.CreateCommand();
                dropLoginCmd.CommandText = $@"
                    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{loginName}')
                        DROP LOGIN [{loginName}];";
                await dropLoginCmd.ExecuteNonQueryAsync();
            }
        }

        // ─── 2. Delete all related records from PostgreSQL ───────────
        // Delete tab switch logs, AI detection logs, blocked saves, submission files, password logs
        await _dbContext.Set<TabSwitchLog>().Where(t => t.UserId == user.Id).ExecuteDeleteAsync();
        await _dbContext.Set<AiDetectionLog>().Where(a => a.UserId == user.Id).ExecuteDeleteAsync();
        await _dbContext.Set<AiBlockedSave>().Where(a => a.UserId == user.Id).ExecuteDeleteAsync();
        await _dbContext.Set<UserSubmissionFile>().Where(s => s.UserId == user.Id).ExecuteDeleteAsync();
        await _dbContext.Set<PasswordChangeLog>().Where(p => p.UserId == user.Id).ExecuteDeleteAsync();

        // Delete execution history
        await _historyRepo.DeleteByUserIdAsync(user.Id);

        // Delete files and folders
        var files = await _fileRepo.GetAllByUserIdAsync(user.Id);
        foreach (var file in files)
            await _fileRepo.DeleteAsync(file.FileId);

        var folders = await _folderRepo.GetAllByUserIdAsync(user.Id);
        foreach (var folder in folders)
            await _folderRepo.DeleteCascadeAsync(folder.FolderId);

        // Delete session
        if (session != null)
            await _sessionRepo.DeleteAsync(session);

        // ─── 3. Delete the user record ───────────────────────────────
        await _userRepo.DeleteAsync(user);

        return true;
    }

    private static UserDto MapToDto(User user, McqTest? mcqTest = null)
    {
        var dto = new UserDto
        {
            Id = user.Id,
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            PasswordResetRequested = user.PasswordResetRequested,
            CreatedDate = user.CreatedDate,
            LastLoginAt = user.LastLoginAt,
            LoginCount = user.LoginCount,
            DbEnginePreference = user.DbEnginePreference.ToString(),
            AssessmentType = user.Assessment?.Type ?? "SQL",
            AssessmentTitle = user.Assessment?.Title,
            AssessmentSubType = user.Assessment?.SubType ?? user.DbEnginePreference.ToString(),
            AssessmentId = user.AssessmentId,
            Session = user.Session != null ? new SessionSummaryDto
            {
                IsActive = user.Session.IsActive && (user.Session.ExpiresAt == null || user.Session.ExpiresAt > DateTimeHelper.Now),
                IsExpired = user.Session.IsActive && user.Session.ExpiresAt.HasValue && user.Session.ExpiresAt < DateTimeHelper.Now,
                DatabaseCreated = user.Session.DatabaseCreated,
                DatabaseName = user.Session.DatabaseName,
                StartedAt = user.Session.StartedAt,
                ExpiresAt = user.Session.ExpiresAt
            } : null
        };

        // MCQ progress
        if (user.Assessment?.Type == "MCQ" && mcqTest != null)
        {
            dto.McqProgress = new McqProgressDto
            {
                Status = mcqTest.IsSubmitted ? "Submitted" : mcqTest.IsInProgress ? "InProgress" : "NotStarted",
                TotalQuestions = mcqTest.TotalQuestions,
                Answered = mcqTest.Attempted,
                Score = mcqTest.IsSubmitted ? mcqTest.Score : null,
                MaxScore = mcqTest.IsSubmitted ? mcqTest.MaxScore : null,
                Percentage = mcqTest.IsSubmitted ? mcqTest.Percentage : null,
                Passed = mcqTest.Passed,
                SubmittedAt = mcqTest.SubmittedAt
            };
        }
        else if (user.Assessment?.Type == "MCQ")
        {
            dto.McqProgress = new McqProgressDto { Status = "NotStarted" };
        }

        return dto;
    }

    private static DbEngineType ParseEnginePreference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DbEngineType.SqlServer;
        return Enum.TryParse<DbEngineType>(value, ignoreCase: true, out var result) ? result : DbEngineType.SqlServer;
    }
}

