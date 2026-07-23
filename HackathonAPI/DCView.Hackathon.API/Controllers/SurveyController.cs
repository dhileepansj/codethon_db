using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.API.Controllers;

[Route("api/surveys")]
[ApiController]
[Authorize]
public class SurveyController : ControllerBase
{
    private readonly ISurveyService _surveyService;

    public SurveyController(ISurveyService surveyService)
    {
        _surveyService = surveyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var surveys = await _surveyService.GetAllSurveysAsync();
        return Ok(surveys);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var survey = await _surveyService.GetSurveyByIdAsync(id);
        if (survey == null) return NotFound(new { message = "Survey not found" });
        return Ok(survey);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSurveyDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var survey = await _surveyService.CreateSurveyAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = survey.Id }, survey);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSurveyDto dto)
    {
        var survey = await _surveyService.UpdateSurveyAsync(id, dto);
        if (survey == null) return NotFound(new { message = "Survey not found" });
        return Ok(survey);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _surveyService.DeleteSurveyAsync(id);
        if (!success) return NotFound(new { message = "Survey not found" });
        return Ok(new { message = "Survey deleted" });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSurveyStatusDto dto)
    {
        var survey = await _surveyService.UpdateStatusAsync(id, dto);
        if (survey == null) return NotFound(new { message = "Survey not found" });
        return Ok(survey);
    }

    [HttpPost("{id:guid}/clone")]
    public async Task<IActionResult> Clone(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var survey = await _surveyService.CloneSurveyAsync(id, userId);
        if (survey == null) return NotFound(new { message = "Survey not found" });
        return Ok(survey);
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out var userId) ? userId : 0;
    }
}
