using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class SubmissionFileRepository : ISubmissionFileRepository
{
    private readonly HackathonDbContext _db;

    public SubmissionFileRepository(HackathonDbContext db) => _db = db;

    public async Task<IEnumerable<UserSubmissionFile>> GetByUserIdAsync(int userId)
        => await _db.SubmissionFiles.Where(f => f.UserId == userId).ToListAsync();

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
