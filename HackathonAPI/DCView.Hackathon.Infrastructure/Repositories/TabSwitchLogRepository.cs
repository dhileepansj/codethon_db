using DCView.Hackathon.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class TabSwitchLogRepository : ITabSwitchLogRepository
{
    private readonly HackathonDbContext _db;

    public TabSwitchLogRepository(HackathonDbContext db) => _db = db;

    public async Task CreateAsync(TabSwitchLog log)
    {
        _db.TabSwitchLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task CreateBatchAsync(IEnumerable<TabSwitchLog> logs)
    {
        _db.TabSwitchLogs.AddRange(logs);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetCountByUserInLastHourAsync(int userId)
    {
        var oneHourAgo = DateTimeHelper.Now.AddHours(-1);
        return await _db.TabSwitchLogs
            .CountAsync(l => l.UserId == userId && l.EventType == "tab_hidden" && l.EventTime >= oneHourAgo);
    }

    public async Task<IEnumerable<TabSwitchLog>> GetByUserIdAsync(int userId, int limit = 100)
    {
        return await _db.TabSwitchLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.EventTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetSwitchCountsForActiveUsersAsync()
    {
        var oneHourAgo = DateTimeHelper.Now.AddHours(-1);
        return await _db.TabSwitchLogs
            .Where(l => l.EventType == "tab_hidden" && l.EventTime >= oneHourAgo)
            .GroupBy(l => l.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}

