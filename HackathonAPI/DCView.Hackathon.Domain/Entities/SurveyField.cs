using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_Fields")]
public class SurveyField
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid SurveyId { get; set; }

    public SurveyFieldType FieldType { get; set; }

    [Required, MaxLength(1000)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Placeholder { get; set; }

    public bool IsRequired { get; set; } = false;

    public int SortOrder { get; set; }

    /// <summary>
    /// JSON array of options for choice fields: [{"value":"v","label":"l"}]
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Options { get; set; }

    /// <summary>
    /// JSON object for validation rules: {"min":0,"max":100,"regex":"...","maxLength":500}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Validation { get; set; }

    [MaxLength(500)]
    public string? SectionTitle { get; set; }

    public string? DefaultValue { get; set; }

    /// <summary>
    /// For Matrix field: JSON array of row labels
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? MatrixRows { get; set; }

    /// <summary>
    /// For Matrix field: JSON array of column labels
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? MatrixColumns { get; set; }

    // Navigation
    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }

    public virtual ICollection<SurveyFieldDependency> Dependencies { get; set; } = new List<SurveyFieldDependency>();
    public virtual ICollection<SurveyFieldDependency> DependentFields { get; set; } = new List<SurveyFieldDependency>();
}
