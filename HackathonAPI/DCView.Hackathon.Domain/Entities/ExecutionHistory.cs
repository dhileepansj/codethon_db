using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_ExecutionHistory")]
public class ExecutionHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [MaxLength(200)]
    public string? DatabaseName { get; set; }

    [Required]
    public string QueryText { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? QueryType { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Success";

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public int? RowsAffected { get; set; }

    public int? DurationMs { get; set; }

    public DateTime ExecutedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}


