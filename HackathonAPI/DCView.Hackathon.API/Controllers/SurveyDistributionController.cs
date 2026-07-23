using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.API.Controllers;

[Route("api/surveys/{surveyId:guid}/distribution")]
[ApiController]
[Authorize]
public class SurveyDistributionController : ControllerBase
{
    private readonly ISurveyDistributionService _distributionService;

    public SurveyDistributionController(ISurveyDistributionService distributionService)
    {
        _distributionService = distributionService;
    }

    [HttpGet("email-settings")]
    public async Task<IActionResult> GetEmailSettings(Guid surveyId)
    {
        var settings = await _distributionService.GetEmailSettingsAsync(surveyId);
        if (settings == null)
            return Ok(new UpdateEmailSettingsDto()); // Return defaults
        return Ok(settings);
    }

    [HttpPut("email-settings")]
    public async Task<IActionResult> UpdateEmailSettings(Guid surveyId, [FromBody] UpdateEmailSettingsDto dto)
    {
        var settings = await _distributionService.UpdateEmailSettingsAsync(surveyId, dto);
        return Ok(settings);
    }

    [HttpPost("send")]
    public async Task<IActionResult> Distribute(Guid surveyId)
    {
        try
        {
            var sentCount = await _distributionService.DistributeAsync(surveyId);
            return Ok(new { message = $"Emails sent to {sentCount} participant(s)", sentCount });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("remind")]
    public async Task<IActionResult> SendReminder(Guid surveyId, [FromBody] SendReminderDto dto)
    {
        try
        {
            var sentCount = await _distributionService.SendReminderAsync(surveyId, dto);
            return Ok(new { message = $"Reminders sent to {sentCount} participant(s)", sentCount });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
