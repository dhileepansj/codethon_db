using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.API.Controllers;

[Route("api/surveys/{surveyId:guid}/fields")]
[ApiController]
[Authorize]
public class SurveyFieldController : ControllerBase
{
    private readonly ISurveyFormBuilderService _builderService;

    public SurveyFieldController(ISurveyFormBuilderService builderService)
    {
        _builderService = builderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFields(Guid surveyId)
    {
        var fields = await _builderService.GetFieldsAsync(surveyId);
        return Ok(fields);
    }

    [HttpPost]
    public async Task<IActionResult> CreateField(Guid surveyId, [FromBody] CreateFieldDto dto)
    {
        var field = await _builderService.CreateFieldAsync(surveyId, dto);
        return Ok(field);
    }

    [HttpPut("{fieldId:guid}")]
    public async Task<IActionResult> UpdateField(Guid surveyId, Guid fieldId, [FromBody] UpdateFieldDto dto)
    {
        var field = await _builderService.UpdateFieldAsync(fieldId, dto);
        if (field == null) return NotFound(new { message = "Field not found" });
        return Ok(field);
    }

    [HttpDelete("{fieldId:guid}")]
    public async Task<IActionResult> DeleteField(Guid surveyId, Guid fieldId)
    {
        var success = await _builderService.DeleteFieldAsync(fieldId);
        if (!success) return NotFound(new { message = "Field not found" });
        return Ok(new { message = "Field deleted" });
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderFields(Guid surveyId, [FromBody] ReorderFieldsDto dto)
    {
        var success = await _builderService.ReorderFieldsAsync(surveyId, dto);
        return Ok(new { message = "Fields reordered" });
    }

    // Dependencies
    [HttpPost("{fieldId:guid}/dependencies")]
    public async Task<IActionResult> CreateDependency(Guid surveyId, Guid fieldId, [FromBody] CreateDependencyDto dto)
    {
        var dependency = await _builderService.CreateDependencyAsync(fieldId, dto);
        return Ok(dependency);
    }

    [HttpDelete("dependencies/{dependencyId:guid}")]
    public async Task<IActionResult> DeleteDependency(Guid surveyId, Guid dependencyId)
    {
        var success = await _builderService.DeleteDependencyAsync(dependencyId);
        if (!success) return NotFound(new { message = "Dependency not found" });
        return Ok(new { message = "Dependency deleted" });
    }

    [HttpGet("dependencies")]
    public async Task<IActionResult> GetAllDependencies(Guid surveyId)
    {
        var deps = await _builderService.GetDependenciesBySurveyAsync(surveyId);
        return Ok(deps);
    }

    [HttpGet("dependencies/validate")]
    public async Task<IActionResult> ValidateDependencies(Guid surveyId)
    {
        var error = await _builderService.ValidateDependencies(surveyId);
        if (error != null) return BadRequest(new { message = error });
        return Ok(new { message = "No circular dependencies found" });
    }
}
