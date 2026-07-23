using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_ParticipantStatusLogs")]
public class SurveyParticipantStatusLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid SurveyId { get; set; }

    public Guid ParticipantId { get; set; }

    public DeclinedByType? DeclinedBy { get; set; }

    [MaxLength(2000)]
    public string? DeclineReason { get; set; }

    /// <summary>
    /// File path for the uploaded email/screenshot as proof.
    /// </summary>
    [MaxLength(500)]
    public string? DeclineAttachmentPath { get; set; }

    public DateTime? DeclinedAt { get; set; }

    /// <summary>
    /// The admin user who marked this participant as declined.
    /// </summary>
    public int? MarkedByUserId { get; set; }

    // Navigation
    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }

    [ForeignKey(nameof(ParticipantId))]
    public virtual SurveyParticipant? Participant { get; set; }

    [ForeignKey(nameof(MarkedByUserId))]
    public virtual User? MarkedByUser { get; set; }
}
