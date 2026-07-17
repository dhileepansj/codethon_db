using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly HackathonDbContext _db;

    public FolderRepository(HackathonDbContext db) => _db = db;

    public async Task<UserFolder?> GetByIdAsync(int folderId)
        => await _db.UserFolders
            .Include(f => f.SubFolders)
            .Include(f => f.Files)
            .FirstOrDefaultAsync(f => f.FolderId == folderId);

    public async Task<IEnumerable<UserFolder>> GetByUserIdAsync(int userId, int? parentFolderId)
        => await _db.UserFolders
            .Where(f => f.UserId == userId && f.ParentFolderId == parentFolderId)
            .OrderBy(f => f.FolderName)
            .ToListAsync();

    public async Task<IEnumerable<UserFolder>> GetAllByUserIdAsync(int userId)
        => await _db.UserFolders
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.FolderName)
            .ToListAsync();

    public async Task<UserFolder> CreateAsync(UserFolder folder)
    {
        _db.UserFolders.Add(folder);
        await _db.SaveChangesAsync();
        return folder;
    }

    public async Task UpdateAsync(UserFolder folder)
    {
        _db.UserFolders.Update(folder);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteCascadeAsync(int folderId)
    {
        var folder = await _db.UserFolders
            .Include(f => f.Files)
            .Include(f => f.SubFolders)
            .FirstOrDefaultAsync(f => f.FolderId == folderId);

        if (folder == null) return;

        // Recursively delete sub-folders
        foreach (var sub in folder.SubFolders.ToList())
        {
            await DeleteCascadeAsync(sub.FolderId);
        }

        _db.UserFiles.RemoveRange(folder.Files);
        _db.UserFolders.Remove(folder);
        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
