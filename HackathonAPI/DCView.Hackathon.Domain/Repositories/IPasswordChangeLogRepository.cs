using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IPasswordChangeLogRepository
{
    Task<IEnumerable<PasswordChangeLog>> GetByUserIdAsync(int userId, int count);
    Task CreateAsync(PasswordChangeLog log);
}
