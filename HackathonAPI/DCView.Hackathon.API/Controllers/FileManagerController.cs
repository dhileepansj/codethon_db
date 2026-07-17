using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Application.DTOs.FileManager;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.API.Controllers;

[Route("api/files")]
[ApiController]
[Authorize(Roles = "Participant,SuperAdmin")]
public class FileManagerController : ControllerBase
{
    private readonly IFileManagerService _fileService;
    private readonly IAiDetectionService _aiDetectionService;

    public FileManagerController(IFileManagerService fileService, IAiDetectionService aiDetectionService)
    {
        _fileService = fileService;
        _aiDetectionService = aiDetectionService;
    }

    // ─── Folders ──────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetRoot()
    {
        var userId = GetUserId();
        var folders = await _fileService.GetFoldersAsync(userId, null);
        var files = await _fileService.GetFilesAsync(userId, null);
        return Ok(new { folders, files });
    }

    [HttpGet("folder/{folderId:int}")]
    public async Task<IActionResult> GetFolderContents(int folderId)
    {
        var userId = GetUserId();
        var folders = await _fileService.GetFoldersAsync(userId, folderId);
        var files = await _fileService.GetFilesAsync(userId, folderId);
        return Ok(new { folders, files });
    }

    [HttpPost("folder")]
    public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto request)
    {
        var folder = await _fileService.CreateFolderAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetFolderContents), new { folderId = folder.FolderId }, folder);
    }

    [HttpPut("folder/{folderId:int}")]
    public async Task<IActionResult> RenameFolder(int folderId, [FromBody] RenameFolderDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FolderName))
            return BadRequest(new { message = "Folder name is required" });

        var success = await _fileService.RenameFolderAsync(GetUserId(), folderId, request.FolderName);
        if (!success) return NotFound(new { message = "Folder not found" });
        return Ok(new { message = "Folder renamed successfully" });
    }

    [HttpDelete("folder/{folderId:int}")]
    public async Task<IActionResult> DeleteFolder(int folderId)
    {
        var success = await _fileService.DeleteFolderAsync(GetUserId(), folderId);
        if (!success) return NotFound(new { message = "Folder not found" });
        return Ok(new { message = "Folder deleted successfully" });
    }

    // ─── Files ────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> CreateFile([FromBody] CreateFileDto request)
    {
        var file = await _fileService.CreateFileAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetFile), new { fileId = file.FileId }, file);
    }

    [HttpGet("{fileId:int}")]
    public async Task<IActionResult> GetFile(int fileId)
    {
        var file = await _fileService.GetFileByIdAsync(GetUserId(), fileId);
        if (file == null) return NotFound(new { message = "File not found" });
        return Ok(file);
    }

    [HttpPut("{fileId:int}")]
    public async Task<IActionResult> UpdateFile(int fileId, [FromBody] UpdateFileDto request)
    {
        var userId = GetUserId();

        // AI Detection check — only for content updates
        if (request.Content != null)
        {
            try
            {
                // Get previous content for comparison
                var existingFile = await _fileService.GetFileByIdAsync(userId, fileId);
                if (existingFile == null) return NotFound(new { message = "File not found" });

                var aiResult = await _aiDetectionService.CheckAndProcessSaveAsync(
                    userId, fileId, existingFile.FileName, request.Content, existingFile.Content);

                if (aiResult.IsBlocked)
                {
                    return StatusCode(403, new
                    {
                        message = aiResult.BlockMessage,
                        blocked = true,
                        confidenceScore = aiResult.ConfidenceScore,
                        detectionResult = aiResult.DetectionResult,
                        reasoning = aiResult.Reasoning,
                        blockedSaveId = aiResult.BlockedSaveId
                    });
                }
            }
            catch
            {
                // AI detection failed — fail-open, allow the save to proceed
            }
        }

        var success = await _fileService.UpdateFileAsync(userId, fileId, request);
        if (!success) return NotFound(new { message = "File not found" });
        return Ok(new { message = "File saved successfully" });
    }

    [HttpDelete("{fileId:int}")]
    public async Task<IActionResult> DeleteFile(int fileId)
    {
        var success = await _fileService.DeleteFileAsync(GetUserId(), fileId);
        if (!success) return NotFound(new { message = "File not found" });
        return Ok(new { message = "File deleted successfully" });
    }

    [HttpPut("{fileId:int}/move")]
    public async Task<IActionResult> MoveFile(int fileId, [FromBody] MoveFileDto request)
    {
        var success = await _fileService.MoveFileAsync(GetUserId(), fileId, request.TargetFolderId);
        if (!success) return NotFound(new { message = "File not found" });
        return Ok(new { message = "File moved successfully" });
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public class RenameFolderDto
{
    public string FolderName { get; set; } = string.Empty;
}
