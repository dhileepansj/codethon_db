using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Repositories;

public interface IHackathonConfigRepository
{
    Task<HackathonConfig?> GetActiveConfigAsync();
    Task<HackathonConfig?> GetActiveConfigAsync(DbEngineType engineType);
    Task<HackathonConfig> CreateOrUpdateAsync(HackathonConfig config);
    Task SaveChangesAsync(CancellationToken ct = default);
}
