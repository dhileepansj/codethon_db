using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class AiDetectionSettingsRepository : IAiDetectionSettingsRepository
{
    private readonly HackathonDbContext _db;

    public AiDetectionSettingsRepository(HackathonDbContext db) => _db = db;

    public async Task<AiDetectionSettings> GetGlobalSettingsAsync()
    {
        var settings = await _db.AiDetectionSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            // Create default settings
            settings = new AiDetectionSettings
            {
                Mode = "AllowAndMark",
                BlockThreshold = 70
            };
            _db.AiDetectionSettings.Add(settings);
            await _db.SaveChangesAsync();
        }
        return settings;
    }

    public async Task UpdateGlobalSettingsAsync(AiDetectionSettings settings)
    {
        var existing = await _db.AiDetectionSettings.FirstOrDefaultAsync();
        if (existing == null)
        {
            _db.AiDetectionSettings.Add(settings);
        }
        else
        {
            existing.Mode = settings.Mode;
            existing.BlockThreshold = settings.BlockThreshold;
            existing.ModifiedDate = settings.ModifiedDate;
            existing.ModifiedBy = settings.ModifiedBy;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<AiDetectionUserOverride?> GetUserOverrideAsync(int userId)
        => await _db.AiDetectionUserOverrides.FirstOrDefaultAsync(o => o.UserId == userId);

    public async Task<IEnumerable<AiDetectionUserOverride>> GetAllUserOverridesAsync()
        => await _db.AiDetectionUserOverrides.Include(o => o.User).ToListAsync();

    public async Task SetUserOverrideAsync(AiDetectionUserOverride overrideSettings)
    {
        var existing = await _db.AiDetectionUserOverrides.FirstOrDefaultAsync(o => o.UserId == overrideSettings.UserId);
        if (existing == null)
        {
            _db.AiDetectionUserOverrides.Add(overrideSettings);
        }
        else
        {
            existing.Mode = overrideSettings.Mode;
            existing.BlockThreshold = overrideSettings.BlockThreshold;
            existing.ModifiedDate = overrideSettings.ModifiedDate;
            existing.ModifiedBy = overrideSettings.ModifiedBy;
        }
        await _db.SaveChangesAsync();
    }

    public async Task RemoveUserOverrideAsync(int userId)
    {
        var existing = await _db.AiDetectionUserOverrides.FirstOrDefaultAsync(o => o.UserId == userId);
        if (existing != null)
        {
            _db.AiDetectionUserOverrides.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
