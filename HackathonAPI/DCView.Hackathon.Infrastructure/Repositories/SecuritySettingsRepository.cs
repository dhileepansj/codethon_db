using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class SecuritySettingsRepository : ISecuritySettingsRepository
{
    private readonly HackathonDbContext _db;

    public SecuritySettingsRepository(HackathonDbContext db) => _db = db;

    public async Task<SecuritySettings> GetAsync()
    {
        var settings = await _db.SecuritySettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            // Create default settings if none exist
            settings = new SecuritySettings
            {
                MinLength = 8,
                MaxLength = 64,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigit = true,
                RequireSpecialChar = true,
                PasswordHistoryCount = 5,
                MaxFailedLoginAttempts = 5,
                LockoutDurationMinutes = 15,
                PasswordExpiryDays = 0,
                MaxConcurrentSessions = 1,
                ModifiedDate = DateTime.UtcNow
            };
            _db.SecuritySettings.Add(settings);
            await _db.SaveChangesAsync();
        }
        return settings;
    }

    public async Task UpdateAsync(SecuritySettings settings)
    {
        _db.SecuritySettings.Update(settings);
        await _db.SaveChangesAsync();
    }
}
