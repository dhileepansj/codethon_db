using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly HackathonDbContext _db;

    public UserRepository(HackathonDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(int id)
        => await _db.Users.Include(u => u.Session).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByUserIDAsync(string userId)
        => await _db.Users.Include(u => u.Session).FirstOrDefaultAsync(u => u.UserID == userId);

    public async Task<IEnumerable<User>> GetAllParticipantsAsync()
        => await _db.Users
            .Include(u => u.Session)
            .Where(u => u.Role == "Participant")
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync();

    public async Task<User> CreateAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string userId)
        => await _db.Users.AnyAsync(u => u.UserID == userId);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
