using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_EmailSettings")]
public class SurveyEmailSettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid SurveyId { get; set; }

    public bool IncludeRmByDefault { get; set; } = false;

    public bool IncludeVhByDefault { get; set; } = false;

    /// <summary>
    /// How emails are sent: SingleBulk (0), IndividualWithSummary (1), IndividualWithCc (2).
    /// </summary>
    public SurveyEmailMode EmailMode { get; set; } = SurveyEmailMode.SingleBulk;

    /// <summary>
    /// Comma-separated additional CC email addresses.
    /// </summary>
    public string? AdditionalCcEmails { get; set; }

    [MaxLength(500)]
    public string? EmailSubject { get; set; }

    /// <summary>
    /// HTML email body template. Supports variables:
    /// {{EmployeeName}}, {{SurveyTitle}}, {{SurveyLink}}, {{Deadline}}, {{RmName}}, {{VhName}}
    /// </summary>
    public string? EmailBody { get; set; }

    public bool ReminderEnabled { get; set; } = false;

    /// <summary>
    /// Days after initial send to auto-remind.
    /// </summary>
    public int ReminderDays { get; set; } = 3;

    /// <summary>
    /// Maximum number of auto-reminders to send.
    /// </summary>
    public int MaxReminders { get; set; } = 2;

    // Navigation
    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }
}
