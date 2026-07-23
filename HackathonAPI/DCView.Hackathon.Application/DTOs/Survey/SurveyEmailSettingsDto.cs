namespace DCView.Hackathon.Application.DTOs.Survey;

public class SurveyEmailSettingsDto
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public bool IncludeRmByDefault { get; set; }
    public bool IncludeVhByDefault { get; set; }
    public int EmailMode { get; set; }
    public string? AdditionalCcEmails { get; set; }
    public string? EmailSubject { get; set; }
    public string? EmailBody { get; set; }
    public bool ReminderEnabled { get; set; }
    public int ReminderDays { get; set; }
    public int MaxReminders { get; set; }
}

public class UpdateEmailSettingsDto
{
    public bool IncludeRmByDefault { get; set; }
    public bool IncludeVhByDefault { get; set; }
    public int EmailMode { get; set; }
    public string? AdditionalCcEmails { get; set; }
    public string? EmailSubject { get; set; }
    public string? EmailBody { get; set; }
    public bool ReminderEnabled { get; set; }
    public int ReminderDays { get; set; } = 3;
    public int MaxReminders { get; set; } = 2;
}
