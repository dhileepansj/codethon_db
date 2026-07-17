using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string UserID { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Participant";

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// True = user must change password on next login.
    /// </summary>
    public bool MustChangePassword { get; set; } = true;

    /// <summary>
    /// True = user has submitted a forgot password request (pending admin reset).
    /// </summary>
    public bool PasswordResetRequested { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(50)]
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Last successful login timestamp.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Total number of successful logins.
    /// </summary>
    public int LoginCount { get; set; } = 0;

    // Navigation
    public virtual HackathonSession? Session { get; set; }
    public virtual ICollection<UserFile> Files { get; set; } = new List<UserFile>();
    public virtual ICollection<UserFolder> Folders { get; set; } = new List<UserFolder>();
    public virtual ICollection<ExecutionHistory> ExecutionHistories { get; set; } = new List<ExecutionHistory>();
    public virtual ICollection<TabSwitchLog> TabSwitchLogs { get; set; } = new List<TabSwitchLog>();
}


