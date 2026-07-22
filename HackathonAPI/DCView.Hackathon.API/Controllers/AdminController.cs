using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Application.DTOs.Admin;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;
using DCView.Hackathon.Shared.Helpers;

namespace DCView.Hackathon.API.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ISessionService _sessionService;
    private readonly IExportService _exportService;
    private readonly IFileManagerService _fileService;
    private readonly IAiDetectionService _aiDetectionService;
    private readonly IHackathonConfigRepository _configRepo;
    private readonly IConfiguration _configuration;

    public AdminController(
        IUserService userService,
        ISessionService sessionService,
        IExportService exportService,
        IFileManagerService fileService,
        IAiDetectionService aiDetectionService,
        IHackathonConfigRepository configRepo,
        IConfiguration configuration)
    {
        _userService = userService;
        _sessionService = sessionService;
        _exportService = exportService;
        _fileService = fileService;
        _aiDetectionService = aiDetectionService;
        _configRepo = configRepo;
        _configuration = configuration;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var stats = await _sessionService.GetDashboardStatsAsync();
        return Ok(stats);
    }

    // ─── User Management ─────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllParticipantsAsync();
        return Ok(users);
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserID) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "UserID and Password are required" });

        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        var user = await _userService.CreateUserAsync(request, createdBy);
        return CreatedAtAction(nameof(GetUser), new { userId = user.UserID }, user);
    }

    [HttpPost("users/bulk")]
    public async Task<IActionResult> BulkCreateUsers([FromBody] List<CreateUserDto> requests)
    {
        if (requests == null || requests.Count == 0)
            return BadRequest(new { message = "At least one user is required" });

        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        var users = await _userService.BulkCreateUsersAsync(requests, createdBy);
        return Ok(new { message = $"{users.Count()} users created successfully", users });
    }

    [HttpPut("users/{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDto request)
    {
        var modifiedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        var success = await _userService.UpdateUserAsync(userId, request, modifiedBy);
        if (!success) return NotFound(new { message = "User not found" });
        return Ok(new { message = "User updated successfully" });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeactivateUser(string userId)
    {
        var success = await _userService.DeactivateUserAsync(userId);
        if (!success) return NotFound(new { message = "User not found" });
        return Ok(new { message = "User deactivated successfully" });
    }

    [HttpDelete("users/{userId}/permanent")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var success = await _userService.DeleteUserAsync(userId);
        if (!success) return NotFound(new { message = "User not found" });
        return Ok(new { message = $"User '{userId}' permanently deleted. Database/schema dropped, all files and logs removed." });
    }

    [HttpPost("users/{userId}/change-password")]
    public async Task<IActionResult> ChangeUserPassword(string userId, [FromBody] AdminChangePasswordDto request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Password is required" });

        var userRepo = HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await userRepo.GetByUserIDAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var authService = HttpContext.RequestServices.GetRequiredService<IAuthService>();
        var adminUserId = User.FindFirst(ClaimTypes.Name)?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        await authService.ChangePasswordAsync(user.Id, request.NewPassword, adminUserId, ipAddress);

        // Force user to change on next login
        user.MustChangePassword = true;
        user.ModifiedDate = DCView.Hackathon.Shared.Helpers.DateTimeHelper.Now;
        user.ModifiedBy = adminUserId;
        await userRepo.UpdateAsync(user);

        return Ok(new { message = $"Password changed for {userId}. User will be asked to change it on next login." });
    }

    [HttpGet("password-reset-requests")]
    public async Task<IActionResult> GetPasswordResetRequests()
    {
        var userRepo = HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var allUsers = await _userService.GetAllParticipantsAsync();
        var requests = allUsers.Where(u => u.PasswordResetRequested).ToList();
        return Ok(requests);
    }

    // ─── Session Management ──────────────────────────────────────

    [HttpPost("sessions/{userId}/activate")]
    public async Task<IActionResult> ActivateSession(string userId, [FromBody] ActivateSessionDto? request)
    {
        var activatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        var success = await _sessionService.ActivateSessionAsync(userId, request?.DurationMinutes, activatedBy);
        if (!success) return NotFound(new { message = "User not found" });
        return Ok(new { message = "Session activated" });
    }

    [HttpPost("sessions/{userId}/deactivate")]
    public async Task<IActionResult> DeactivateSession(string userId)
    {
        var success = await _sessionService.DeactivateSessionAsync(userId);
        if (!success) return NotFound(new { message = "User or session not found" });
        return Ok(new { message = "Session deactivated" });
    }

    [HttpPost("sessions/{userId}/extend")]
    public async Task<IActionResult> ExtendSession(string userId, [FromBody] ExtendSessionDto request)
    {
        var success = await _sessionService.ExtendSessionAsync(userId, request.AdditionalMinutes);
        if (!success) return NotFound(new { message = "User or session not found" });
        return Ok(new { message = $"Session extended by {request.AdditionalMinutes} minutes" });
    }

    [HttpPost("users/{userId}/reset-db")]
    public async Task<IActionResult> ResetDatabase(string userId)
    {
        var success = await _sessionService.ResetDatabaseAsync(userId);
        if (!success) return NotFound(new { message = "User/session not found or database not created" });
        return Ok(new { message = "Database reset successfully. User can create a new one." });
    }

    // ─── Files & Export ──────────────────────────────────────────

    [HttpGet("users/{userId}/files")]
    public async Task<IActionResult> GetUserFiles(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var files = await _fileService.GetAllFilesByUserIdAsync(user.Id);
        return Ok(files);
    }

    [HttpGet("export/{userId}")]
    public async Task<IActionResult> ExportUser(string userId)
    {
        var zipBytes = await _exportService.ExportUserAsync(userId);
        return File(zipBytes, "application/zip", $"Hackathon_Export_{userId}_{DateTimeHelper.Now:yyyyMMdd}.zip");
    }

    [HttpGet("export/all")]
    public async Task<IActionResult> ExportAll()
    {
        var zipBytes = await _exportService.ExportAllAsync();
        return File(zipBytes, "application/zip", $"Hackathon_Export_All_{DateTimeHelper.Now:yyyyMMdd}.zip");
    }

    // ─── Hackathon Server Config ─────────────────────────────────

    [HttpGet("config/hackathon-server")]
    public async Task<IActionResult> GetHackathonConfig()
    {
        var config = await _configRepo.GetActiveConfigAsync();
        if (config == null)
            return Ok(new { configured = false });

        return Ok(new
        {
            configured = true,
            config.ServerName,
            config.AdminUserId,
            config.DbPrefix,
            config.MaxQueryTimeoutSeconds,
            config.MaxRowsPerPage,
            dbEngineType = config.DbEngineType.ToString(),
            config.OracleServiceName,
            config.Port,
            config.IsActive
        });
    }

    [HttpPost("config/hackathon-server")]
    public async Task<IActionResult> ConfigureHackathonServer([FromBody] ConfigureServerDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ServerName) ||
            string.IsNullOrWhiteSpace(request.AdminUserId) ||
            string.IsNullOrWhiteSpace(request.AdminPassword))
            return BadRequest(new { message = "ServerName, AdminUserId, and AdminPassword are required" });

        // Parse engine type
        var engineType = DCView.Hackathon.Domain.Enums.DbEngineType.SqlServer;
        if (!string.IsNullOrWhiteSpace(request.DbEngineType))
        {
            if (!Enum.TryParse<DCView.Hackathon.Domain.Enums.DbEngineType>(request.DbEngineType, ignoreCase: true, out engineType))
                return BadRequest(new { message = "DbEngineType must be 'SqlServer' or 'Oracle'" });
        }

        // Oracle-specific validation
        if (engineType == DCView.Hackathon.Domain.Enums.DbEngineType.Oracle)
        {
            if (string.IsNullOrWhiteSpace(request.OracleServiceName))
                return BadRequest(new { message = "OracleServiceName is required for Oracle engine" });
        }

        string encKey = _configuration["Encryption:Key"]!;
        string encryptedPwd = EncryptionHelper.Encrypt(request.AdminPassword, encKey);

        var config = new HackathonConfig
        {
            ServerName = request.ServerName,
            AdminUserId = request.AdminUserId,
            AdminPasswordEncrypted = encryptedPwd,
            DbPrefix = request.DbPrefix ?? (engineType == DCView.Hackathon.Domain.Enums.DbEngineType.Oracle ? "HACK_" : "Hackathon_"),
            MaxQueryTimeoutSeconds = request.MaxQueryTimeoutSeconds ?? 30,
            MaxRowsPerPage = request.MaxRowsPerPage ?? 25,
            DbEngineType = engineType,
            OracleServiceName = request.OracleServiceName,
            Port = request.Port,
            IsActive = true,
            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value
        };

        await _configRepo.CreateOrUpdateAsync(config);
        return Ok(new { message = $"Hackathon server configured successfully (Engine: {engineType})" });
    }

    // ─── AI Detection ────────────────────────────────────────────

    [HttpGet("ai-detection/flagged")]
    public async Task<IActionResult> GetFlaggedFiles([FromQuery] int minScore = 60)
    {
        var flagged = await _aiDetectionService.GetFlaggedAsync(minScore);
        return Ok(flagged);
    }

    [HttpGet("ai-detection/user/{userId}")]
    public async Task<IActionResult> GetUserAiDetectionLogs(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var logs = await _aiDetectionService.GetLogsByUserIdAsync(user.Id);
        return Ok(logs);
    }

    [HttpGet("ai-detection/settings")]
    public async Task<IActionResult> GetAiDetectionSettings()
    {
        var settings = await _aiDetectionService.GetSettingsAsync();
        return Ok(settings);
    }

    [HttpPut("ai-detection/settings")]
    public async Task<IActionResult> UpdateAiDetectionSettings([FromBody] Application.DTOs.AiDetection.UpdateAiSettingsDto request)
    {
        var validModes = new[] { "Block", "AllowAndMark", "Disabled" };
        if (!validModes.Contains(request.Mode))
            return BadRequest(new { message = "Mode must be Block, AllowAndMark, or Disabled" });

        if (request.BlockThreshold < 0 || request.BlockThreshold > 100)
            return BadRequest(new { message = "Threshold must be between 0 and 100" });

        var admin = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        await _aiDetectionService.UpdateGlobalSettingsAsync(request, admin);
        return Ok(new { message = "AI detection settings updated" });
    }

    [HttpPut("ai-detection/settings/user/{userId}")]
    public async Task<IActionResult> SetUserAiOverride(string userId, [FromBody] Application.DTOs.AiDetection.UpdateUserOverrideDto request)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var admin = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        await _aiDetectionService.SetUserOverrideAsync(user.Id, request, admin);
        return Ok(new { message = $"AI detection override set for {userId}" });
    }

    [HttpDelete("ai-detection/settings/user/{userId}")]
    public async Task<IActionResult> RemoveUserAiOverride(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        await _aiDetectionService.RemoveUserOverrideAsync(user.Id);
        return Ok(new { message = $"AI detection override removed for {userId}" });
    }

    [HttpGet("ai-detection/blocked")]
    public async Task<IActionResult> GetBlockedSaves([FromQuery] string? status)
    {
        if (status == "pending")
        {
            var pending = await _aiDetectionService.GetPendingBlockedSavesAsync();
            return Ok(pending);
        }
        var all = await _aiDetectionService.GetAllBlockedSavesAsync();
        return Ok(all);
    }

    [HttpGet("ai-detection/blocked/user/{userId}")]
    public async Task<IActionResult> GetUserBlockedSaves(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var blocked = await _aiDetectionService.GetBlockedSavesByUserAsync(user.Id);
        return Ok(blocked);
    }

    [HttpPost("ai-detection/blocked/{id}/approve")]
    public async Task<IActionResult> ApproveBlockedSave(long id, [FromBody] Application.DTOs.AiDetection.ReviewBlockedSaveDto? request)
    {
        var admin = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        var success = await _aiDetectionService.ApproveBlockedSaveAsync(id, admin, request?.Remarks);
        if (!success) return NotFound(new { message = "Blocked save not found or already reviewed" });
        return Ok(new { message = "Save approved. File content has been saved for the user." });
    }

    [HttpPost("ai-detection/blocked/{id}/reject")]
    public async Task<IActionResult> RejectBlockedSave(long id, [FromBody] Application.DTOs.AiDetection.ReviewBlockedSaveDto? request)
    {
        var admin = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        var success = await _aiDetectionService.RejectBlockedSaveAsync(id, admin, request?.Remarks);
        if (!success) return NotFound(new { message = "Blocked save not found or already reviewed" });
        return Ok(new { message = "Save rejected." });
    }

    // ─── Submission Management ───────────────────────────────────

    [HttpPost("submissions/{userId}/release")]
    public async Task<IActionResult> ReleaseSubmission(string userId, [FromBody] ReleaseSubmissionDto? request)
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var adminName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Admin";
        var assessmentType = user.AssessmentType ?? "SQL";

        // Release session submission (SQL + ManualTesting)
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (session != null && session.IsSubmitted)
        {
            session.IsSubmitted = false;
            session.SubmittedAt = null;
        }

        // Release MCQ test (reset to allow retake)
        if (assessmentType == "MCQ" && user.AssessmentId.HasValue)
        {
            var mcqTest = await db.McqTests
                .Include(t => t.Answers)
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.AssessmentId == user.AssessmentId && t.IsSubmitted);

            if (mcqTest != null)
            {
                // Remove the test and answers so user can retake
                db.McqAnswers.RemoveRange(mcqTest.Answers);
                db.McqTests.Remove(mcqTest);
            }
        }

        // Audit log
        db.SubmissionAuditLogs.Add(new Domain.Entities.SubmissionAuditLog
        {
            UserId = user.Id,
            UserLoginId = userId,
            Action = "Released",
            AssessmentType = assessmentType,
            PerformedBy = adminName,
            Reason = request?.Reason,
            EventTime = DCView.Hackathon.Shared.Helpers.DateTimeHelper.Now
        });

        await db.SaveChangesAsync();

        return Ok(new { message = $"Submission released for {userId}. User can now edit/retake." });
    }

    [HttpGet("submissions/{userId}/audit")]
    public async Task<IActionResult> GetSubmissionAudit(string userId)
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var logs = await db.SubmissionAuditLogs
            .Where(l => l.UserLoginId == userId.ToUpper())
            .OrderByDescending(l => l.EventTime)
            .Select(l => new { l.Action, l.AssessmentType, l.PerformedBy, l.Reason, l.EventTime })
            .ToListAsync();
        return Ok(logs);
    }

    [HttpGet("submissions/{userId}/files")]
    public async Task<IActionResult> GetUserSubmissionFiles(string userId)
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var files = await db.SubmissionFiles
            .Where(f => f.UserId == user.Id)
            .Select(f => new { f.Id, f.FileName, f.ContentType, f.FileSizeBytes, f.UploadedAt })
            .ToListAsync();

        return Ok(files);
    }

    [HttpGet("submissions/files/{fileId}/download")]
    public async Task<IActionResult> DownloadSubmissionFile(int fileId)
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var file = await db.SubmissionFiles.FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null) return NotFound(new { message = "File not found" });

        return File(file.FileData, file.ContentType, file.FileName);
    }

    // ─── Question Paper Management ───────────────────────────────

    [HttpGet("question-paper")]
    public async Task<IActionResult> GetQuestionPaper()
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var paper = await db.QuestionPapers.FirstOrDefaultAsync(q => q.IsActive);

        if (paper == null)
            return Ok(new { configured = false });

        return Ok(new
        {
            configured = true,
            paper.Id,
            paper.Title,
            paper.HtmlContent,
            paper.ScheduledDate,
            startTime = paper.StartTime?.ToString(@"hh\:mm"),
            endTime = paper.EndTime?.ToString(@"hh\:mm"),
            paper.DurationMinutes,
            paper.IsActive,
            paper.CreatedDate,
            paper.ModifiedDate
        });
    }

    [HttpPost("question-paper")]
    public async Task<IActionResult> SaveQuestionPaper([FromBody] SaveQuestionPaperDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required" });

        if (string.IsNullOrWhiteSpace(request.HtmlContent))
            return BadRequest(new { message = "Question HTML content is required" });

        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var admin = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";

        var existing = await db.QuestionPapers.FirstOrDefaultAsync(q => q.IsActive);

        if (existing != null)
        {
            existing.Title = request.Title;
            existing.HtmlContent = request.HtmlContent;
            existing.ScheduledDate = request.ScheduledDate;
            existing.StartTime = !string.IsNullOrEmpty(request.StartTime) ? TimeSpan.Parse(request.StartTime) : null;
            existing.EndTime = !string.IsNullOrEmpty(request.EndTime) ? TimeSpan.Parse(request.EndTime) : null;
            existing.DurationMinutes = request.DurationMinutes;
            existing.ModifiedDate = DateTimeHelper.Now;
            existing.ModifiedBy = admin;
            db.QuestionPapers.Update(existing);
        }
        else
        {
            var paper = new HackathonQuestionPaper
            {
                Title = request.Title,
                HtmlContent = request.HtmlContent,
                ScheduledDate = request.ScheduledDate,
                StartTime = !string.IsNullOrEmpty(request.StartTime) ? TimeSpan.Parse(request.StartTime) : null,
                EndTime = !string.IsNullOrEmpty(request.EndTime) ? TimeSpan.Parse(request.EndTime) : null,
                DurationMinutes = request.DurationMinutes,
                IsActive = true,
                CreatedDate = DateTimeHelper.Now,
                CreatedBy = admin
            };
            db.QuestionPapers.Add(paper);
        }

        await db.SaveChangesAsync();
        return Ok(new { message = "Question paper saved successfully" });
    }

    [HttpPost("question-paper/upload-html")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadQuestionHtml(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "HTML file is required" });

        if (!file.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .html or .htm files are allowed" });

        using var reader = new StreamReader(file.OpenReadStream());
        var htmlContent = await reader.ReadToEndAsync();

        return Ok(new { htmlContent, fileName = file.FileName });
    }

    // ─── Security Settings ────────────────────────────────────────

    [HttpGet("security-settings")]
    public async Task<IActionResult> GetSecuritySettings()
    {
        var repo = HttpContext.RequestServices.GetRequiredService<ISecuritySettingsRepository>();
        var settings = await repo.GetAsync();
        return Ok(settings);
    }

    [HttpPut("security-settings")]
    public async Task<IActionResult> UpdateSecuritySettings([FromBody] UpdateSecuritySettingsDto request)
    {
        var repo = HttpContext.RequestServices.GetRequiredService<ISecuritySettingsRepository>();
        var settings = await repo.GetAsync();

        settings.MinLength = request.MinLength;
        settings.MaxLength = request.MaxLength;
        settings.RequireUppercase = request.RequireUppercase;
        settings.RequireLowercase = request.RequireLowercase;
        settings.RequireDigit = request.RequireDigit;
        settings.RequireSpecialChar = request.RequireSpecialChar;
        settings.PasswordHistoryCount = request.PasswordHistoryCount;
        settings.MaxFailedLoginAttempts = request.MaxFailedLoginAttempts;
        settings.LockoutDurationMinutes = request.LockoutDurationMinutes;
        settings.PasswordExpiryDays = request.PasswordExpiryDays;
        settings.MaxConcurrentSessions = request.MaxConcurrentSessions;
        settings.ModifiedDate = DateTimeHelper.Now;
        settings.ModifiedBy = User.FindFirst(ClaimTypes.Name)?.Value;

        await repo.UpdateAsync(settings);
        return Ok(new { message = "Security settings updated" });
    }

    [HttpGet("password-change-logs")]
    public async Task<IActionResult> GetPasswordChangeLogs([FromQuery] string? userId)
    {
        var logRepo = HttpContext.RequestServices.GetRequiredService<IPasswordChangeLogRepository>();
        var userRepo = HttpContext.RequestServices.GetRequiredService<IUserRepository>();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await userRepo.GetByUserIDAsync(userId);
            if (user == null) return NotFound(new { message = "User not found" });
            var logs = await logRepo.GetByUserIdAsync(user.Id, 50);
            return Ok(logs.Select(l => new
            {
                l.Id,
                l.UserId,
                loginId = userId,
                l.ChangedBy,
                l.ChangedByUserId,
                l.ChangedAt,
                l.IpAddress
            }));
        }

        // Return all users' recent changes (last 100)
        var users = await userRepo.GetAllParticipantsAsync();
        var allLogs = new List<object>();
        foreach (var u in users)
        {
            var logs = await logRepo.GetByUserIdAsync(u.Id, 5);
            allLogs.AddRange(logs.Select(l => new
            {
                l.Id,
                l.UserId,
                loginId = u.UserID,
                fullName = u.FullName,
                l.ChangedBy,
                l.ChangedByUserId,
                l.ChangedAt,
                l.IpAddress
            }));
        }
        return Ok(allLogs.OrderByDescending(l => ((dynamic)l).ChangedAt).Take(100));
    }

    // ─── Scaffold Scripts ─────────────────────────────────────────

    [HttpGet("scaffold-scripts")]
    public async Task<IActionResult> GetScaffoldScripts()
    {
        var repo = HttpContext.RequestServices.GetRequiredService<IScaffoldScriptRepository>();
        var scripts = await repo.GetAllAsync();
        return Ok(scripts);
    }

    [HttpPost("scaffold-scripts")]
    public async Task<IActionResult> CreateScaffoldScript([FromBody] CreateScaffoldScriptDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.SqlContent))
            return BadRequest(new { message = "Title and SQL content are required" });

        var repo = HttpContext.RequestServices.GetRequiredService<IScaffoldScriptRepository>();
        var script = new ScaffoldScript
        {
            Title = request.Title.Trim(),
            FileName = request.FileName?.Trim() ?? $"{request.Title.Trim()}.sql",
            SqlContent = request.SqlContent,
            ExecutionOrder = request.ExecutionOrder,
            IsActive = true,
            CreatedDate = DateTimeHelper.Now,
            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value
        };
        await repo.CreateAsync(script);
        return Ok(script);
    }

    [HttpPut("scaffold-scripts/{id:int}")]
    public async Task<IActionResult> UpdateScaffoldScript(int id, [FromBody] CreateScaffoldScriptDto request)
    {
        var repo = HttpContext.RequestServices.GetRequiredService<IScaffoldScriptRepository>();
        var script = await repo.GetByIdAsync(id);
        if (script == null) return NotFound(new { message = "Script not found" });

        if (!string.IsNullOrWhiteSpace(request.Title)) script.Title = request.Title.Trim();
        if (!string.IsNullOrWhiteSpace(request.FileName)) script.FileName = request.FileName.Trim();
        if (!string.IsNullOrWhiteSpace(request.SqlContent)) script.SqlContent = request.SqlContent;
        script.ExecutionOrder = request.ExecutionOrder;
        script.IsActive = request.IsActive;
        script.ModifiedDate = DateTimeHelper.Now;
        script.ModifiedBy = User.FindFirst(ClaimTypes.Name)?.Value;

        await repo.UpdateAsync(script);
        return Ok(script);
    }

    [HttpDelete("scaffold-scripts/{id:int}")]
    public async Task<IActionResult> DeleteScaffoldScript(int id)
    {
        var repo = HttpContext.RequestServices.GetRequiredService<IScaffoldScriptRepository>();
        await repo.DeleteAsync(id);
        return Ok(new { message = "Script deleted" });
    }

    // ─── Schedule Management ──────────────────────────────────────

    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule()
    {
        var repo = HttpContext.RequestServices.GetRequiredService<IScheduleRepository>();
        var schedule = await repo.GetActiveScheduleAsync();
        if (schedule == null) return Ok(new { configured = false });
        return Ok(new
        {
            configured = true,
            schedule.Id,
            schedule.SessionStartTime,
            schedule.SessionEndTime,
            schedule.ExtensionMinutes,
            schedule.AlertConfig,
            schedule.IsActive,
            schedule.ScheduleDate,
            breaks = schedule.Breaks.Select(b => new { b.Id, b.Title, b.StartTime, b.EndTime })
        });
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> SaveSchedule([FromBody] SaveScheduleDto request)
    {
        var repo = HttpContext.RequestServices.GetRequiredService<IScheduleRepository>();

        var schedule = await repo.GetActiveScheduleAsync();
        if (schedule == null)
        {
            schedule = new HackathonSchedule
            {
                CreatedDate = DateTimeHelper.Now,
                CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value
            };
        }

        schedule.SessionStartTime = request.SessionStartTime;
        schedule.SessionEndTime = request.SessionEndTime;
        schedule.ScheduleDate = request.ScheduleDate;
        if (!string.IsNullOrWhiteSpace(request.AlertConfig))
            schedule.AlertConfig = request.AlertConfig;
        schedule.IsActive = true;
        schedule.ModifiedDate = DateTimeHelper.Now;
        schedule.ModifiedBy = User.FindFirst(ClaimTypes.Name)?.Value;

        await repo.CreateOrUpdateAsync(schedule);
        return Ok(new { message = "Schedule saved", schedule.Id });
    }

    [HttpPost("schedule/breaks")]
    public async Task<IActionResult> AddBreak([FromBody] AddBreakDto request)
    {
        var repo = HttpContext.RequestServices.GetRequiredService<IScheduleRepository>();
        var schedule = await repo.GetActiveScheduleAsync();
        if (schedule == null) return BadRequest(new { message = "No active schedule. Create a schedule first." });

        var breakItem = new HackathonBreak
        {
            ScheduleId = schedule.Id,
            Title = request.Title?.Trim() ?? "Break",
            StartTime = request.StartTime,
            EndTime = request.EndTime,
        };
        await repo.AddBreakAsync(breakItem);
        return Ok(new { message = "Break added", breakItem.Id });
    }

    [HttpDelete("schedule/breaks/{breakId:int}")]
    public async Task<IActionResult> RemoveBreak(int breakId)
    {
        var repo = HttpContext.RequestServices.GetRequiredService<IScheduleRepository>();
        await repo.RemoveBreakAsync(breakId);
        return Ok(new { message = "Break removed" });
    }

    [HttpPost("schedule/extend")]
    public async Task<IActionResult> ExtendSchedule([FromBody] ExtendScheduleDto request)
    {
        var repo = HttpContext.RequestServices.GetRequiredService<IScheduleRepository>();
        var schedule = await repo.GetActiveScheduleAsync();
        if (schedule == null) return BadRequest(new { message = "No active schedule" });

        await repo.UpdateExtensionAsync(schedule.Id, request.Minutes);
        return Ok(new { message = $"Extended by {request.Minutes} minutes. Total extension: {schedule.ExtensionMinutes + request.Minutes}m" });
    }
}

public class ConfigureServerDto
{
    public string ServerName { get; set; } = string.Empty;
    public string AdminUserId { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string? DbPrefix { get; set; }
    public int? MaxQueryTimeoutSeconds { get; set; }
    public int? MaxRowsPerPage { get; set; }

    /// <summary>Database engine: "SqlServer" (default) or "Oracle".</summary>
    public string? DbEngineType { get; set; }

    /// <summary>Oracle-specific: service name (e.g., "XEPDB1", "ORCL").</summary>
    public string? OracleServiceName { get; set; }

    /// <summary>Oracle-specific: port number (default 1521).</summary>
    public int? Port { get; set; }
}

public class AdminChangePasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

public class SaveQuestionPaperDto
{
    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public DateTime? ScheduledDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
}

public class UpdateSecuritySettingsDto
{
    public int MinLength { get; set; } = 8;
    public int MaxLength { get; set; } = 64;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialChar { get; set; } = true;
    public int PasswordHistoryCount { get; set; } = 5;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public int PasswordExpiryDays { get; set; } = 0;
    public int MaxConcurrentSessions { get; set; } = 1;
}

public class CreateScaffoldScriptDto
{
    public string Title { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string SqlContent { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class SaveScheduleDto
{
    public string SessionStartTime { get; set; } = "10:00";
    public string SessionEndTime { get; set; } = "18:00";
    public DateTime? ScheduleDate { get; set; }
    public string? AlertConfig { get; set; }
}

public class AddBreakDto
{
    public string? Title { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

public class ExtendScheduleDto
{
    public int Minutes { get; set; }
}

public class ReleaseSubmissionDto
{
    public string? Reason { get; set; }
}


