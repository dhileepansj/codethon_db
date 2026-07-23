using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Application.DTOs.Survey;

public class SurveyFieldDto
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public SurveyFieldType FieldType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? Options { get; set; }
    public string? Validation { get; set; }
    public string? SectionTitle { get; set; }
    public string? DefaultValue { get; set; }
    public string? MatrixRows { get; set; }
    public string? MatrixColumns { get; set; }
    public List<FieldDependencyDto> Dependencies { get; set; } = new();
}

public class CreateFieldDto
{
    public SurveyFieldType FieldType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; } = false;
    public int? SortOrder { get; set; }
    public string? Options { get; set; }
    public string? Validation { get; set; }
    public string? SectionTitle { get; set; }
    public string? DefaultValue { get; set; }
    public string? MatrixRows { get; set; }
    public string? MatrixColumns { get; set; }
}

public class UpdateFieldDto
{
    public SurveyFieldType? FieldType { get; set; }
    public string? Label { get; set; }
    public string? Description { get; set; }
    public string? Placeholder { get; set; }
    public bool? IsRequired { get; set; }
    public string? Options { get; set; }
    public string? Validation { get; set; }
    public string? SectionTitle { get; set; }
    public string? DefaultValue { get; set; }
    public string? MatrixRows { get; set; }
    public string? MatrixColumns { get; set; }
}

public class ReorderFieldsDto
{
    public List<FieldOrderItem> Fields { get; set; } = new();
}

public class FieldOrderItem
{
    public Guid FieldId { get; set; }
    public int SortOrder { get; set; }
}
