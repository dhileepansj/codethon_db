using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.API.Controllers;

[Route("api/surveys/{surveyId:guid}/dashboard")]
[ApiController]
[Authorize]
public class SurveyDashboardController : ControllerBase
{
    private readonly ISurveyDashboardService _dashboardService;

    public SurveyDashboardController(ISurveyDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard(Guid surveyId)
    {
        var dashboard = await _dashboardService.GetDashboardAsync(surveyId);
        if (dashboard == null) return NotFound(new { message = "Survey not found" });
        return Ok(dashboard);
    }

    [HttpGet("responses")]
    public async Task<IActionResult> GetResponses(Guid surveyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var responses = await _dashboardService.GetResponsesAsync(surveyId, page, pageSize);
        return Ok(responses);
    }

    [HttpGet("responses/{responseId:guid}")]
    public async Task<IActionResult> GetResponseDetail(Guid surveyId, Guid responseId)
    {
        var response = await _dashboardService.GetResponseDetailAsync(responseId);
        if (response == null) return NotFound(new { message = "Response not found" });
        return Ok(response);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(Guid surveyId)
    {
        var analytics = await _dashboardService.GetAnalyticsAsync(surveyId);
        return Ok(analytics);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(Guid surveyId)
    {
        var bytes = await _dashboardService.ExportResponsesAsync(surveyId);
        return File(bytes, "text/csv", $"survey_responses_{surveyId:N}.csv");
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel(Guid surveyId)
    {
        var bytes = await _dashboardService.ExportResponsesExcelAsync(surveyId);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"survey_responses_{surveyId:N}.xlsx");
    }
}
