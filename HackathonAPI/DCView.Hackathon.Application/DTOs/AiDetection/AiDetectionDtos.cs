namespace DCView.Hackathon.Application.DTOs.AiDetection;

// ─── Detection Log ────────────────────────────────────────────

public class AiDetectionLogDto
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string? LoginId { get; set; }
    public string? FullName { get; set; }
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public string DetectionResult { get; set; } = string.Empty;
    public string? Reasoning { get; set; }
    public int ContentLength { get; set; }
    public int ContentDelta { get; set; }
    public bool TabSwitchBeforeSave { get; set; }
    public string? ModelUsed { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public DateTime AnalyzedDate { get; set; }
}

// ─── Save Check Result ────────────────────────────────────────

public class AiSaveCheckResult
{
    public bool IsBlocked { get; set; }
    public int ConfidenceScore { get; set; }
    public string DetectionResult { get; set; } = "Human";
    public string? Reasoning { get; set; }
    public string? BlockMessage { get; set; }
    public long? BlockedSaveId { get; set; }
}

// ─── Ollama Response ──────────────────────────────────────────

public class OllamaResponse
{
    public string Model { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public bool Done { get; set; }
}

public class AiDetectionResult
{
    public int Score { get; set; }
    public string Result { get; set; } = "Uncertain";
    public string Reasoning { get; set; } = string.Empty;
}

// ─── Settings DTOs ────────────────────────────────────────────

public class AiDetectionSettingsDto
{
    public string Mode { get; set; } = "AllowAndMark";
    public int BlockThreshold { get; set; } = 70;
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public List<UserOverrideDto> UserOverrides { get; set; } = new();
}

public class UpdateAiSettingsDto
{
    public string Mode { get; set; } = "AllowAndMark"; // Block, AllowAndMark, Disabled
    public int BlockThreshold { get; set; } = 70;
}

public class UpdateUserOverrideDto
{
    public string? Mode { get; set; } // Block, AllowAndMark, Disabled
    public int? BlockThreshold { get; set; }
}

public class UserOverrideDto
{
    public int UserId { get; set; }
    public string? LoginId { get; set; }
    public string? FullName { get; set; }
    public string? Mode { get; set; }
    public int? BlockThreshold { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}

// ─── Blocked Save DTOs ────────────────────────────────────────

public class BlockedSaveDto
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string? LoginId { get; set; }
    public string? FullName { get; set; }
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? AttemptedContent { get; set; }
    public int ConfidenceScore { get; set; }
    public string? Reasoning { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? AdminRemarks { get; set; }
    public DateTime BlockedDate { get; set; }
}

public class ReviewBlockedSaveDto
{
    public string? Remarks { get; set; }
}
