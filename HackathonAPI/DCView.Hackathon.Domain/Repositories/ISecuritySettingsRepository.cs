using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface ISecuritySettingsRepository
{
    Task<SecuritySettings> GetAsync();
    Task UpdateAsync(SecuritySettings settings);
}
