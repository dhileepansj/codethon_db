using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// Single-row table for configurable security/password policies.
/// </summary>
[Table("Hackathon_SecuritySettings")]
public class SecuritySettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // ─── Password Complexity ──────────────────────────────────

    /// <summary>Minimum password length.</summary>
    public int MinLength { get; set; } = 8;

    /// <summary>Maximum password length (0 = no limit).</summary>
    public int MaxLength { get; set; } = 64;

    /// <summary>Require at least one uppercase letter.</summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>Require at least one lowercase letter.</summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>Require at least one digit.</summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>Require at least one special character.</summary>
    public bool RequireSpecialChar { get; set; } = true;

    // ─── Password History ─────────────────────────────────────

    /// <summary>Number of previous passwords that cannot be reused (0 = disabled).</summary>
    public int PasswordHistoryCount { get; set; } = 5;

    // ─── Account Lockout ──────────────────────────────────────

    /// <summary>Max failed login attempts before lockout (0 = disabled).</summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>Lockout duration in minutes (0 = permanent until admin unlocks).</summary>
    public int LockoutDurationMinutes { get; set; } = 15;

    // ─── Session Security ─────────────────────────────────────

    /// <summary>Force password change every N days (0 = never).</summary>
    public int PasswordExpiryDays { get; set; } = 0;

    /// <summary>Maximum concurrent sessions per user (0 = unlimited).</summary>
    public int MaxConcurrentSessions { get; set; } = 1;

    // ─── Metadata ─────────────────────────────────────────────

    public DateTime ModifiedDate { get; set; }

    [MaxLength(50)]
    public string? ModifiedBy { get; set; }
}
