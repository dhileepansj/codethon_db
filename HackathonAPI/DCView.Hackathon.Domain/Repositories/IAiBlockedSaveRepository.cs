using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IAiBlockedSaveRepository
{
    Task CreateAsync(AiBlockedSave blockedSave);
    Task<AiBlockedSave?> GetByIdAsync(long id);
    Task<IEnumerable<AiBlockedSave>> GetPendingAsync();
    Task<IEnumerable<AiBlockedSave>> GetByUserIdAsync(int userId);
    Task<IEnumerable<AiBlockedSave>> GetAllAsync();

    /// <summary>
    /// Check if admin has already approved this specific file+content combo.
    /// </summary>
    Task<bool> IsApprovedAsync(int userId, int fileId, string contentHash);

    Task UpdateAsync(AiBlockedSave blockedSave);
    Task SaveChangesAsync(CancellationToken ct = default);
}
