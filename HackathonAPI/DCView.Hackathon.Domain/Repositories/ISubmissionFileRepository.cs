using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface ISubmissionFileRepository
{
    Task<IEnumerable<UserSubmissionFile>> GetByUserIdAsync(int userId);
    Task SaveChangesAsync(CancellationToken ct = default);
}
