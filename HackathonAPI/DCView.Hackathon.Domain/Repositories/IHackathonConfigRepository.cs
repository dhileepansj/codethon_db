using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IHackathonConfigRepository
{
    Task<HackathonConfig?> GetActiveConfigAsync();
    Task<HackathonConfig> CreateOrUpdateAsync(HackathonConfig config);
    Task SaveChangesAsync(CancellationToken ct = default);
}
