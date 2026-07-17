using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IAiDetectionSettingsRepository
{
    Task<AiDetectionSettings> GetGlobalSettingsAsync();
    Task UpdateGlobalSettingsAsync(AiDetectionSettings settings);
    Task<AiDetectionUserOverride?> GetUserOverrideAsync(int userId);
    Task<IEnumerable<AiDetectionUserOverride>> GetAllUserOverridesAsync();
    Task SetUserOverrideAsync(AiDetectionUserOverride overrideSettings);
    Task RemoveUserOverrideAsync(int userId);
    Task SaveChangesAsync(CancellationToken ct = default);
}
