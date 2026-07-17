using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly HackathonDbContext _db;

    public ScheduleRepository(HackathonDbContext db) => _db = db;

    public async Task<HackathonSchedule?> GetActiveScheduleAsync()
        => await _db.HackathonSchedules
            .Include(s => s.Breaks)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.ScheduleDate)
            .FirstOrDefaultAsync();

    public async Task<HackathonSchedule?> GetByIdAsync(int id)
        => await _db.HackathonSchedules
            .Include(s => s.Breaks)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<HackathonSchedule> CreateOrUpdateAsync(HackathonSchedule schedule)
    {
        if (schedule.Id == 0)
        {
            _db.HackathonSchedules.Add(schedule);
        }
        else
        {
            _db.HackathonSchedules.Update(schedule);
        }
        await _db.SaveChangesAsync();
        return schedule;
    }

    public async Task AddBreakAsync(HackathonBreak breakItem)
    {
        _db.HackathonBreaks.Add(breakItem);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveBreakAsync(int breakId)
    {
        var item = await _db.HackathonBreaks.FindAsync(breakId);
        if (item != null)
        {
            _db.HackathonBreaks.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateExtensionAsync(int scheduleId, int additionalMinutes)
    {
        var schedule = await _db.HackathonSchedules.FindAsync(scheduleId);
        if (schedule != null)
        {
            schedule.ExtensionMinutes += additionalMinutes;
            await _db.SaveChangesAsync();
        }
    }
}
