using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IFolderRepository
{
    Task<UserFolder?> GetByIdAsync(int folderId);
    Task<IEnumerable<UserFolder>> GetByUserIdAsync(int userId, int? parentFolderId);
    Task<IEnumerable<UserFolder>> GetAllByUserIdAsync(int userId);
    Task<UserFolder> CreateAsync(UserFolder folder);
    Task UpdateAsync(UserFolder folder);
    Task DeleteCascadeAsync(int folderId);
    Task SaveChangesAsync(CancellationToken ct = default);
}
