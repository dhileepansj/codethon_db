using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Application.DTOs.Survey;

public class FieldDependencyDto
{
    public Guid Id { get; set; }
    public Guid FieldId { get; set; }
    public Guid DependsOnFieldId { get; set; }
    public DependencyCondition Condition { get; set; }
    public string? Value { get; set; }
    public DependencyAction Action { get; set; }
    public string? OptionMap { get; set; }
    public Guid? LogicGroupId { get; set; }
    public string LogicOperator { get; set; } = "AND";
}

public class CreateDependencyDto
{
    public Guid DependsOnFieldId { get; set; }
    public DependencyCondition Condition { get; set; }
    public string? Value { get; set; }
    public DependencyAction Action { get; set; } = DependencyAction.Show;
    public string? OptionMap { get; set; }
    public Guid? LogicGroupId { get; set; }
    public string LogicOperator { get; set; } = "AND";
}
