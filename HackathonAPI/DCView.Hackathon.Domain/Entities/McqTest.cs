using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// A generated test instance for a participant — contains the shuffled question set and results.
/// </summary>
[Table("Hackathon_McqTests")]
public class McqTest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int AssessmentId { get; set; }

    /// <summary>When the participant started the test</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When the participant submitted (or auto-submitted)</summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>Total time spent in seconds</summary>
    public int? TimeSpentSeconds { get; set; }

    /// <summary>Total questions presented</summary>
    public int TotalQuestions { get; set; }

    /// <summary>Questions attempted (answered)</summary>
    public int Attempted { get; set; } = 0;

    /// <summary>Correct answers</summary>
    public int Correct { get; set; } = 0;

    /// <summary>Wrong answers</summary>
    public int Wrong { get; set; } = 0;

    /// <summary>Skipped/unanswered questions</summary>
    public int Skipped { get; set; } = 0;

    /// <summary>Total score (after marks calculation and negative marking)</summary>
    public decimal Score { get; set; } = 0;

    /// <summary>Maximum possible score</summary>
    public decimal MaxScore { get; set; } = 0;

    /// <summary>Score percentage</summary>
    public decimal Percentage { get; set; } = 0;

    /// <summary>Whether the participant passed (based on PassPercentage setting)</summary>
    public bool? Passed { get; set; }

    /// <summary>Whether the test was auto-submitted due to timeout</summary>
    public bool IsAutoSubmitted { get; set; } = false;

    /// <summary>Whether the test is currently in progress</summary>
    public bool IsInProgress { get; set; } = false;

    /// <summary>Whether the test has been submitted</summary>
    public bool IsSubmitted { get; set; } = false;

    /// <summary>
    /// JSON array of question IDs in the order presented to this participant.
    /// E.g., [45, 12, 89, 3, ...] — preserves shuffled order.
    /// </summary>
    public string QuestionOrder { get; set; } = "[]";

    /// <summary>
    /// JSON object mapping questionId → shuffled option order.
    /// E.g., {"45": ["C","A","D","B"], "12": ["B","D","A","C"]}
    /// Only populated when ShuffleOptions is enabled.
    /// </summary>
    public string? OptionOrder { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(AssessmentId))]
    public virtual Assessment Assessment { get; set; } = null!;

    public virtual ICollection<McqAnswer> Answers { get; set; } = new List<McqAnswer>();
}
