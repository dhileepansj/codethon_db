namespace DCView.Hackathon.Application.Interfaces;

public interface ISurveyEmailService
{
    /// <summary>
    /// Send a survey invitation email to a participant.
    /// </summary>
    Task<bool> SendInvitationAsync(SurveyEmailMessage message);

    /// <summary>
    /// Send a single bulk email with multiple participants in TO.
    /// </summary>
    Task<bool> SendBulkInvitationAsync(BulkSurveyEmailMessage message);

    /// <summary>
    /// Send a summary notification to an RM/VH listing their reportees.
    /// </summary>
    Task<bool> SendManagerSummaryAsync(string managerEmail, string managerName, string surveyTitle, List<string> reporteeNames);

    /// <summary>
    /// Send a reminder email to a participant.
    /// </summary>
    Task<bool> SendReminderAsync(SurveyEmailMessage message);

    /// <summary>
    /// Send an OTP verification email.
    /// </summary>
    Task<bool> SendOtpAsync(string toEmail, string toName, string otpCode, string surveyTitle);
}

public class SurveyEmailMessage
{
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string? RmEmail { get; set; }
    public string? VhEmail { get; set; }
    public bool IncludeRm { get; set; }
    public bool IncludeVh { get; set; }
    public string? AdditionalCcEmails { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string SurveyTitle { get; set; } = string.Empty;
    public string SurveyLink { get; set; } = string.Empty;
    public string? Deadline { get; set; }
}

public class BulkSurveyEmailMessage
{
    /// <summary>
    /// All participant emails in TO.
    /// </summary>
    public List<(string Email, string Name)> ToRecipients { get; set; } = new();

    /// <summary>
    /// All unique RM/VH emails in CC.
    /// </summary>
    public List<string> CcEmails { get; set; } = new();

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// The survey link for QR code generation.
    /// </summary>
    public string? SurveyLink { get; set; }
}
