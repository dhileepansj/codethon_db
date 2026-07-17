using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class AiBlockedSaveRepository : IAiBlockedSaveRepository
{
    private readonly HackathonDbContext _db;

    public AiBlockedSaveRepository(HackathonDbContext db) => _db = db;

    public async Task CreateAsync(AiBlockedSave blockedSave)
    {
        _db.AiBlockedSaves.Add(blockedSave);
        await _db.SaveChangesAsync();
    }

    public async Task<AiBlockedSave?> GetByIdAsync(long id)
        => await _db.AiBlockedSaves
            .Include(b => b.User)
            .Include(b => b.File)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IEnumerable<AiBlockedSave>> GetPendingAsync()
        => await _db.AiBlockedSaves
            .Include(b => b.User)
            .Include(b => b.File)
            .Where(b => b.Status == "Pending")
            .OrderByDescending(b => b.BlockedDate)
            .ToListAsync();

    public async Task<IEnumerable<AiBlockedSave>> GetByUserIdAsync(int userId)
        => await _db.AiBlockedSaves
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BlockedDate)
            .ToListAsync();

    public async Task<IEnumerable<AiBlockedSave>> GetAllAsync()
        => await _db.AiBlockedSaves
            .Include(b => b.User)
            .Include(b => b.File)
            .OrderByDescending(b => b.BlockedDate)
            .ToListAsync();

    public async Task<bool> IsApprovedAsync(int userId, int fileId, string contentHash)
        => await _db.AiBlockedSaves
            .AnyAsync(b => b.UserId == userId
                && b.FileId == fileId
                && b.Status == "Approved"
                && b.AttemptedContent != null
                && b.AttemptedContent.Length.ToString() == contentHash);

    public async Task UpdateAsync(AiBlockedSave blockedSave)
    {
        _db.AiBlockedSaves.Update(blockedSave);
        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
