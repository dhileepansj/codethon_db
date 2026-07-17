using DCView.Hackathon.Application.DTOs.AiDetection;

namespace DCView.Hackathon.Application.Interfaces;

public interface IAiDetectionService
{
    /// <summary>
    /// Analyzes file content. Returns null if save is allowed, or a BlockedSaveResult if blocked.
    /// </summary>
    Task<AiSaveCheckResult> CheckAndProcessSaveAsync(int userId, int fileId, string fileName, string? content, string? previousContent);

    /// <summary>
    /// Gets all AI detection logs for a specific user (Admin use).
    /// </summary>
    Task<IEnumerable<AiDetectionLogDto>> GetLogsByUserIdAsync(int userId);

    /// <summary>
    /// Gets all flagged files above a confidence threshold (Admin use).
    /// </summary>
    Task<IEnumerable<AiDetectionLogDto>> GetFlaggedAsync(int minScore = 60);

    // ─── Settings ─────────────────────────────────────────────
    Task<AiDetectionSettingsDto> GetSettingsAsync();
    Task UpdateGlobalSettingsAsync(UpdateAiSettingsDto request, string modifiedBy);
    Task SetUserOverrideAsync(int userId, UpdateUserOverrideDto request, string modifiedBy);
    Task RemoveUserOverrideAsync(int userId);
    Task<IEnumerable<UserOverrideDto>> GetAllUserOverridesAsync();

    // ─── Blocked Saves ────────────────────────────────────────
    Task<IEnumerable<BlockedSaveDto>> GetPendingBlockedSavesAsync();
    Task<IEnumerable<BlockedSaveDto>> GetAllBlockedSavesAsync();
    Task<IEnumerable<BlockedSaveDto>> GetBlockedSavesByUserAsync(int userId);
    Task<bool> ApproveBlockedSaveAsync(long blockedSaveId, string adminUser, string? remarks);
    Task<bool> RejectBlockedSaveAsync(long blockedSaveId, string adminUser, string? remarks);
}
