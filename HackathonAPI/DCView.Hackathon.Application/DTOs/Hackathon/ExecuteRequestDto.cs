namespace DCView.Hackathon.Application.DTOs.Hackathon;

public class ExecuteRequestDto
{
    public string Sql { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class ExecuteResultDto
{
    public List<BatchResultDto> Results { get; set; } = new();
    public int TotalBatches { get; set; }
    public int ExecutedBatches { get; set; }
}

public class BatchResultDto
{
    public int BatchIndex { get; set; }
    public string Type { get; set; } = string.Empty;          // DDL, DML, SELECT, ERROR
    public string? Message { get; set; }
    public int? RowsAffected { get; set; }
    public int DurationMs { get; set; }

    // For SELECT results
    public List<string>? Columns { get; set; }
    public List<Dictionary<string, object?>>? Rows { get; set; }
    public int? TotalRows { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }

    // For errors
    public string? Error { get; set; }
}

public class CreateDatabaseResultDto
{
    public string DatabaseName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SessionStatusDto
{
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool DatabaseCreated { get; set; }
    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? RemainingMinutes { get; set; }

    // Schedule info
    public ScheduleInfoDto? Schedule { get; set; }
}

public class ScheduleInfoDto
{
    public string SessionStartTime { get; set; } = string.Empty;
    public string SessionEndTime { get; set; } = string.Empty;
    public int ExtensionMinutes { get; set; }
    public bool IsInBreak { get; set; }
    public string? CurrentBreakTitle { get; set; }
    public string? BreakEndsAt { get; set; }
    /// <summary>True if schedule date is set but doesn't match today.</summary>
    public bool IsWrongDate { get; set; }
    /// <summary>True if current time is before session start.</summary>
    public bool IsBeforeStart { get; set; }
    /// <summary>True if current time is after session end.</summary>
    public bool IsAfterEnd { get; set; }
    /// <summary>The schedule date (if set).</summary>
    public string? ScheduleDate { get; set; }
    /// <summary>Alert thresholds with colors.</summary>
    public List<AlertConfigItem> Alerts { get; set; } = new();
    public List<BreakInfoDto> Breaks { get; set; } = new();
}

public class AlertConfigItem
{
    public int Minutes { get; set; }
    public string Color { get; set; } = "#3b82f6";
}

public class BreakInfoDto
{
    public string Title { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
