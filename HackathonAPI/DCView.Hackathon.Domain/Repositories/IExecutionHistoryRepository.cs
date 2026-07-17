using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IExecutionHistoryRepository
{
    Task<ExecutionHistory> CreateAsync(ExecutionHistory entry);
    Task<(IEnumerable<ExecutionHistory> Items, int TotalCount)> GetByUserIdAsync(int userId, int page, int pageSize);
    Task<(IEnumerable<ExecutionHistory> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountByUserIdAsync(int userId);
    Task<int> GetTodayCountAsync();
    Task SaveChangesAsync(CancellationToken ct = default);
}
