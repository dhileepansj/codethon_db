using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IAiDetectionLogRepository
{
    Task CreateAsync(AiDetectionLog log);
    Task<IEnumerable<AiDetectionLog>> GetByUserIdAsync(int userId);
    Task<IEnumerable<AiDetectionLog>> GetByFileIdAsync(int fileId);
    Task<AiDetectionLog?> GetLatestByFileIdAsync(int fileId);
    Task<IEnumerable<AiDetectionLog>> GetFlaggedLogsAsync(int minConfidenceScore = 60);
    Task SaveChangesAsync(CancellationToken ct = default);
}
