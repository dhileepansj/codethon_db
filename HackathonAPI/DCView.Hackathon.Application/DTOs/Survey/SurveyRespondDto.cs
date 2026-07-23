namespace DCView.Hackathon.Application.DTOs.Survey;

/// <summary>
/// DTOs for the public survey response flow (email verification + OTP).
/// </summary>

public class VerifyEmailRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyEmailResponseDto
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public string? MaskedEmail { get; set; }
}

public class SendOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class SendOtpResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? MaskedEmail { get; set; }
    public int ExpiresInSeconds { get; set; }
}

public class VerifyOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class VerifyOtpResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? SessionToken { get; set; }
    public ParticipantInfoDto? ParticipantInfo { get; set; }
}

public class ParticipantInfoDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
}

/// <summary>
/// The complete data sent to the respondent once OTP is verified.
/// </summary>
public class SurveyFormDto
{
    public Guid SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ParticipantInfoDto Participant { get; set; } = new();
    public List<SurveyFieldDto> Fields { get; set; } = new();
    public string? ThankYouMessage { get; set; }
}

public class SendReminderDto
{
    public List<Guid> ParticipantIds { get; set; } = new();
}
