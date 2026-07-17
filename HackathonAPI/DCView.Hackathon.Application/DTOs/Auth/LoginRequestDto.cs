namespace DCView.Hackathon.Application.DTOs.Auth;

public class LoginRequestDto
{
    public string UserID { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserID { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool MustChangePassword { get; set; }
    public SessionInfoDto? Session { get; set; }
}

public class SessionInfoDto
{
    public bool IsActive { get; set; }
    public bool DatabaseCreated { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? RemainingMinutes { get; set; }
}

public class ChangePasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequestDto
{
    public string UserID { get; set; } = string.Empty;
}
