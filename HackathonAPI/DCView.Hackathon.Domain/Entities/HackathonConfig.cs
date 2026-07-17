using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string? CreatedBy { get; set; }
}


