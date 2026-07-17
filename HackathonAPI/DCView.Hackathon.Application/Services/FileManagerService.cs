using DCView.Hackathon.Shared.Helpers;
using DCView.Hackathon.Application.DTOs.FileManager;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.Application.Services;

public class FileManagerService : IFileManagerService
{
    private readonly IFileRepository _fileRepo;
    private readonly IFolderRepository _folderRepo;

    public FileManagerService(IFileRepository fileRepo, IFolderRepository folderRepo)
    {
        _fileRepo = fileRepo;
        _folderRepo = folderRepo;
    }

    // ─── Folders ──────────────────────────────────────────────────

    public async Task<IEnumerable<FolderDto>> GetFoldersAsync(int userId, int? parentFolderId)
    {
        var folders = await _folderRepo.GetByUserIdAsync(userId, parentFolderId);
        var result = new List<FolderDto>();

        foreach (var f in folders)
        {
            // Get counts
            var subFolders = await _folderRepo.GetByUserIdAsync(userId, f.FolderId);
            var files = await _fileRepo.GetByUserIdAsync(userId, f.FolderId);

            result.Add(new FolderDto
            {
                FolderId = f.FolderId,
                ParentFolderId = f.ParentFolderId,
                FolderName = f.FolderName,
                CreatedDate = f.CreatedDate,
                SubFolderCount = subFolders.Count(),
                FileCount = files.Count()
            });
        }

        return result;
    }

    public async Task<FolderDto> CreateFolderAsync(int userId, CreateFolderDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FolderName))
            throw new InvalidOperationException("Folder name is required.");

        var folder = new UserFolder
        {
            UserId = userId,
            ParentFolderId = request.ParentFolderId,
            FolderName = request.FolderName.Trim(),
            CreatedDate = DateTimeHelper.Now
        };

        await _folderRepo.CreateAsync(folder);

        return new FolderDto
        {
            FolderId = folder.FolderId,
            ParentFolderId = folder.ParentFolderId,
            FolderName = folder.FolderName,
            CreatedDate = folder.CreatedDate,
            SubFolderCount = 0,
            FileCount = 0
        };
    }

    public async Task<bool> RenameFolderAsync(int userId, int folderId, string newName)
    {
        var folder = await _folderRepo.GetByIdAsync(folderId);
        if (folder == null || folder.UserId != userId) return false;

        folder.FolderName = newName.Trim();
        await _folderRepo.UpdateAsync(folder);
        return true;
    }

    public async Task<bool> DeleteFolderAsync(int userId, int folderId)
    {
        var folder = await _folderRepo.GetByIdAsync(folderId);
        if (folder == null || folder.UserId != userId) return false;

        await _folderRepo.DeleteCascadeAsync(folderId);
        return true;
    }

    // ─── Files ────────────────────────────────────────────────────

    public async Task<IEnumerable<FileListItemDto>> GetFilesAsync(int userId, int? folderId)
    {
        var files = await _fileRepo.GetByUserIdAsync(userId, folderId);
        return files.Select(MapToListItem);
    }

    public async Task<FileDetailDto?> GetFileByIdAsync(int userId, int fileId)
    {
        var file = await _fileRepo.GetByIdAsync(fileId);
        if (file == null || file.UserId != userId) return null;

        return new FileDetailDto
        {
            FileId = file.FileId,
            FolderId = file.FolderId,
            FileName = file.FileName,
            FileType = file.FileType,
            Content = file.Content,
            CreatedDate = file.CreatedDate,
            ModifiedDate = file.ModifiedDate
        };
    }

    public async Task<FileListItemDto> CreateFileAsync(int userId, CreateFileDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new InvalidOperationException("File name is required.");

        // Check for duplicate file name in the same folder
        var exists = await _fileRepo.ExistsByNameAsync(userId, request.FileName.Trim(), request.FolderId);
        if (exists)
            throw new InvalidOperationException($"A file named '{request.FileName.Trim()}' already exists in this folder.");

        var file = new UserFile
        {
            UserId = userId,
            FolderId = request.FolderId,
            FileName = request.FileName.Trim(),
            FileType = request.FileType ?? "Script",
            Content = request.Content,
            CreatedDate = DateTimeHelper.Now
        };

        await _fileRepo.CreateAsync(file);
        return MapToListItem(file);
    }

    public async Task<bool> UpdateFileAsync(int userId, int fileId, UpdateFileDto request)
    {
        var file = await _fileRepo.GetByIdAsync(fileId);
        if (file == null || file.UserId != userId) return false;

        // Check for duplicate name if renaming
        if (request.FileName != null && !string.Equals(file.FileName, request.FileName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _fileRepo.ExistsByNameAsync(userId, request.FileName.Trim(), file.FolderId);
            if (exists)
                throw new InvalidOperationException($"A file named '{request.FileName.Trim()}' already exists in this folder.");
        }

        if (request.FileName != null) file.FileName = request.FileName.Trim();
        if (request.FileType != null) file.FileType = request.FileType;
        if (request.Content != null) file.Content = request.Content;

        await _fileRepo.UpdateAsync(file);
        return true;
    }

    public async Task<bool> DeleteFileAsync(int userId, int fileId)
    {
        var file = await _fileRepo.GetByIdAsync(fileId);
        if (file == null || file.UserId != userId) return false;

        await _fileRepo.DeleteAsync(fileId);
        return true;
    }

    public async Task<bool> MoveFileAsync(int userId, int fileId, int? targetFolderId)
    {
        var file = await _fileRepo.GetByIdAsync(fileId);
        if (file == null || file.UserId != userId) return false;

        file.FolderId = targetFolderId;
        await _fileRepo.UpdateAsync(file);
        return true;
    }

    public async Task<IEnumerable<FileListItemDto>> GetAllFilesByUserIdAsync(int userId)
    {
        var files = await _fileRepo.GetAllByUserIdAsync(userId);
        return files.Select(f => new FileListItemDto
        {
            FileId = f.FileId,
            FolderId = f.FolderId,
            FolderPath = f.Folder?.FolderName,
            FileName = f.FileName,
            FileType = f.FileType,
            CreatedDate = f.CreatedDate,
            ModifiedDate = f.ModifiedDate
        });
    }

    private static FileListItemDto MapToListItem(UserFile file) => new()
    {
        FileId = file.FileId,
        FolderId = file.FolderId,
        FileName = file.FileName,
        FileType = file.FileType,
        CreatedDate = file.CreatedDate,
        ModifiedDate = file.ModifiedDate
    };
}
