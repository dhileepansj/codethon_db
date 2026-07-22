namespace DCView.Hackathon.Application.DTOs.Admin;

public class UserDto
{
    public int Id { get; set; }
    public string UserID { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public bool PasswordResetRequested { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int LoginCount { get; set; }
    /// <summary>"SqlServer" or "Oracle"</summary>
    public string DbEnginePreference { get; set; } = "SqlServer";
    /// <summary>Assessment type: "SQL" or "MCQ"</summary>
    public string AssessmentType { get; set; } = "SQL";
    /// <summary>Assessment title (e.g., "Selenium MCQ Test")</summary>
    public string? AssessmentTitle { get; set; }
    /// <summary>Assessment sub-type (e.g., "SqlServer", "Oracle", "Selenium")</summary>
    public string? AssessmentSubType { get; set; }
    public int? AssessmentId { get; set; }
    /// <summary>MCQ test progress info (null for SQL users)</summary>
    public McqProgressDto? McqProgress { get; set; }
    public SessionSummaryDto? Session { get; set; }
}

public class McqProgressDto
{
    /// <summary>"NotStarted", "InProgress", "Submitted"</summary>
    public string Status { get; set; } = "NotStarted";
    public int TotalQuestions { get; set; }
    public int Answered { get; set; }
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? Percentage { get; set; }
    public bool? Passed { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public class SessionSummaryDto
{
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool DatabaseCreated { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CreateUserDto
{
    public string UserID { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    /// <summary>"SqlServer" or "Oracle". Defaults to SqlServer.</summary>
    public string? DbEnginePreference { get; set; }
    /// <summary>Assessment ID to assign. If set, overrides DbEnginePreference.</summary>
    public int? AssessmentId { get; set; }
}

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
    /// <summary>"SqlServer" or "Oracle". Null = don't change.</summary>
    public string? DbEnginePreference { get; set; }
}

public class ActivateSessionDto
{
    public int? DurationMinutes { get; set; }
}

public class ExtendSessionDto
{
    public int AdditionalMinutes { get; set; }
}

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveSessions { get; set; }
    public int DatabasesCreated { get; set; }
    public int QueriesToday { get; set; }
}
