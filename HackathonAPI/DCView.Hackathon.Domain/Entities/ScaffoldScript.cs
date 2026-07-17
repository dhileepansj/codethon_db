using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// SQL scripts uploaded by admin that get executed when a participant creates their database.
/// Also copied as files into the participant's file manager for reference.
/// </summary>
[Table("Hackathon_ScaffoldScripts")]
public class ScaffoldScript
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>File name shown in participant's file manager (e.g., "01_CreateTables.sql")</summary>
    [Required, MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>The SQL content to execute and provide to participants.</summary>
    [Required]
    public string SqlContent { get; set; } = string.Empty;

    /// <summary>Order of execution (1, 2, 3...)</summary>
    public int ExecutionOrder { get; set; } = 1;

    /// <summary>If true, this script is executed and provided to participants.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(50)]
    public string? ModifiedBy { get; set; }
}
