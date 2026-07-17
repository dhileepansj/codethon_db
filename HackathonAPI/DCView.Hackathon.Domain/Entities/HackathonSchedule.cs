using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// Defines the hackathon day schedule with time slots and breaks.
/// Admin configures: session start, breaks, session end.
/// Example: 10:00-13:00 (Session 1), 13:00-14:00 (Lunch Break), 14:00-18:00 (Session 2)
/// </summary>
[Table("Hackathon_Schedule")]
public class HackathonSchedule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Date this schedule applies to (null = default/every day).</summary>
    public DateTime? ScheduleDate { get; set; }

    /// <summary>Overall session start time (e.g., "10:00").</summary>
    [Required, MaxLength(10)]
    public string SessionStartTime { get; set; } = "10:00";

    /// <summary>Overall session end time (e.g., "18:00").</summary>
    [Required, MaxLength(10)]
    public string SessionEndTime { get; set; } = "18:00";

    /// <summary>Whether schedule is currently active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Additional minutes added by admin (extension).</summary>
    public int ExtensionMinutes { get; set; } = 0;

    /// <summary>
    /// JSON array of alert configurations. Each item: { "minutes": 30, "color": "#3b82f6" }
    /// </summary>
    [MaxLength(500)]
    public string AlertConfig { get; set; } = "[{\"minutes\":30,\"color\":\"#3b82f6\"},{\"minutes\":15,\"color\":\"#f59e0b\"},{\"minutes\":5,\"color\":\"#f97316\"},{\"minutes\":1,\"color\":\"#ef4444\"}]";

    public DateTime CreatedDate { get; set; }

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(50)]
    public string? ModifiedBy { get; set; }

    // Navigation
    public virtual ICollection<HackathonBreak> Breaks { get; set; } = new List<HackathonBreak>();
}

/// <summary>
/// A break period within the hackathon schedule (e.g., lunch break, tea break).
/// During breaks, participants cannot execute queries but can still view/edit files.
/// </summary>
[Table("Hackathon_ScheduleBreaks")]
public class HackathonBreak
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; } = "Break";

    /// <summary>Break start time (e.g., "13:00").</summary>
    [Required, MaxLength(10)]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>Break end time (e.g., "14:00").</summary>
    [Required, MaxLength(10)]
    public string EndTime { get; set; } = string.Empty;

    // Navigation
    [ForeignKey(nameof(ScheduleId))]
    public virtual HackathonSchedule Schedule { get; set; } = null!;
}
