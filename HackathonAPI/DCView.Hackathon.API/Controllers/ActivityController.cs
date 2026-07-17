using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.API.Controllers;

[Route("api/activity")]
[ApiController]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly ITabSwitchLogRepository _tabSwitchRepo;
    private readonly IUserRepository _userRepo;

    public ActivityController(ITabSwitchLogRepository tabSwitchRepo, IUserRepository userRepo)
    {
        _tabSwitchRepo = tabSwitchRepo;
        _userRepo = userRepo;
    }

    /// <summary>
    /// Participant reports tab-switch events (batched from the client).
    /// </summary>
    [HttpPost("tab-switch")]
    public async Task<IActionResult> ReportTabSwitch([FromBody] TabSwitchBatchDto request)
    {
        var userId = GetUserId();
        if (request.Events == null || request.Events.Count == 0)
            return Ok(new { message = "No events" });

        var logs = request.Events.Select(e => new TabSwitchLog
        {
            UserId = userId,
            EventType = e.EventType,
            EventTime = DCView.Hackathon.Shared.Helpers.DateTimeHelper.FromUtc(e.Timestamp.ToUniversalTime()),
            AwayDurationSeconds = e.AwayDurationSeconds
        }).ToList();

        await _tabSwitchRepo.CreateBatchAsync(logs);

        // Check if suspicious
        var countInLastHour = await _tabSwitchRepo.GetCountByUserInLastHourAsync(userId);
        bool isSuspicious = countInLastHour >= 20;

        return Ok(new { logged = logs.Count, switchesInLastHour = countInLastHour, isSuspicious });
    }

    /// <summary>
    /// Admin: Get tab-switch activity for all active users (real-time overview).
    /// </summary>
    [HttpGet("overview")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetActivityOverview()
    {
        var switchCounts = await _tabSwitchRepo.GetSwitchCountsForActiveUsersAsync();
        var users = await _userRepo.GetAllParticipantsAsync();

        var overview = users.Select(u => new
        {
            userId = u.UserID,
            fullName = u.FullName,
            switchesInLastHour = switchCounts.GetValueOrDefault(u.Id, 0),
            isSuspicious = switchCounts.GetValueOrDefault(u.Id, 0) >= 20
        })
        .OrderByDescending(x => x.switchesInLastHour)
        .ToList();

        return Ok(overview);
    }

    /// <summary>
    /// Admin: Get detailed tab-switch logs for a specific user.
    /// </summary>
    [HttpGet("{userId}/logs")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetUserLogs(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var logs = await _tabSwitchRepo.GetByUserIdAsync(user.Id);
        var countInLastHour = await _tabSwitchRepo.GetCountByUserInLastHourAsync(user.Id);

        return Ok(new
        {
            userId = user.UserID,
            switchesInLastHour = countInLastHour,
            isSuspicious = countInLastHour >= 20,
            logs = logs.Select(l => new
            {
                l.EventType,
                l.EventTime,
                l.AwayDurationSeconds
            })
        });
    }

    /// <summary>
    /// Admin: Get devtools detection attempts for all users.
    /// </summary>
    [HttpGet("devtools")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetDevToolsAttempts()
    {
        var users = await _userRepo.GetAllParticipantsAsync();
        var result = new List<object>();

        foreach (var u in users)
        {
            var logs = await _tabSwitchRepo.GetByUserIdAsync(u.Id, 500);
            var devtoolsLogs = logs.Where(l => l.EventType.StartsWith("devtools_")).ToList();

            if (devtoolsLogs.Count > 0)
            {
                result.Add(new
                {
                    userId = u.UserID,
                    fullName = u.FullName,
                    totalAttempts = devtoolsLogs.Count,
                    logoutCount = devtoolsLogs.Count(l => l.EventType.Contains("logout")),
                    lastAttempt = devtoolsLogs.Max(l => l.EventTime),
                    attempts = devtoolsLogs
                        .OrderByDescending(l => l.EventTime)
                        .Select(l => new
                        {
                            eventType = l.EventType,
                            eventTime = l.EventTime,
                            method = l.EventType.Replace("devtools_", "").Replace("logout_", "")
                        })
                });
            }
        }

        return Ok(result.OrderByDescending(r => ((dynamic)r).totalAttempts));
    }

    /// <summary>
    /// Admin: Get devtools detection attempts for a specific user.
    /// </summary>
    [HttpGet("devtools/{userId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetUserDevToolsAttempts(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var logs = await _tabSwitchRepo.GetByUserIdAsync(user.Id, 500);
        var devtoolsLogs = logs.Where(l => l.EventType.StartsWith("devtools_")).ToList();

        return Ok(new
        {
            userId = user.UserID,
            fullName = user.FullName,
            totalAttempts = devtoolsLogs.Count,
            logoutCount = devtoolsLogs.Count(l => l.EventType.Contains("logout")),
            attempts = devtoolsLogs
                .OrderByDescending(l => l.EventTime)
                .Select(l => new
                {
                    eventType = l.EventType,
                    eventTime = l.EventTime,
                    method = l.EventType.Replace("devtools_", "").Replace("logout_", "")
                })
        });
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public class TabSwitchBatchDto
{
    public List<TabSwitchEventDto> Events { get; set; } = new();
}

public class TabSwitchEventDto
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int? AwayDurationSeconds { get; set; }
}
