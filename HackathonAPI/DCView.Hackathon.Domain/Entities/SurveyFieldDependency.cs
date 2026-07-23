using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_FieldDependencies")]
public class SurveyFieldDependency
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// The field that is shown/hidden/required based on the condition.
    /// </summary>
    public Guid FieldId { get; set; }

    /// <summary>
    /// The parent field whose value determines the condition.
    /// </summary>
    public Guid DependsOnFieldId { get; set; }

    public DependencyCondition Condition { get; set; }

    /// <summary>
    /// The value to compare against. For choice fields, this is the option value.
    /// </summary>
    [MaxLength(2000)]
    public string? Value { get; set; }

    public DependencyAction Action { get; set; } = DependencyAction.Show;

    /// <summary>
    /// For SetOptions action: JSON map of parent value → child options.
    /// {"Tamil Nadu":["Chennai","Coimbatore"],"Karnataka":["Bangalore","Mysore"]}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? OptionMap { get; set; }

    /// <summary>
    /// For multi-condition rules: "AND" or "OR" logic group identifier.
    /// Fields with same GroupId are evaluated together.
    /// </summary>
    public Guid? LogicGroupId { get; set; }

    /// <summary>
    /// Logic operator for grouped conditions: AND (all must match) or OR (any must match).
    /// </summary>
    [MaxLength(3)]
    public string LogicOperator { get; set; } = "AND";

    // Navigation
    [ForeignKey(nameof(FieldId))]
    public virtual SurveyField? Field { get; set; }

    [ForeignKey(nameof(DependsOnFieldId))]
    public virtual SurveyField? DependsOnField { get; set; }
}
