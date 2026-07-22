using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// Admin user with configurable permissions.
/// SuperAdmin has all permissions implicitly.
/// </summary>
[Table("Hackathon_AdminUsers")]
public class AdminUser
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

    /// <summary>Role: "SuperAdmin" or "Admin"</summary>
    [Required, MaxLength(20)]
    public string Role { get; set; } = "Admin";

    public bool IsActive { get; set; } = true;

    /// <summary>True = admin must change password on next login.</summary>
    public bool MustChangePassword { get; set; } = true;

    // ─── Permissions (granular) ──────────────────────────────────

    public bool CanManageUsers { get; set; } = false;
    public bool CanManageSessions { get; set; } = false;
    public bool CanViewMonitoring { get; set; } = false;
    public bool CanManageAssessments { get; set; } = false;
    public bool CanViewResults { get; set; } = false;
    public bool CanManageHackathonSetup { get; set; } = false;
    public bool CanManageServerConfig { get; set; } = false;
    public bool CanManageScaffoldScripts { get; set; } = false;
    public bool CanManageSecuritySettings { get; set; } = false;
    public bool CanManageAiDetection { get; set; } = false;
    public bool CanManageManualTesting { get; set; } = false;
    public bool CanExportData { get; set; } = false;
    public bool CanResetDatabase { get; set; } = false;
    public bool CanDeleteUsers { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
