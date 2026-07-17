using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.API.Controllers;

[Route("api/history")]
[ApiController]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly IExecutionHistoryRepository _historyRepo;

    public HistoryController(IExecutionHistoryRepository historyRepo) => _historyRepo = historyRepo;

    [HttpGet]
    public async Task<IActionResult> GetMyHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var (items, total) = await _historyRepo.GetByUserIdAsync(userId, page, pageSize);

        return Ok(new
        {
            items = items.Select(h => new
            {
                h.Id,
                h.DatabaseName,
                queryText = h.QueryText,
                queryPreview = h.QueryText.Length > 200 ? h.QueryText[..200] + "..." : h.QueryText,
                h.QueryType,
                h.Status,
                h.ErrorMessage,
                h.RowsAffected,
                h.DurationMs,
                h.ExecutedAt
            }),
            totalCount = total,
            page,
            pageSize
        });
    }

    [HttpGet("{userId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetUserHistory(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        // Resolve UserID string to int id
        var userRepo = HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await userRepo.GetByUserIDAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var (items, total) = await _historyRepo.GetByUserIdAsync(user.Id, page, pageSize);

        return Ok(new
        {
            items = items.Select(h => new
            {
                h.Id,
                UserID = user.UserID,
                h.DatabaseName,
                queryText = h.QueryText,
                queryPreview = h.QueryText.Length > 200 ? h.QueryText[..200] + "..." : h.QueryText,
                h.QueryType,
                h.Status,
                h.ErrorMessage,
                h.RowsAffected,
                h.DurationMs,
                h.ExecutedAt
            }),
            totalCount = total,
            page,
            pageSize
        });
    }
}
