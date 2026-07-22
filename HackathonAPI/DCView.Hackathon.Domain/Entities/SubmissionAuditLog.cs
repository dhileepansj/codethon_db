using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// Audit log for submission and release events.
/// </summary>
[Table("Hackathon_SubmissionAuditLog")]
public class SubmissionAuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>The participant's login ID</summary>
    [Required, MaxLength(50)]
    public string UserLoginId { get; set; } = string.Empty;

    /// <summary>"Submitted" or "Released"</summary>
    [Required, MaxLength(20)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Assessment type: "SQL", "MCQ", "ManualTesting"</summary>
    [MaxLength(30)]
    public string? AssessmentType { get; set; }

    /// <summary>Who performed this action (participant for submit, admin for release)</summary>
    [MaxLength(50)]
    public string? PerformedBy { get; set; }

    /// <summary>Reason for release (admin provides)</summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    public DateTime EventTime { get; set; } = DateTime.Now;
}
