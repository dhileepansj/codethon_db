using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_UserFiles")]
public class UserFile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FileId { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? FolderId { get; set; }

    [Required, MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string FileType { get; set; } = "Script";

    public string? Content { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? ModifiedDate { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(FolderId))]
    public virtual UserFolder? Folder { get; set; }
}


