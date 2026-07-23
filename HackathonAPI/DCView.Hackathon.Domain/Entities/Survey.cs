using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_Surveys")]
public class Survey
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// If true, participants can submit the survey more than once.
    /// </summary>
    public bool AllowMultiple { get; set; } = false;

    /// <summary>
    /// If true, responses are anonymous (participant info not linked).
    /// </summary>
    public bool IsAnonymous { get; set; } = false;

    [MaxLength(2000)]
    public string? ThankYouMessage { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation
    [ForeignKey(nameof(CreatedByUserId))]
    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<SurveyField> Fields { get; set; } = new List<SurveyField>();
    public virtual ICollection<SurveyParticipant> Participants { get; set; } = new List<SurveyParticipant>();
    public virtual ICollection<SurveyDistribution> Distributions { get; set; } = new List<SurveyDistribution>();
    public virtual ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
    public virtual SurveyEmailSettings? EmailSettings { get; set; }
}
