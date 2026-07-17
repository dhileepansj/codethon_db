using DCView.Hackathon.Application.DTOs.FileManager;

namespace DCView.Hackathon.Application.Interfaces;

public interface IFileManagerService
{
    // Folders
    Task<IEnumerable<FolderDto>> GetFoldersAsync(int userId, int? parentFolderId);
    Task<FolderDto> CreateFolderAsync(int userId, CreateFolderDto request);
    Task<bool> RenameFolderAsync(int userId, int folderId, string newName);
    Task<bool> DeleteFolderAsync(int userId, int folderId);

    // Files
    Task<IEnumerable<FileListItemDto>> GetFilesAsync(int userId, int? folderId);
    Task<FileDetailDto?> GetFileByIdAsync(int userId, int fileId);
    Task<FileListItemDto> CreateFileAsync(int userId, CreateFileDto request);
    Task<bool> UpdateFileAsync(int userId, int fileId, UpdateFileDto request);
    Task<bool> DeleteFileAsync(int userId, int fileId);
    Task<bool> MoveFileAsync(int userId, int fileId, int? targetFolderId);

    // For admin/export
    Task<IEnumerable<FileListItemDto>> GetAllFilesByUserIdAsync(int userId);
}
