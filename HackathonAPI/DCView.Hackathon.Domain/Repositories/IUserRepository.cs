using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUserIDAsync(string userId);
    Task<IEnumerable<User>> GetAllParticipantsAsync();
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> ExistsAsync(string userId);
    Task SaveChangesAsync(CancellationToken ct = default);
}
