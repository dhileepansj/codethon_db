using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// Global AI detection settings (single row).
/// </summary>
[Table("Hackathon_AiDetectionSettings")]
public class AiDetectionSettings
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Detection mode: Block, AllowAndMark, Disabled
    /// </summary>
    [Required, MaxLength(20)]
    public string Mode { get; set; } = "AllowAndMark";

    /// <summary>
    /// Threshold score (0-100). If AI confidence >= this, action is taken.
    /// </summary>
    public int BlockThreshold { get; set; } = 70;

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Per-user override for AI detection settings.
/// </summary>
[Table("Hackathon_AiDetectionUserOverrides")]
public class AiDetectionUserOverride
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Override mode: Block, AllowAndMark, Disabled (or null to use global)
    /// </summary>
    [MaxLength(20)]
    public string? Mode { get; set; }

    /// <summary>
    /// Override threshold (or null to use global)
    /// </summary>
    public int? BlockThreshold { get; set; }

    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public string? ModifiedBy { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
