using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_AiDetectionLogs")]
public class AiDetectionLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int FileId { get; set; }

    [Required, MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// AI confidence score (0-100). Higher = more likely AI-generated.
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// Detection result: AI, Human, Uncertain
    /// </summary>
    [Required, MaxLength(20)]
    public string DetectionResult { get; set; } = "Uncertain";

    /// <summary>
    /// Reasoning from the AI model
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Content length at the time of analysis (character count)
    /// </summary>
    public int ContentLength { get; set; }

    /// <summary>
    /// Content size change from previous save (characters added/removed)
    /// </summary>
    public int ContentDelta { get; set; }

    /// <summary>
    /// Whether there was a tab-switch event shortly before this save
    /// </summary>
    public bool TabSwitchBeforeSave { get; set; }

    /// <summary>
    /// The model used for detection
    /// </summary>
    [MaxLength(50)]
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Time taken for AI analysis in milliseconds
    /// </summary>
    public int? ProcessingTimeMs { get; set; }

    public DateTime AnalyzedDate { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(FileId))]
    public virtual UserFile File { get; set; } = null!;
}
