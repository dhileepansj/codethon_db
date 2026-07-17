using DCView.Hackathon.Application.DTOs.Admin;

namespace DCView.Hackathon.Application.Interfaces;

public interface ISessionService
{
    Task<bool> ActivateSessionAsync(string userId, int? durationMinutes, string activatedBy);
    Task<bool> DeactivateSessionAsync(string userId);
    Task<bool> ExtendSessionAsync(string userId, int additionalMinutes);
    Task<bool> ResetDatabaseAsync(string userId);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}
