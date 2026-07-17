using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.API.Controllers;

[Route("api/schema")]
[ApiController]
[Authorize(Roles = "Participant,SuperAdmin")]
public class SchemaController : ControllerBase
{
    private readonly ISchemaExplorerService _schemaService;

    public SchemaController(ISchemaExplorerService schemaService) => _schemaService = schemaService;

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var result = await _schemaService.GetOverviewAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables()
    {
        var result = await _schemaService.GetTablesAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("tables/{tableName}/columns")]
    public async Task<IActionResult> GetTableColumns(string tableName)
    {
        var result = await _schemaService.GetTableColumnsAsync(GetUserId(), tableName);
        return Ok(result);
    }

    [HttpGet("tables/{tableName}/indexes")]
    public async Task<IActionResult> GetTableIndexes(string tableName)
    {
        var result = await _schemaService.GetTableIndexesAsync(GetUserId(), tableName);
        return Ok(result);
    }

    [HttpGet("tables/{tableName}/data")]
    public async Task<IActionResult> GetTableData(string tableName, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 25;
        var result = await _schemaService.GetTableDataAsync(GetUserId(), tableName, page, pageSize);
        return Ok(result);
    }

    [HttpGet("views")]
    public async Task<IActionResult> GetViews()
    {
        var result = await _schemaService.GetViewsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("views/{viewName}/definition")]
    public async Task<IActionResult> GetViewDefinition(string viewName)
    {
        var result = await _schemaService.GetViewDefinitionAsync(GetUserId(), viewName);
        if (result == null) return NotFound(new { message = "View not found or has no definition" });
        return Ok(new { name = viewName, definition = result });
    }

    [HttpGet("procedures")]
    public async Task<IActionResult> GetProcedures()
    {
        var result = await _schemaService.GetProceduresAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("procedures/{procName}/definition")]
    public async Task<IActionResult> GetProcedureDefinition(string procName)
    {
        var result = await _schemaService.GetProcedureDefinitionAsync(GetUserId(), procName);
        if (result == null) return NotFound(new { message = "Procedure not found or has no definition" });
        return Ok(new { name = procName, definition = result });
    }

    [HttpGet("functions")]
    public async Task<IActionResult> GetFunctions()
    {
        var result = await _schemaService.GetFunctionsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("functions/{funcName}/definition")]
    public async Task<IActionResult> GetFunctionDefinition(string funcName)
    {
        var result = await _schemaService.GetFunctionDefinitionAsync(GetUserId(), funcName);
        if (result == null) return NotFound(new { message = "Function not found or has no definition" });
        return Ok(new { name = funcName, definition = result });
    }

    [HttpGet("triggers")]
    public async Task<IActionResult> GetTriggers()
    {
        var result = await _schemaService.GetTriggersAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("triggers/{triggerName}/definition")]
    public async Task<IActionResult> GetTriggerDefinition(string triggerName)
    {
        var result = await _schemaService.GetTriggerDefinitionAsync(GetUserId(), triggerName);
        if (result == null) return NotFound(new { message = "Trigger not found or has no definition" });
        return Ok(new { name = triggerName, definition = result });
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
