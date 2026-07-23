using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_Distributions")]
public class SurveyDistribution
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid SurveyId { get; set; }

    public Guid ParticipantId { get; set; }

    /// <summary>
    /// Unique token for the survey response link.
    /// </summary>
    [Required, MaxLength(100)]
    public string Token { get; set; } = string.Empty;

    public DateTime? SentAt { get; set; }

    public bool IncludeRm { get; set; } = false;

    public bool IncludeVh { get; set; } = false;

    /// <summary>
    /// Comma-separated additional CC email addresses.
    /// </summary>
    public string? CcEmails { get; set; }

    public SurveyEmailStatus EmailStatus { get; set; } = SurveyEmailStatus.Pending;

    public DateTime? RespondedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }

    [ForeignKey(nameof(ParticipantId))]
    public virtual SurveyParticipant? Participant { get; set; }

    public virtual ICollection<SurveyReminderLog> Reminders { get; set; } = new List<SurveyReminderLog>();
}
