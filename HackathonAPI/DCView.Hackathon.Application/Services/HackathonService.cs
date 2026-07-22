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
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IDbEngineFactory _engineFactory;
    private readonly IConfiguration _config;

    public HackathonService(
        ISessionRepository sessionRepo,
        IHackathonConfigRepository configRepo,
        IExecutionHistoryRepository historyRepo,
        IUserRepository userRepo,
        IScaffoldScriptRepository scaffoldRepo,
        IFileRepository fileRepo,
        IFolderRepository folderRepo,
        IScheduleRepository scheduleRepo,
        IDbEngineFactory engineFactory,
        IConfiguration config)
    {
        _sessionRepo = sessionRepo;
        _configRepo = configRepo;
        _historyRepo = historyRepo;
        _userRepo = userRepo;
        _scaffoldRepo = scaffoldRepo;
        _fileRepo = fileRepo;
        _folderRepo = folderRepo;
        _scheduleRepo = scheduleRepo;
        _engineFactory = engineFactory;
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

        // Get schedule info
        ScheduleInfoDto? scheduleInfo = null;
        try
        {
            var schedule = await _scheduleRepo.GetActiveScheduleAsync();
            if (schedule != null)
            {
                var now = DateTimeHelper.Now;

                // Only apply schedule if no date is set (applies every day) or date matches today
                var scheduleApplies = !schedule.ScheduleDate.HasValue
                    || schedule.ScheduleDate.Value.Date == now.Date;

                // If date is set but doesn't match today — block
                if (schedule.ScheduleDate.HasValue && schedule.ScheduleDate.Value.Date != now.Date)
                {
                    scheduleInfo = new ScheduleInfoDto
                    {
                        SessionStartTime = schedule.SessionStartTime,
                        SessionEndTime = schedule.SessionEndTime,
                        ExtensionMinutes = schedule.ExtensionMinutes,
                        IsWrongDate = true,
                        ScheduleDate = schedule.ScheduleDate.Value.ToString("yyyy-MM-dd"),
                        Alerts = ParseAlertConfig(schedule.AlertConfig),
                        Breaks = schedule.Breaks.Select(b => new BreakInfoDto { Title = b.Title, StartTime = b.StartTime, EndTime = b.EndTime }).ToList()
                    };
                }
                else if (scheduleApplies)
                {
                var currentTime = now.ToString("HH:mm");

                // Calculate effective end time with extension
                var endTimeParts = schedule.SessionEndTime.Split(':');
                var effectiveEnd = new DateTime(now.Year, now.Month, now.Day,
                    int.Parse(endTimeParts[0]), int.Parse(endTimeParts[1]), 0)
                    .AddMinutes(schedule.ExtensionMinutes);

                // Check if before start time
                bool isBeforeStart = string.Compare(currentTime, schedule.SessionStartTime) < 0;

                // Check if after end time
                bool isAfterEnd = now > effectiveEnd;

                // If individual session has a later ExpiresAt (admin extended), respect that
                if (isAfterEnd && session.ExpiresAt.HasValue && session.ExpiresAt.Value > now)
                {
                    isAfterEnd = false;
                }

                // Check if currently in a break
                bool isInBreak = false;
                string? currentBreakTitle = null;
                string? breakEndsAt = null;

                if (!isBeforeStart && !isAfterEnd)
                {
                    foreach (var b in schedule.Breaks)
                    {
                        if (string.Compare(currentTime, b.StartTime) >= 0 && string.Compare(currentTime, b.EndTime) < 0)
                        {
                            isInBreak = true;
                            currentBreakTitle = b.Title;
                            breakEndsAt = b.EndTime;
                            break;
                        }
                    }
                }

                // Check if session has ended based on schedule
                if (isAfterEnd && session.IsActive)
                {
                    isExpired = true;
                    remaining = 0;
                }
                else if (!isExpired && !isBeforeStart)
                {
                    // Use the later of schedule end or session ExpiresAt for remaining calculation
                    var effectiveEndForRemaining = effectiveEnd;
                    if (session.ExpiresAt.HasValue && session.ExpiresAt.Value > effectiveEnd)
                        effectiveEndForRemaining = session.ExpiresAt.Value;

                    var scheduleRemaining = (int)(effectiveEndForRemaining - now).TotalMinutes;
                    // Subtract remaining break time
                    foreach (var b in schedule.Breaks)
                    {
                        if (string.Compare(currentTime, b.StartTime) < 0)
                        {
                            var bStart = TimeSpan.Parse(b.StartTime);
                            var bEnd = TimeSpan.Parse(b.EndTime);
                            scheduleRemaining -= (int)(bEnd - bStart).TotalMinutes;
                        }
                    }
                    if (remaining == null || scheduleRemaining < remaining)
                        remaining = Math.Max(0, scheduleRemaining);
                }

                scheduleInfo = new ScheduleInfoDto
                {
                    SessionStartTime = schedule.SessionStartTime,
                    SessionEndTime = effectiveEnd.ToString("HH:mm"),
                    ExtensionMinutes = schedule.ExtensionMinutes,
                    IsInBreak = isInBreak,
                    IsBeforeStart = isBeforeStart,
                    IsAfterEnd = isAfterEnd,
                    CurrentBreakTitle = currentBreakTitle,
                    BreakEndsAt = breakEndsAt,
                    ScheduleDate = schedule.ScheduleDate?.ToString("yyyy-MM-dd"),
                    Alerts = ParseAlertConfig(schedule.AlertConfig),
                    Breaks = schedule.Breaks.Select(b => new BreakInfoDto
                    {
                        Title = b.Title,
                        StartTime = b.StartTime,
                        EndTime = b.EndTime
                    }).ToList()
                };
                }
            }
        }
        catch { /* Schedule not configured — ignore */ }

        return new SessionStatusDto
        {
            IsActive = session.IsActive && !isExpired,
            IsExpired = session.IsActive && isExpired,
            DatabaseCreated = session.DatabaseCreated,
            IsSubmitted = session.IsSubmitted,
            SubmittedAt = session.SubmittedAt,
            DatabaseName = session.DatabaseName,
            ExpiresAt = session.ExpiresAt,
            RemainingMinutes = remaining,
            Schedule = scheduleInfo
        };
    }

    public async Task<CreateDatabaseResultDto> CreateDatabaseAsync(int userId)
    {
        var session = await _sessionRepo.GetByUserIdAsync(userId);
        if (session == null || !session.IsActive)
            throw new InvalidOperationException("Session is not active.");

        if (session.DatabaseCreated)
            throw new InvalidOperationException("Database already created.");

        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        // Resolve config based on the user's engine preference (fallback to any active config)
        var hackConfig = await _configRepo.GetActiveConfigAsync(user.DbEnginePreference)
            ?? await _configRepo.GetActiveConfigAsync()
            ?? throw new InvalidOperationException($"No {user.DbEnginePreference} server is configured. Please contact the administrator.");

        string encKey = _config["Encryption:Key"]!;
        string adminPassword = EncryptionHelper.Decrypt(hackConfig.AdminPasswordEncrypted, encKey);

        // Use the configured DB engine
        var engine = _engineFactory.GetEngine(hackConfig.DbEngineType);
        var dbResult = await engine.CreateParticipantDatabaseAsync(hackConfig, user.UserID, adminPassword, encKey);

        // Update session
        session.DatabaseCreated = true;
        session.DatabaseName = dbResult.DatabaseName;
        session.DbLoginPassword = EncryptionHelper.Encrypt(dbResult.LoginPassword, encKey);
        await _sessionRepo.UpdateAsync(session);

        // ─── Execute Scaffold Scripts & Copy to File Manager ─────────
        var scaffoldScripts = (await _scaffoldRepo.GetAllActiveAsync()).ToList();
        if (scaffoldScripts.Count > 0)
        {
            await engine.ExecuteScaffoldScriptsAsync(hackConfig, dbResult.DatabaseName, adminPassword, scaffoldScripts);

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
            DatabaseName = dbResult.DatabaseName,
            Message = $"Database '{dbResult.DatabaseName}' created successfully. You can now start working."
        };
    }

    public async Task<ExecuteResultDto> ExecuteAsync(int userId, ExecuteRequestDto request)
    {
        var session = await _sessionRepo.GetByUserIdAsync(userId);
        if (session == null || !session.IsActive || !session.DatabaseCreated)
            throw new InvalidOperationException("Session is not active or database not created.");

        if (session.ExpiresAt.HasValue && session.ExpiresAt < DateTimeHelper.Now)
            throw new InvalidOperationException("Session has expired.");

        var user = await _userRepo.GetByIdAsync(userId);
        var hackConfig = await _configRepo.GetActiveConfigAsync(user!.DbEnginePreference)
            ?? await _configRepo.GetActiveConfigAsync()
            ?? throw new InvalidOperationException("Hackathon server not configured.");

        string encKey = _config["Encryption:Key"]!;
        string loginPassword = EncryptionHelper.Decrypt(session.DbLoginPassword!, encKey);

        // Use the configured DB engine
        var engine = _engineFactory.GetEngine(hackConfig.DbEngineType);
        var result = await engine.ExecuteAsync(hackConfig, session, user!.UserID, loginPassword, request);

        // Log execution history for each batch result
        var batches = engine.SplitBatches(request.Sql);
        for (int i = 0; i < result.Results.Count; i++)
        {
            var batchResult = result.Results[i];
            var batchText = i < batches.Count ? batches[batchResult.BatchIndex] : request.Sql;

            await _historyRepo.CreateAsync(new ExecutionHistory
            {
                UserId = userId,
                DatabaseName = session.DatabaseName,
                QueryText = batchText.Length > 4000 ? batchText[..4000] : batchText,
                QueryType = batchResult.Type == "ERROR" ? "ERROR" : batchResult.Type,
                Status = batchResult.Type == "ERROR"
                    ? (batchResult.Error?.Contains("Blocked") == true ? "Blocked" :
                       batchResult.Error?.Contains("timed out") == true ? "Timeout" : "Failed")
                    : "Success",
                ErrorMessage = batchResult.Error != null && batchResult.Error.Length > 2000
                    ? batchResult.Error[..2000] : batchResult.Error,
                RowsAffected = batchResult.RowsAffected,
                DurationMs = batchResult.DurationMs,
                ExecutedAt = DateTimeHelper.Now
            });
        }

        return result;
    }

    private static List<AlertConfigItem> ParseAlertConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<AlertConfigItem>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<AlertConfigItem>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch
        {
            return new List<AlertConfigItem>();
        }
    }
}


