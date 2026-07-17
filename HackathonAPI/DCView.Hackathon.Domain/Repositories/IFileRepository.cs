using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IFileRepository
{
    Task<UserFile?> GetByIdAsync(int fileId);
    Task<IEnumerable<UserFile>> GetByUserIdAsync(int userId, int? folderId);
    Task<IEnumerable<UserFile>> GetAllByUserIdAsync(int userId);
    Task<bool> ExistsByNameAsync(int userId, string fileName, int? folderId);
    Task<UserFile> CreateAsync(UserFile file);
    Task UpdateAsync(UserFile file);
    Task DeleteAsync(int fileId);
    Task SaveChangesAsync(CancellationToken ct = default);
}
