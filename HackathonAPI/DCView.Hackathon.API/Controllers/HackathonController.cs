using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Application.DTOs.Hackathon;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.API.Controllers;

[Route("api/hackathon")]
[ApiController]
[Authorize(Roles = "Participant,SuperAdmin")]
public class HackathonController : ControllerBase
{
    private readonly IHackathonService _hackathonService;

    public HackathonController(IHackathonService hackathonService) => _hackathonService = hackathonService;

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetCurrentUserId();
        var status = await _hackathonService.GetSessionStatusAsync(userId);
        return Ok(status);
    }

    [HttpPost("create-database")]
    public async Task<IActionResult> CreateDatabase()
    {
        var userId = GetCurrentUserId();
        var result = await _hackathonService.CreateDatabaseAsync(userId);
        return Ok(result);
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ExecuteRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
            return BadRequest(new { message = "SQL cannot be empty" });

        if (request.Page < 1) request.Page = 1;
        if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 25;

        var userId = GetCurrentUserId();
        var result = await _hackathonService.ExecuteAsync(userId, request);
        return Ok(result);
    }

    [HttpGet("question-paper")]
    public async Task<IActionResult> GetQuestionPaper()
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var paper = await db.QuestionPapers.FirstOrDefaultAsync(q => q.IsActive);

        if (paper == null)
            return NotFound(new { message = "No question paper has been published yet" });

        return Ok(new
        {
            paper.Title,
            paper.HtmlContent,
            paper.ScheduledDate,
            startTime = paper.StartTime?.ToString(@"hh\:mm"),
            endTime = paper.EndTime?.ToString(@"hh\:mm"),
            paper.DurationMinutes
        });
    }

    // ─── Submission ──────────────────────────────────────────────

    [HttpPost("submit")]
    public async Task<IActionResult> Submit()
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var userId = GetCurrentUserId();

        var session = await db.Sessions.FirstOrDefaultAsync(s => s.UserId == userId);
        if (session == null) return NotFound(new { message = "Session not found" });
        if (session.IsSubmitted) return BadRequest(new { message = "Already submitted" });

        session.IsSubmitted = true;
        session.SubmittedAt = DCView.Hackathon.Shared.Helpers.DateTimeHelper.Now;
        await db.SaveChangesAsync();

        return Ok(new { message = "Submission successful. Your work has been locked." });
    }

    [HttpPost("submission-files")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)] // 6 MB to allow overhead
    public async Task<IActionResult> UploadSubmissionFiles([FromForm] List<IFormFile> files)
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var userId = GetCurrentUserId();

        var session = await db.Sessions.FirstOrDefaultAsync(s => s.UserId == userId);
        if (session == null) return NotFound(new { message = "Session not found" });
        if (session.IsSubmitted) return BadRequest(new { message = "Already submitted. Cannot upload." });

        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files provided" });

        // Allowed extensions
        var allowedExtensions = new[] { ".docx", ".doc", ".xlsx", ".xls" };
        var allowedContentTypes = new[] {
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel"
        };

        // Validate file types
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { message = $"Invalid file type: {file.FileName}. Only Word (.docx, .doc) and Excel (.xlsx, .xls) files are allowed." });
        }

        // Validate total size (5 MB max combined)
        var existingFiles = await db.SubmissionFiles.Where(f => f.UserId == userId).ToListAsync();
        long existingSize = existingFiles.Sum(f => f.FileSizeBytes);
        long newSize = files.Sum(f => f.Length);
        long totalSize = existingSize + newSize;
        const long maxTotalSize = 5 * 1024 * 1024; // 5 MB

        if (totalSize > maxTotalSize)
            return BadRequest(new { message = $"Total file size exceeds 5 MB limit. Current: {existingSize / 1024}KB, New: {newSize / 1024}KB, Max: 5120KB." });

        // Save files
        var uploaded = new List<object>();
        foreach (var file in files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var submissionFile = new DCView.Hackathon.Domain.Entities.UserSubmissionFile
            {
                UserId = userId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                FileData = ms.ToArray(),
                UploadedAt = DCView.Hackathon.Shared.Helpers.DateTimeHelper.Now
            };
            db.SubmissionFiles.Add(submissionFile);
            uploaded.Add(new { submissionFile.FileName, size = file.Length });
        }
        await db.SaveChangesAsync();

        return Ok(new { message = $"{files.Count} file(s) uploaded", files = uploaded });
    }

    [HttpGet("submission-files")]
    public async Task<IActionResult> GetMySubmissionFiles()
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var userId = GetCurrentUserId();

        var files = await db.SubmissionFiles
            .Where(f => f.UserId == userId)
            .Select(f => new { f.Id, f.FileName, f.ContentType, f.FileSizeBytes, f.UploadedAt })
            .ToListAsync();

        return Ok(files);
    }

    [HttpDelete("submission-files/{fileId}")]
    public async Task<IActionResult> DeleteSubmissionFile(int fileId)
    {
        var db = HttpContext.RequestServices.GetRequiredService<HackathonDbContext>();
        var userId = GetCurrentUserId();

        var file = await db.SubmissionFiles.FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
        if (file == null) return NotFound(new { message = "File not found" });

        var session = await db.Sessions.FirstOrDefaultAsync(s => s.UserId == userId);
        if (session?.IsSubmitted == true) return BadRequest(new { message = "Already submitted. Cannot delete." });

        db.SubmissionFiles.Remove(file);
        await db.SaveChangesAsync();

        return Ok(new { message = "File deleted" });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim!);
    }
}
