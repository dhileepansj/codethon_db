using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_Sessions")]
public class HackathonSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SessionId { get; set; }

    [Required]
    public int UserId { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = false;

    public bool DatabaseCreated { get; set; } = false;

    [MaxLength(200)]
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Encrypted credentials for the user's DB login on the hackathon server.
    /// </summary>
    [MaxLength(500)]
    public string? DbLoginPassword { get; set; }

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Whether the participant has submitted their work (final submission).
    /// Once submitted, no further edits allowed.
    /// </summary>
    public bool IsSubmitted { get; set; } = false;

    public DateTime? SubmittedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}


