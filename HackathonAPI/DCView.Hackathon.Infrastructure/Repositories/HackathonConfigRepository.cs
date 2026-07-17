using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class HackathonConfigRepository : IHackathonConfigRepository
{
    private readonly HackathonDbContext _db;

    public HackathonConfigRepository(HackathonDbContext db) => _db = db;

    public async Task<HackathonConfig?> GetActiveConfigAsync()
        => await _db.HackathonConfigs.FirstOrDefaultAsync(c => c.IsActive);

    public async Task<HackathonConfig> CreateOrUpdateAsync(HackathonConfig config)
    {
        var existing = await _db.HackathonConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (existing != null)
        {
            existing.ServerName = config.ServerName;
            existing.AdminUserId = config.AdminUserId;
            existing.AdminPasswordEncrypted = config.AdminPasswordEncrypted;
            existing.DbPrefix = config.DbPrefix;
            existing.MaxQueryTimeoutSeconds = config.MaxQueryTimeoutSeconds;
            existing.MaxRowsPerPage = config.MaxRowsPerPage;
            _db.HackathonConfigs.Update(existing);
            await _db.SaveChangesAsync();
            return existing;
        }

        _db.HackathonConfigs.Add(config);
        await _db.SaveChangesAsync();
        return config;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
