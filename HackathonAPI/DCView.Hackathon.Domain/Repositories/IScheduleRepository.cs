using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IScheduleRepository
{
    Task<HackathonSchedule?> GetActiveScheduleAsync();
    Task<HackathonSchedule?> GetByIdAsync(int id);
    Task<HackathonSchedule> CreateOrUpdateAsync(HackathonSchedule schedule);
    Task AddBreakAsync(HackathonBreak breakItem);
    Task RemoveBreakAsync(int breakId);
    Task UpdateExtensionAsync(int scheduleId, int additionalMinutes);
}
