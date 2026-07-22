using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// A participant's answer to a single MCQ question.
/// </summary>
[Table("Hackathon_McqAnswers")]
public class McqAnswer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public int TestId { get; set; }

    [Required]
    public int QuestionId { get; set; }

    /// <summary>The option selected by the participant: "A", "B", "C", "D", or null (skipped)</summary>
    [MaxLength(1)]
    public string? SelectedAnswer { get; set; }

    /// <summary>Whether the selected answer is correct</summary>
    public bool? IsCorrect { get; set; }

    /// <summary>Marks awarded for this answer (can be negative if negative marking enabled)</summary>
    public decimal MarksAwarded { get; set; } = 0;

    /// <summary>Time spent on this question in seconds</summary>
    public int? TimeTakenSeconds { get; set; }

    /// <summary>Whether the participant flagged this question for review</summary>
    public bool IsFlagged { get; set; } = false;

    /// <summary>When the answer was last updated</summary>
    public DateTime? AnsweredAt { get; set; }

    /// <summary>Order position in the test (1-based)</summary>
    public int QuestionIndex { get; set; }

    // Navigation
    [ForeignKey(nameof(TestId))]
    public virtual McqTest Test { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public virtual McqQuestion Question { get; set; } = null!;
}
