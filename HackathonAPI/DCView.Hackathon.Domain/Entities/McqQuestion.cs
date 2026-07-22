using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// A single MCQ question in a question bank.
/// </summary>
[Table("Hackathon_McqQuestions")]
public class McqQuestion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int AssessmentId { get; set; }

    /// <summary>Serial number within the bank (for ordering/reference)</summary>
    public int SNo { get; set; }

    /// <summary>The question text (supports HTML for formatting)</summary>
    [Required]
    public string Question { get; set; } = string.Empty;

    /// <summary>Option A text</summary>
    [Required, MaxLength(1000)]
    public string OptionA { get; set; } = string.Empty;

    /// <summary>Option B text</summary>
    [Required, MaxLength(1000)]
    public string OptionB { get; set; } = string.Empty;

    /// <summary>Option C text</summary>
    [Required, MaxLength(1000)]
    public string OptionC { get; set; } = string.Empty;

    /// <summary>Option D text</summary>
    [Required, MaxLength(1000)]
    public string OptionD { get; set; } = string.Empty;

    /// <summary>Correct answer: "A", "B", "C", or "D"</summary>
    [Required, MaxLength(1)]
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>Difficulty: "Simple", "Medium", "Complex"</summary>
    [Required, MaxLength(20)]
    public string Complexity { get; set; } = "Simple";

    /// <summary>Marks for this question (derived from complexity, but can be overridden)</summary>
    public int Marks { get; set; } = 1;

    /// <summary>Optional topic/category tag (e.g., "Locators", "Waits", "Grid")</summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>Optional explanation shown after submission</summary>
    public string? Explanation { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(AssessmentId))]
    public virtual Assessment Assessment { get; set; } = null!;
}
