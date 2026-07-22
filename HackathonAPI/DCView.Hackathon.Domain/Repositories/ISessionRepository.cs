using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface ISessionRepository
{
    Task<HackathonSession?> GetByUserIdAsync(int userId);
    Task<HackathonSession> CreateAsync(HackathonSession session);
    Task UpdateAsync(HackathonSession session);
    Task DeleteAsync(HackathonSession session);
    Task<IEnumerable<HackathonSession>> GetAllActiveSessionsAsync();
    Task SaveChangesAsync(CancellationToken ct = default);
}
