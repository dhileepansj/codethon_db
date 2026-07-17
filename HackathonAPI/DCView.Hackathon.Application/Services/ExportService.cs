using DCView.Hackathon.Shared.Helpers;
using System.IO.Compression;
using System.Text;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.Application.Services;

public class ExportService : IExportService
{
    private readonly IUserRepository _userRepo;
    private readonly IFileRepository _fileRepo;
    private readonly IFolderRepository _folderRepo;
    private readonly IExecutionHistoryRepository _historyRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly ISubmissionFileRepository _submissionFileRepo;

    public ExportService(
        IUserRepository userRepo,
        IFileRepository fileRepo,
        IFolderRepository folderRepo,
        IExecutionHistoryRepository historyRepo,
        ISessionRepository sessionRepo,
        ISubmissionFileRepository submissionFileRepo)
    {
        _userRepo = userRepo;
        _fileRepo = fileRepo;
        _folderRepo = folderRepo;
        _historyRepo = historyRepo;
        _sessionRepo = sessionRepo;
        _submissionFileRepo = submissionFileRepo;
    }

    public async Task<byte[]> ExportUserAsync(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");

        // Check if there's anything to export
        var files = await _fileRepo.GetAllByUserIdAsync(user.Id);
        var queryCount = await _historyRepo.GetTotalCountByUserIdAsync(user.Id);

        if (!files.Any() && queryCount == 0)
            throw new InvalidOperationException($"No files or execution history found for user '{userId}'. Nothing to export.");

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            await AddUserToArchiveAsync(archive, user.Id, user.UserID, "");
        }

        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportAllAsync()
    {
        var users = await _userRepo.GetAllParticipantsAsync();

        if (!users.Any())
            throw new InvalidOperationException("No participants found. Nothing to export.");

        // Check if at least one user has content
        bool hasContent = false;
        foreach (var u in users)
        {
            var files = await _fileRepo.GetAllByUserIdAsync(u.Id);
            var queryCount = await _historyRepo.GetTotalCountByUserIdAsync(u.Id);
            if (files.Any() || queryCount > 0) { hasContent = true; break; }
        }

        if (!hasContent)
            throw new InvalidOperationException("No files or execution history found for any participant. Nothing to export.");

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Master summary CSV
            var summaryBuilder = new StringBuilder();
            summaryBuilder.AppendLine("UserID,FullName,DatabaseCreated,TotalQueries,TotalFiles");

            foreach (var user in users)
            {
                var session = await _sessionRepo.GetByUserIdAsync(user.Id);
                var files = await _fileRepo.GetAllByUserIdAsync(user.Id);
                var queryCount = await _historyRepo.GetTotalCountByUserIdAsync(user.Id);

                summaryBuilder.AppendLine($"{user.UserID},{user.FullName ?? ""},{"  "}{(session?.DatabaseCreated == true ? "Yes" : "No")},{queryCount},{files.Count()}");

                await AddUserToArchiveAsync(archive, user.Id, user.UserID, $"{user.UserID}/");
            }

            var summaryEntry = archive.CreateEntry("master_summary.csv");
            using var summaryWriter = new StreamWriter(summaryEntry.Open());
            await summaryWriter.WriteAsync(summaryBuilder.ToString());
        }

        return memoryStream.ToArray();
    }

    private async Task AddUserToArchiveAsync(ZipArchive archive, int userInternalId, string userId, string prefix)
    {
        // Get all files
        var files = await _fileRepo.GetAllByUserIdAsync(userInternalId);
        var folders = await _folderRepo.GetAllByUserIdAsync(userInternalId);

        // Build folder path lookup
        var folderPaths = BuildFolderPaths(folders);

        // Add script files organized by folder
        foreach (var file in files)
        {
            string folderPath = file.FolderId.HasValue && folderPaths.ContainsKey(file.FolderId.Value)
                ? folderPaths[file.FolderId.Value]
                : "Scripts";

            string entryPath = $"{prefix}scripts/{folderPath}/{file.FileName}";
            if (!entryPath.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                entryPath += ".sql";

            var entry = archive.CreateEntry(entryPath);
            using var writer = new StreamWriter(entry.Open());
            await writer.WriteAsync(file.Content ?? "-- Empty file");
        }

        // Add submission files (Word/Excel uploads)
        await AddSubmissionFilesToArchiveAsync(archive, userInternalId, prefix);

        // Add execution history CSV
        var (historyItems, _) = await _historyRepo.GetByUserIdAsync(userInternalId, 1, 10000);
        if (historyItems.Any())
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Timestamp,QueryType,Status,Duration(ms),RowsAffected,Query");

            foreach (var h in historyItems)
            {
                var queryEscaped = $"\"{h.QueryText.Replace("\"", "\"\"")}\"";
                csvBuilder.AppendLine($"{h.ExecutedAt:yyyy-MM-dd HH:mm:ss},{h.QueryType},{h.Status},{h.DurationMs},{h.RowsAffected},{queryEscaped}");
            }

            var historyEntry = archive.CreateEntry($"{prefix}execution_history.csv");
            using var historyWriter = new StreamWriter(historyEntry.Open());
            await historyWriter.WriteAsync(csvBuilder.ToString());
        }

        // Add summary JSON
        var session = await _sessionRepo.GetByUserIdAsync(userInternalId);
        var totalQueries = await _historyRepo.GetTotalCountByUserIdAsync(userInternalId);
        var summaryJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            userId,
            databaseName = session?.DatabaseName ?? "N/A",
            databaseCreated = session?.DatabaseCreated ?? false,
            sessionStarted = session?.StartedAt,
            totalQueries,
            totalFiles = files.Count(),
            exportDate = DateTimeHelper.Now
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        var summaryEntry2 = archive.CreateEntry($"{prefix}summary.json");
        using var summaryWriter2 = new StreamWriter(summaryEntry2.Open());
        await summaryWriter2.WriteAsync(summaryJson);
    }

    private static Dictionary<int, string> BuildFolderPaths(IEnumerable<Domain.Entities.UserFolder> folders)
    {
        var folderDict = folders.ToDictionary(f => f.FolderId);
        var paths = new Dictionary<int, string>();

        foreach (var folder in folders)
        {
            paths[folder.FolderId] = BuildPath(folder.FolderId, folderDict);
        }

        return paths;
    }

    private static string BuildPath(int folderId, Dictionary<int, Domain.Entities.UserFolder> folderDict)
    {
        var parts = new List<string>();
        var current = folderId;
        var visited = new HashSet<int>();

        while (folderDict.ContainsKey(current) && !visited.Contains(current))
        {
            visited.Add(current);
            parts.Insert(0, folderDict[current].FolderName);
            if (folderDict[current].ParentFolderId.HasValue)
                current = folderDict[current].ParentFolderId.Value;
            else
                break;
        }

        return string.Join("/", parts);
    }

    private async Task AddSubmissionFilesToArchiveAsync(ZipArchive archive, int userInternalId, string prefix)
    {
        var submissionFiles = await _submissionFileRepo.GetByUserIdAsync(userInternalId);

        foreach (var sf in submissionFiles)
        {
            var entry = archive.CreateEntry($"{prefix}submissions/{sf.FileName}");
            using var stream = entry.Open();
            await stream.WriteAsync(sf.FileData);
        }
    }
}

