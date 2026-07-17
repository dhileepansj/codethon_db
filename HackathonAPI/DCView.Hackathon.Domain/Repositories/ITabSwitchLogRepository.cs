using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface ITabSwitchLogRepository
{
    Task CreateAsync(TabSwitchLog log);
    Task CreateBatchAsync(IEnumerable<TabSwitchLog> logs);
    Task<int> GetCountByUserInLastHourAsync(int userId);
    Task<IEnumerable<TabSwitchLog>> GetByUserIdAsync(int userId, int limit = 100);
    Task<Dictionary<int, int>> GetSwitchCountsForActiveUsersAsync();
    Task SaveChangesAsync(CancellationToken ct = default);
}
