using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_ResponseAnswers")]
public class SurveyResponseAnswer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid ResponseId { get; set; }

    public Guid FieldId { get; set; }

    /// <summary>
    /// The answer value. For multi-select/checkbox, stored as JSON array.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// For file upload fields, the URL/path to the uploaded file.
    /// </summary>
    [MaxLength(1000)]
    public string? FileUrl { get; set; }

    // Navigation
    [ForeignKey(nameof(ResponseId))]
    public virtual SurveyResponse? Response { get; set; }

    [ForeignKey(nameof(FieldId))]
    public virtual SurveyField? Field { get; set; }
}
