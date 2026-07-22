using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_Config")]
public class HackathonConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string ServerName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string AdminUserId { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string AdminPasswordEncrypted { get; set; } = string.Empty;

    [MaxLength(50)]
    public string DbPrefix { get; set; } = "Hackathon_";

    public int MaxQueryTimeoutSeconds { get; set; } = 30;

    public int MaxRowsPerPage { get; set; } = 25;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Database engine type: SqlServer (0) or Oracle (1).
    /// Defaults to SqlServer for backward compatibility.
    /// </summary>
    public DbEngineType DbEngineType { get; set; } = DbEngineType.SqlServer;

    /// <summary>
    /// Oracle-specific: Service name or SID (e.g., "XEPDB1", "ORCL").
    /// Not used for SQL Server.
    /// </summary>
    [MaxLength(200)]
    public string? OracleServiceName { get; set; }

    /// <summary>
    /// Oracle-specific: Port number (default 1521).
    /// For SQL Server, the port is part of ServerName.
    /// </summary>
    public int? Port { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string? CreatedBy { get; set; }
}


