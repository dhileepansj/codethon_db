using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_Responses")]
public class SurveyResponse
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid SurveyId { get; set; }

    public Guid? ParticipantId { get; set; }

    public Guid? DistributionId { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>
    /// Time taken to fill the survey in seconds.
    /// </summary>
    public int? TimeTakenSeconds { get; set; }

    // Navigation
    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }

    [ForeignKey(nameof(ParticipantId))]
    public virtual SurveyParticipant? Participant { get; set; }

    [ForeignKey(nameof(DistributionId))]
    public virtual SurveyDistribution? Distribution { get; set; }

    public virtual ICollection<SurveyResponseAnswer> Answers { get; set; } = new List<SurveyResponseAnswer>();
}
