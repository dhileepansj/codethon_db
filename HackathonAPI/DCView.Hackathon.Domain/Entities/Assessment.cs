using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// Represents an assessment that can be assigned to participants.
/// Can be a SQL hackathon (SQL Server / Oracle) or an MCQ test (Selenium, Playwright, custom).
/// </summary>
[Table("Hackathon_Assessments")]
public class Assessment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Display title. E.g., "SQL Server Hackathon", "Selenium MCQ Test"</summary>
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Assessment type: "SQL" or "MCQ"</summary>
    [Required, MaxLength(20)]
    public string Type { get; set; } = "SQL";

    /// <summary>Sub-type for routing: "SqlServer", "Oracle", "Selenium", "Playwright", or custom name</summary>
    [Required, MaxLength(50)]
    public string SubType { get; set; } = "SqlServer";

    /// <summary>Test duration in minutes. Null = unlimited (controlled by session).</summary>
    public int? DurationMinutes { get; set; }

    // ─── MCQ-specific settings ───────────────────────────────────

    /// <summary>Total questions to present from the bank (e.g., 30 from 150)</summary>
    public int TotalQuestions { get; set; } = 30;

    /// <summary>Maximum marks for the test</summary>
    public int MaxMarks { get; set; } = 45;

    /// <summary>Percentage of Simple questions to pick (0-100)</summary>
    public int SimplePercentage { get; set; } = 60;

    /// <summary>Percentage of Medium questions to pick (0-100)</summary>
    public int MediumPercentage { get; set; } = 30;

    /// <summary>Percentage of Complex questions to pick (0-100)</summary>
    public int ComplexPercentage { get; set; } = 10;

    /// <summary>Marks for Simple questions</summary>
    public int SimpleMarks { get; set; } = 1;

    /// <summary>Marks for Medium questions</summary>
    public int MediumMarks { get; set; } = 2;

    /// <summary>Marks for Complex questions</summary>
    public int ComplexMarks { get; set; } = 3;

    /// <summary>Enable negative marking for wrong answers</summary>
    public bool NegativeMarking { get; set; } = false;

    /// <summary>Negative mark value per wrong answer (e.g., 0.25)</summary>
    public decimal NegativeMarkValue { get; set; } = 0;

    /// <summary>Shuffle question order per participant</summary>
    public bool ShuffleQuestions { get; set; } = true;

    /// <summary>Shuffle option order (A/B/C/D) per participant</summary>
    public bool ShuffleOptions { get; set; } = true;

    /// <summary>Show result to participant immediately after submission</summary>
    public bool ShowResultImmediately { get; set; } = false;

    /// <summary>Pass percentage (0-100). 0 = no pass/fail concept.</summary>
    public int PassPercentage { get; set; } = 0;

    /// <summary>Allow navigating between questions (vs linear mode)</summary>
    public bool AllowNavigation { get; set; } = true;

    /// <summary>Allow participants to flag/mark questions for review</summary>
    public bool AllowReview { get; set; } = true;

    /// <summary>Auto-submit when time runs out</summary>
    public bool AutoSubmitOnTimeout { get; set; } = true;

    /// <summary>Show one question at a time vs all questions on one page</summary>
    public bool OneQuestionPerPage { get; set; } = true;

    /// <summary>Show complexity label (Simple/Medium/Complex) to participants during the test</summary>
    public bool ShowComplexity { get; set; } = true;

    /// <summary>Show marks per question to participants during the test</summary>
    public bool ShowMarks { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(50)]
    public string? ModifiedBy { get; set; }

    // Navigation
    public virtual ICollection<McqQuestion> Questions { get; set; } = new List<McqQuestion>();
}
