using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// Files uploaded by participants as part of their final submission (Word/Excel).
/// </summary>
[Table("Hackathon_SubmissionFiles")]
public class UserSubmissionFile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required, MaxLength(300)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type: application/vnd.openxmlformats-officedocument.wordprocessingml.document,
    /// application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, etc.
    /// </summary>
    [Required, MaxLength(200)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// File content stored as binary.
    /// </summary>
    [Required]
    public byte[] FileData { get; set; } = Array.Empty<byte>();

    public DateTime UploadedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
