using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class PasswordChangeLogRepository : IPasswordChangeLogRepository
{
    private readonly HackathonDbContext _db;

    public PasswordChangeLogRepository(HackathonDbContext db) => _db = db;

    public async Task<IEnumerable<PasswordChangeLog>> GetByUserIdAsync(int userId, int count)
        => await _db.PasswordChangeLogs
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.ChangedAt)
            .Take(count)
            .ToListAsync();

    public async Task CreateAsync(PasswordChangeLog log)
    {
        _db.PasswordChangeLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
