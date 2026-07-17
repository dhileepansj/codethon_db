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
    public SessionSummaryDto? Session { get; set; }
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
}

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
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
