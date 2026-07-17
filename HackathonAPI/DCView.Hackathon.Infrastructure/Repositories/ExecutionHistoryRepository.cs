using DCView.Hackathon.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class ExecutionHistoryRepository : IExecutionHistoryRepository
{
    private readonly HackathonDbContext _db;

    public ExecutionHistoryRepository(HackathonDbContext db) => _db = db;

    public async Task<ExecutionHistory> CreateAsync(ExecutionHistory entry)
    {
        _db.ExecutionHistories.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<(IEnumerable<ExecutionHistory> Items, int TotalCount)> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        var query = _db.ExecutionHistories.Where(e => e.UserId == userId);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<ExecutionHistory> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = _db.ExecutionHistories.Include(e => e.User);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<int> GetTotalCountByUserIdAsync(int userId)
        => await _db.ExecutionHistories.CountAsync(e => e.UserId == userId);

    public async Task<int> GetTodayCountAsync()
    {
        var today = DateTimeHelper.Now.Date;
        return await _db.ExecutionHistories.CountAsync(e => e.ExecutedAt >= today);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}

