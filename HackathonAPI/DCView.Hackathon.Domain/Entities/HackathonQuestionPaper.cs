using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_QuestionPaper")]
public class HackathonQuestionPaper
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Title of the hackathon / exam (shown in header).
    /// </summary>
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The HTML content of the question paper.
    /// </summary>
    [Required]
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled date of the hackathon.
    /// </summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>
    /// Scheduled start time.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Scheduled end time.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Duration in minutes (used when activating all sessions).
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Whether this question paper is currently active (only one should be active).
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    [MaxLength(50)]
    public string? ModifiedBy { get; set; }
}
