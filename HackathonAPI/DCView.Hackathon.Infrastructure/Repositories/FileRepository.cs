using DCView.Hackathon.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class FileRepository : IFileRepository
{
    private readonly HackathonDbContext _db;

    public FileRepository(HackathonDbContext db) => _db = db;

    public async Task<UserFile?> GetByIdAsync(int fileId)
        => await _db.UserFiles.FindAsync(fileId);

    public async Task<IEnumerable<UserFile>> GetByUserIdAsync(int userId, int? folderId)
        => await _db.UserFiles
            .Where(f => f.UserId == userId && f.FolderId == folderId)
            .OrderBy(f => f.FileName)
            .ToListAsync();

    public async Task<IEnumerable<UserFile>> GetAllByUserIdAsync(int userId)
        => await _db.UserFiles
            .Include(f => f.Folder)
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.FileName)
            .ToListAsync();

    public async Task<bool> ExistsByNameAsync(int userId, string fileName, int? folderId)
        => await _db.UserFiles
            .AnyAsync(f => f.UserId == userId
                && f.FolderId == folderId
                && f.FileName.ToLower() == fileName.ToLower());

    public async Task<UserFile> CreateAsync(UserFile file)
    {
        _db.UserFiles.Add(file);
        await _db.SaveChangesAsync();
        return file;
    }

    public async Task UpdateAsync(UserFile file)
    {
        file.ModifiedDate = DateTimeHelper.Now;
        _db.UserFiles.Update(file);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int fileId)
    {
        var file = await _db.UserFiles.FindAsync(fileId);
        if (file != null)
        {
            _db.UserFiles.Remove(file);
            await _db.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}

