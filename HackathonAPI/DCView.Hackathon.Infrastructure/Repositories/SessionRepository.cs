using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly HackathonDbContext _db;

    public SessionRepository(HackathonDbContext db) => _db = db;

    public async Task<HackathonSession?> GetByUserIdAsync(int userId)
        => await _db.Sessions.FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task<HackathonSession> CreateAsync(HackathonSession session)
    {
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task UpdateAsync(HackathonSession session)
    {
        _db.Sessions.Update(session);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(HackathonSession session)
    {
        _db.Sessions.Remove(session);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<HackathonSession>> GetAllActiveSessionsAsync()
        => await _db.Sessions
            .Include(s => s.User)
            .Where(s => s.IsActive)
            .ToListAsync();

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
