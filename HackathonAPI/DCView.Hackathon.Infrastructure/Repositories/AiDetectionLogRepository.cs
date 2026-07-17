using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class AiDetectionLogRepository : IAiDetectionLogRepository
{
    private readonly HackathonDbContext _db;

    public AiDetectionLogRepository(HackathonDbContext db) => _db = db;

    public async Task CreateAsync(AiDetectionLog log)
    {
        _db.AiDetectionLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<AiDetectionLog>> GetByUserIdAsync(int userId)
        => await _db.AiDetectionLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.AnalyzedDate)
            .ToListAsync();

    public async Task<IEnumerable<AiDetectionLog>> GetByFileIdAsync(int fileId)
        => await _db.AiDetectionLogs
            .Where(l => l.FileId == fileId)
            .OrderByDescending(l => l.AnalyzedDate)
            .ToListAsync();

    public async Task<AiDetectionLog?> GetLatestByFileIdAsync(int fileId)
        => await _db.AiDetectionLogs
            .Where(l => l.FileId == fileId)
            .OrderByDescending(l => l.AnalyzedDate)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<AiDetectionLog>> GetFlaggedLogsAsync(int minConfidenceScore = 60)
        => await _db.AiDetectionLogs
            .Include(l => l.User)
            .Include(l => l.File)
            .Where(l => l.ConfidenceScore >= minConfidenceScore)
            .OrderByDescending(l => l.ConfidenceScore)
            .ThenByDescending(l => l.AnalyzedDate)
            .ToListAsync();

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
