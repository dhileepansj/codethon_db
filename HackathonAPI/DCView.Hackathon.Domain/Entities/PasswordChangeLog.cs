using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_PasswordChangeLogs")]
public class PasswordChangeLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// The hashed password that was set (for history comparison).
    /// </summary>
    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Who initiated the change: "Self", "Admin"
    /// </summary>
    [Required, MaxLength(20)]
    public string ChangedBy { get; set; } = "Self";

    /// <summary>
    /// The admin's user ID if changed by admin, otherwise null.
    /// </summary>
    [MaxLength(50)]
    public string? ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// IP address of the requester (optional).
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
