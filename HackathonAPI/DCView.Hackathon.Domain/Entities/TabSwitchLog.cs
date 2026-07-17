using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_TabSwitchLogs")]
public class TabSwitchLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string EventType { get; set; } = string.Empty; // "tab_hidden", "tab_visible", "window_blur", "window_focus", "devtools_*"

    public DateTime EventTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Duration in seconds the user was away (only set on "tab_visible" / "window_focus" events)
    /// </summary>
    public int? AwayDurationSeconds { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}


