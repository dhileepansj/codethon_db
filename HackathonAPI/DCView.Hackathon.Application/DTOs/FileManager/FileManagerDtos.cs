namespace DCView.Hackathon.Application.DTOs.FileManager;

public class FolderDto
{
    public int FolderId { get; set; }
    public int? ParentFolderId { get; set; }
    public string FolderName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public int FileCount { get; set; }
    public int SubFolderCount { get; set; }
}

public class CreateFolderDto
{
    public int? ParentFolderId { get; set; }
    public string FolderName { get; set; } = string.Empty;
}

public class FileListItemDto
{
    public int FileId { get; set; }
    public int? FolderId { get; set; }
    public string? FolderPath { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class FileDetailDto
{
    public int FileId { get; set; }
    public int? FolderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class CreateFileDto
{
    public int? FolderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = "Script";
    public string? Content { get; set; }
}

public class UpdateFileDto
{
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public string? Content { get; set; }
}

public class MoveFileDto
{
    public int? TargetFolderId { get; set; }
}
