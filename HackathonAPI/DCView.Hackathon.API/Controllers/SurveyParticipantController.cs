using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.API.Controllers;

[Route("api/surveys/{surveyId:guid}/participants")]
[ApiController]
[Authorize]
public class SurveyParticipantController : ControllerBase
{
    private readonly ISurveyDistributionService _distributionService;

    public SurveyParticipantController(ISurveyDistributionService distributionService)
    {
        _distributionService = distributionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetParticipants(Guid surveyId)
    {
        var participants = await _distributionService.GetParticipantsAsync(surveyId);
        return Ok(participants);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(Guid surveyId)
    {
        var participants = await _distributionService.GetPendingParticipantsAsync(surveyId);
        return Ok(participants);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> BulkUpload(Guid surveyId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File is required" });

        var allowedExtensions = new[] { ".csv", ".txt" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = "Only CSV files are accepted" });

        using var stream = file.OpenReadStream();
        var result = await _distributionService.BulkUploadAsync(surveyId, stream, file.FileName);
        return Ok(result);
    }

    [HttpDelete("{participantId:guid}")]
    public async Task<IActionResult> Delete(Guid surveyId, Guid participantId)
    {
        var success = await _distributionService.DeleteParticipantAsync(participantId);
        if (!success) return NotFound(new { message = "Participant not found" });
        return Ok(new { message = "Participant removed" });
    }

    [HttpPut("{participantId:guid}/decline")]
    public async Task<IActionResult> Decline(Guid surveyId, Guid participantId,
        [FromForm] DeclineParticipantDto dto, IFormFile? attachment)
    {
        var userId = GetCurrentUserId();
        var success = await _distributionService.DeclineParticipantAsync(participantId, dto, attachment, userId);
        if (!success) return NotFound(new { message = "Participant not found" });
        return Ok(new { message = "Participant marked as declined" });
    }

    [HttpPut("{participantId:guid}/reset")]
    public async Task<IActionResult> ResetStatus(Guid surveyId, Guid participantId)
    {
        var success = await _distributionService.ResetParticipantStatusAsync(participantId);
        if (!success) return NotFound(new { message = "Participant not found" });
        return Ok(new { message = "Participant status reset" });
    }

    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate()
    {
        var bytes = await _distributionService.GetParticipantTemplateAsync();
        return File(bytes, "text/csv", "participant_template.csv");
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out var userId) ? userId : 0;
    }
}
