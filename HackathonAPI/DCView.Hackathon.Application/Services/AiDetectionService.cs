using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DCView.Hackathon.Application.DTOs.AiDetection;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Shared.Helpers;

namespace DCView.Hackathon.Application.Services;

public class AiDetectionService : IAiDetectionService
{
    private readonly IAiDetectionLogRepository _detectionRepo;
    private readonly IAiDetectionSettingsRepository _settingsRepo;
    private readonly IAiBlockedSaveRepository _blockedRepo;
    private readonly ITabSwitchLogRepository _tabSwitchRepo;
    private readonly IFileRepository _fileRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiDetectionService> _logger;

    public AiDetectionService(
        IAiDetectionLogRepository detectionRepo,
        IAiDetectionSettingsRepository settingsRepo,
        IAiBlockedSaveRepository blockedRepo,
        ITabSwitchLogRepository tabSwitchRepo,
        IFileRepository fileRepo,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AiDetectionService> logger)
    {
        _detectionRepo = detectionRepo;
        _settingsRepo = settingsRepo;
        _blockedRepo = blockedRepo;
        _tabSwitchRepo = tabSwitchRepo;
        _fileRepo = fileRepo;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // ─── Main Save Check ──────────────────────────────────────────

    public async Task<AiSaveCheckResult> CheckAndProcessSaveAsync(
        int userId, int fileId, string fileName, string? content, string? previousContent)
    {
        // Skip if content is empty or too short
        if (string.IsNullOrWhiteSpace(content) || content.Length < 50)
            return new AiSaveCheckResult { IsBlocked = false, DetectionResult = "Skipped" };

        // Get effective settings for this user
        var (mode, threshold) = await GetEffectiveSettingsAsync(userId);

        // If disabled, allow immediately
        if (mode == "Disabled")
            return new AiSaveCheckResult { IsBlocked = false, DetectionResult = "Disabled" };

        // Check if this content was previously approved by admin
        var contentHash = ComputeContentHash(content);
        var isApproved = await IsContentApprovedAsync(userId, fileId, content);
        if (isApproved)
            return new AiSaveCheckResult { IsBlocked = false, DetectionResult = "AdminApproved" };

        // Call Ollama for AI detection
        var sw = Stopwatch.StartNew();
        var detectionResult = await CallOllamaForDetection(content);
        sw.Stop();

        // Check tab-switch context
        var recentSwitches = await _tabSwitchRepo.GetByUserIdAsync(userId, 5);
        var tabSwitchBeforeSave = recentSwitches.Any(s =>
            s.EventType == "tab_hidden" &&
            s.EventTime >= DateTimeHelper.Now.AddMinutes(-2));

        var contentDelta = (content?.Length ?? 0) - (previousContent?.Length ?? 0);

        // Log the detection
        var log = new AiDetectionLog
        {
            UserId = userId,
            FileId = fileId,
            FileName = fileName,
            ConfidenceScore = detectionResult.Score,
            DetectionResult = detectionResult.Result,
            Reasoning = detectionResult.Reasoning,
            ContentLength = content.Length,
            ContentDelta = contentDelta,
            TabSwitchBeforeSave = tabSwitchBeforeSave,
            ModelUsed = _configuration["AiDetection:Model"] ?? "MiniMax-M1",
            ProcessingTimeMs = (int)sw.ElapsedMilliseconds,
            AnalyzedDate = DateTimeHelper.Now
        };
        await _detectionRepo.CreateAsync(log);

        // Decision: should we block?
        if (detectionResult.Score >= threshold && mode == "Block")
        {
            // Block the save
            var blockedSave = new AiBlockedSave
            {
                UserId = userId,
                FileId = fileId,
                FileName = fileName,
                AttemptedContent = content,
                ConfidenceScore = detectionResult.Score,
                Reasoning = detectionResult.Reasoning,
                Status = "Pending",
                BlockedDate = DateTimeHelper.Now
            };
            await _blockedRepo.CreateAsync(blockedSave);

            return new AiSaveCheckResult
            {
                IsBlocked = true,
                ConfidenceScore = detectionResult.Score,
                DetectionResult = detectionResult.Result,
                Reasoning = detectionResult.Reasoning,
                BlockMessage = "Your file save was blocked due to suspected AI-generated content. An admin has been notified and will review your submission.",
                BlockedSaveId = blockedSave.Id
            };
        }

        // AllowAndMark or below threshold — allow the save
        return new AiSaveCheckResult
        {
            IsBlocked = false,
            ConfidenceScore = detectionResult.Score,
            DetectionResult = detectionResult.Result,
            Reasoning = detectionResult.Reasoning
        };
    }

    // ─── Settings ─────────────────────────────────────────────────

    public async Task<AiDetectionSettingsDto> GetSettingsAsync()
    {
        var global = await _settingsRepo.GetGlobalSettingsAsync();
        var overrides = await _settingsRepo.GetAllUserOverridesAsync();

        return new AiDetectionSettingsDto
        {
            Mode = global.Mode,
            BlockThreshold = global.BlockThreshold,
            ModifiedDate = global.ModifiedDate,
            ModifiedBy = global.ModifiedBy,
            UserOverrides = overrides.Select(o => new UserOverrideDto
            {
                UserId = o.UserId,
                LoginId = o.User?.UserID,
                FullName = o.User?.FullName,
                Mode = o.Mode,
                BlockThreshold = o.BlockThreshold,
                ModifiedDate = o.ModifiedDate,
                ModifiedBy = o.ModifiedBy
            }).ToList()
        };
    }

    public async Task UpdateGlobalSettingsAsync(UpdateAiSettingsDto request, string modifiedBy)
    {
        var settings = new AiDetectionSettings
        {
            Id = 1,
            Mode = request.Mode,
            BlockThreshold = request.BlockThreshold,
            ModifiedDate = DateTimeHelper.Now,
            ModifiedBy = modifiedBy
        };
        await _settingsRepo.UpdateGlobalSettingsAsync(settings);
    }

    public async Task SetUserOverrideAsync(int userId, UpdateUserOverrideDto request, string modifiedBy)
    {
        var overrideEntity = new AiDetectionUserOverride
        {
            UserId = userId,
            Mode = request.Mode,
            BlockThreshold = request.BlockThreshold,
            ModifiedDate = DateTimeHelper.Now,
            ModifiedBy = modifiedBy
        };
        await _settingsRepo.SetUserOverrideAsync(overrideEntity);
    }

    public async Task RemoveUserOverrideAsync(int userId)
    {
        await _settingsRepo.RemoveUserOverrideAsync(userId);
    }

    public async Task<IEnumerable<UserOverrideDto>> GetAllUserOverridesAsync()
    {
        var overrides = await _settingsRepo.GetAllUserOverridesAsync();
        return overrides.Select(o => new UserOverrideDto
        {
            UserId = o.UserId,
            LoginId = o.User?.UserID,
            FullName = o.User?.FullName,
            Mode = o.Mode,
            BlockThreshold = o.BlockThreshold,
            ModifiedDate = o.ModifiedDate,
            ModifiedBy = o.ModifiedBy
        });
    }

    // ─── Blocked Saves ────────────────────────────────────────────

    public async Task<IEnumerable<BlockedSaveDto>> GetPendingBlockedSavesAsync()
    {
        var items = await _blockedRepo.GetPendingAsync();
        return items.Select(MapBlockedSaveToDto);
    }

    public async Task<IEnumerable<BlockedSaveDto>> GetAllBlockedSavesAsync()
    {
        var items = await _blockedRepo.GetAllAsync();
        return items.Select(MapBlockedSaveToDto);
    }

    public async Task<IEnumerable<BlockedSaveDto>> GetBlockedSavesByUserAsync(int userId)
    {
        var items = await _blockedRepo.GetByUserIdAsync(userId);
        return items.Select(MapBlockedSaveToDto);
    }

    public async Task<bool> ApproveBlockedSaveAsync(long blockedSaveId, string adminUser, string? remarks)
    {
        var blocked = await _blockedRepo.GetByIdAsync(blockedSaveId);
        if (blocked == null || blocked.Status != "Pending") return false;

        blocked.Status = "Approved";
        blocked.ReviewedBy = adminUser;
        blocked.ReviewedDate = DateTimeHelper.Now;
        blocked.AdminRemarks = remarks;
        await _blockedRepo.UpdateAsync(blocked);

        // Auto-save the content to the file
        if (!string.IsNullOrEmpty(blocked.AttemptedContent))
        {
            var file = await _fileRepo.GetByIdAsync(blocked.FileId);
            if (file != null)
            {
                file.Content = blocked.AttemptedContent;
                await _fileRepo.UpdateAsync(file);
            }
        }

        return true;
    }

    public async Task<bool> RejectBlockedSaveAsync(long blockedSaveId, string adminUser, string? remarks)
    {
        var blocked = await _blockedRepo.GetByIdAsync(blockedSaveId);
        if (blocked == null || blocked.Status != "Pending") return false;

        blocked.Status = "Rejected";
        blocked.ReviewedBy = adminUser;
        blocked.ReviewedDate = DateTimeHelper.Now;
        blocked.AdminRemarks = remarks;
        await _blockedRepo.UpdateAsync(blocked);

        return true;
    }

    // ─── Logs ─────────────────────────────────────────────────────

    public async Task<IEnumerable<AiDetectionLogDto>> GetLogsByUserIdAsync(int userId)
    {
        var logs = await _detectionRepo.GetByUserIdAsync(userId);
        return logs.Select(MapLogToDto);
    }

    public async Task<IEnumerable<AiDetectionLogDto>> GetFlaggedAsync(int minScore = 60)
    {
        var logs = await _detectionRepo.GetFlaggedLogsAsync(minScore);
        return logs.Select(l => new AiDetectionLogDto
        {
            Id = l.Id,
            UserId = l.UserId,
            LoginId = l.User?.UserID,
            FullName = l.User?.FullName,
            FileId = l.FileId,
            FileName = l.FileName,
            ConfidenceScore = l.ConfidenceScore,
            DetectionResult = l.DetectionResult,
            Reasoning = l.Reasoning,
            ContentLength = l.ContentLength,
            ContentDelta = l.ContentDelta,
            TabSwitchBeforeSave = l.TabSwitchBeforeSave,
            ModelUsed = l.ModelUsed,
            ProcessingTimeMs = l.ProcessingTimeMs,
            AnalyzedDate = l.AnalyzedDate
        });
    }

    // ─── Private Helpers ──────────────────────────────────────────

    private async Task<(string Mode, int Threshold)> GetEffectiveSettingsAsync(int userId)
    {
        var global = await _settingsRepo.GetGlobalSettingsAsync();
        var userOverride = await _settingsRepo.GetUserOverrideAsync(userId);

        var mode = userOverride?.Mode ?? global.Mode;
        var threshold = userOverride?.BlockThreshold ?? global.BlockThreshold;

        return (mode, threshold);
    }

    private async Task<bool> IsContentApprovedAsync(int userId, int fileId, string content)
    {
        // Check if there's an approved blocked save for this exact file with matching content
        var userBlocked = await _blockedRepo.GetByUserIdAsync(userId);
        return userBlocked.Any(b =>
            b.FileId == fileId &&
            b.Status == "Approved" &&
            b.AttemptedContent == content);
    }

    private static string ComputeContentHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    private async Task<AiDetectionResult> CallOllamaForDetection(string content)
    {
        var baseUrl = _configuration["AiDetection:OllamaUrl"] ?? "https://ebszivalos.novactech.net/ollama";

        // Get model list — supports both "Models" (array) and legacy "Model" (single string)
        var models = _configuration.GetSection("AiDetection:Models").Get<string[]>()
            ?? new[] { _configuration["AiDetection:Model"] ?? "MiniMax-M1" };

        // Truncate content if too long
        var analysisContent = content.Length > 1500 ? content[..1500] : content;

        var prompt = $@"You are an AI-generated code detector. Analyze the following SQL code and determine if it was likely written by an AI tool (like ChatGPT, Copilot, etc.) or by a human.

Consider these signals:
- AI-generated SQL tends to have very consistent formatting, perfect indentation, template-like comments
- AI code often has verbose, overly descriptive naming and uniform comment blocks
- Human-written code during a timed hackathon often has typos, inconsistent spacing, shorthand names, and incremental patterns
- AI code is usually complete and well-structured in a single block
- Human code under time pressure has shortcuts, partial implementations, and less polished formatting

SQL Code to analyze:
```sql
{analysisContent}
```

Respond ONLY in this exact JSON format (no other text):
{{""score"": <0-100>, ""result"": ""<AI|Human|Uncertain>"", ""reasoning"": ""<brief explanation>""}}

Where score is 0 (definitely human) to 100 (definitely AI).";

        // Try each model in order — fallback on failure
        for (int m = 0; m < models.Length; m++)
        {
            var model = models[m];
            try
            {
                var client = _httpClientFactory.CreateClient("Ollama");

                // Add API key if configured
                var apiKey = _configuration["AiDetection:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }

                var requestBody = new
                {
                    model = model,
                    prompt = prompt,
                    stream = false
                };

                var requestUrl = $"{baseUrl}/api/generate";
                _logger.LogInformation("AI Detection — Calling Ollama: URL={Url}, Model={Model} ({ModelIndex}/{Total}), ContentLength={Length}",
                    requestUrl, model, m + 1, models.Length, analysisContent.Length);

                var response = await client.PostAsJsonAsync(requestUrl, requestBody);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("AI Detection — Ollama Response: Model={Model}, StatusCode={StatusCode}",
                    model, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Model '{Model}' returned {StatusCode}. Response: {Body}",
                        model, (int)response.StatusCode, responseBody.Length > 300 ? responseBody[..300] : responseBody);

                    // If there are more models to try, continue to next
                    if (m < models.Length - 1)
                    {
                        _logger.LogInformation("Falling back to next model: {NextModel}", models[m + 1]);
                        continue;
                    }

                    // All models failed
                    return new AiDetectionResult { Score = 0, Result = "Uncertain", Reasoning = $"All AI models unavailable — save allowed (fail-open)." };
                }

                OllamaResponse? ollamaResponse;
                try
                {
                    ollamaResponse = System.Text.Json.JsonSerializer.Deserialize<OllamaResponse>(responseBody,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize response from model '{Model}': {Body}", model, responseBody.Length > 300 ? responseBody[..300] : responseBody);
                    if (m < models.Length - 1) continue;
                    return new AiDetectionResult { Score = 0, Result = "Uncertain", Reasoning = "Failed to parse AI model response." };
                }

                if (ollamaResponse == null || string.IsNullOrWhiteSpace(ollamaResponse.Response))
                {
                    _logger.LogWarning("Model '{Model}' returned empty response.", model);
                    if (m < models.Length - 1) continue;
                    return new AiDetectionResult { Score = 0, Result = "Uncertain", Reasoning = "Empty response from AI model — save allowed." };
                }

                _logger.LogInformation("AI Detection — Success with model '{Model}'", model);
                return ParseDetectionResponse(ollamaResponse.Response);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Model '{Model}' timed out.", model);
                if (m < models.Length - 1) continue;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Model '{Model}' connection failed.", model);
                if (m < models.Length - 1) continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error with model '{Model}'.", model);
                if (m < models.Length - 1) continue;
            }
        }

        // All models exhausted
        _logger.LogWarning("All {Count} AI models failed — allowing save (fail-open)", models.Length);
        return new AiDetectionResult { Score = 0, Result = "Uncertain", Reasoning = "All AI models unavailable — save allowed (fail-open)." };
    }

    private static AiDetectionResult ParseDetectionResponse(string responseText)
    {
        try
        {
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = responseText[jsonStart..(jsonEnd + 1)];
                var parsed = JsonSerializer.Deserialize<JsonElement>(jsonStr);

                var score = parsed.GetProperty("score").GetInt32();
                var result = parsed.GetProperty("result").GetString() ?? "Uncertain";
                var reasoning = parsed.GetProperty("reasoning").GetString() ?? "";

                score = Math.Clamp(score, 0, 100);

                if (result != "AI" && result != "Human" && result != "Uncertain")
                    result = score >= 60 ? "AI" : score <= 30 ? "Human" : "Uncertain";

                return new AiDetectionResult { Score = score, Result = result, Reasoning = reasoning };
            }
        }
        catch { /* Parse failed */ }

        return new AiDetectionResult { Score = 0, Result = "Uncertain", Reasoning = "Could not parse AI model response." };
    }

    private static BlockedSaveDto MapBlockedSaveToDto(AiBlockedSave b) => new()
    {
        Id = b.Id,
        UserId = b.UserId,
        LoginId = b.User?.UserID,
        FullName = b.User?.FullName,
        FileId = b.FileId,
        FileName = b.FileName,
        AttemptedContent = b.AttemptedContent,
        ConfidenceScore = b.ConfidenceScore,
        Reasoning = b.Reasoning,
        Status = b.Status,
        ReviewedBy = b.ReviewedBy,
        ReviewedDate = b.ReviewedDate,
        AdminRemarks = b.AdminRemarks,
        BlockedDate = b.BlockedDate
    };

    private static AiDetectionLogDto MapLogToDto(AiDetectionLog l) => new()
    {
        Id = l.Id,
        UserId = l.UserId,
        FileId = l.FileId,
        FileName = l.FileName,
        ConfidenceScore = l.ConfidenceScore,
        DetectionResult = l.DetectionResult,
        Reasoning = l.Reasoning,
        ContentLength = l.ContentLength,
        ContentDelta = l.ContentDelta,
        TabSwitchBeforeSave = l.TabSwitchBeforeSave,
        ModelUsed = l.ModelUsed,
        ProcessingTimeMs = l.ProcessingTimeMs,
        AnalyzedDate = l.AnalyzedDate
    };
}
