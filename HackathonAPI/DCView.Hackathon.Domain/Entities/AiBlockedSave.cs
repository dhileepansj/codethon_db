using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// Stores blocked save attempts for admin review.
/// </summary>
[Table("Hackathon_AiBlockedSaves")]
public class AiBlockedSave
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
    /// The content that was blocked from saving.
    /// </summary>
    public string? AttemptedContent { get; set; }

    /// <summary>
    /// The AI confidence score that triggered the block.
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// AI reasoning for the detection.
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Status: Pending, Approved, Rejected
    /// </summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Admin who reviewed this blocked save.
    /// </summary>
    [MaxLength(100)]
    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedDate { get; set; }

    [MaxLength(500)]
    public string? AdminRemarks { get; set; }

    public DateTime BlockedDate { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(FileId))]
    public virtual UserFile File { get; set; } = null!;
}
