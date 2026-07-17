using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Hackathon_UserFolders")]
public class UserFolder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FolderId { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? ParentFolderId { get; set; }

    [Required, MaxLength(200)]
    public string FolderName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(ParentFolderId))]
    public virtual UserFolder? ParentFolder { get; set; }

    public virtual ICollection<UserFolder> SubFolders { get; set; } = new List<UserFolder>();
    public virtual ICollection<UserFile> Files { get; set; } = new List<UserFile>();
}


