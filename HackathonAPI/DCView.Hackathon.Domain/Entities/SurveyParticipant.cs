using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_Participants")]
public class SurveyParticipant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid SurveyId { get; set; }

    [Required, MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string EmployeeEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RmName { get; set; }

    [MaxLength(300)]
    public string? RmEmail { get; set; }

    [MaxLength(200)]
    public string? VhName { get; set; }

    [MaxLength(300)]
    public string? VhEmail { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tracks bulk upload batches.
    /// </summary>
    public Guid? BatchId { get; set; }

    public SurveyParticipantStatus Status { get; set; } = SurveyParticipantStatus.Pending;

    // Navigation
    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }

    public virtual SurveyDistribution? Distribution { get; set; }
    public virtual SurveyParticipantStatusLog? StatusLog { get; set; }
}
