using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.Application.Services;

public class SurveyFormBuilderService : ISurveyFormBuilderService
{
    private readonly ISurveyFieldRepository _fieldRepo;
    private readonly ISurveyRepository _surveyRepo;

    public SurveyFormBuilderService(ISurveyFieldRepository fieldRepo, ISurveyRepository surveyRepo)
    {
        _fieldRepo = fieldRepo;
        _surveyRepo = surveyRepo;
    }

    public async Task<IEnumerable<SurveyFieldDto>> GetFieldsAsync(Guid surveyId)
    {
        var fields = await _fieldRepo.GetBySurveyIdWithDependenciesAsync(surveyId);
        return fields.Select(MapToDto);
    }

    public async Task<SurveyFieldDto?> GetFieldByIdAsync(Guid fieldId)
    {
        var field = await _fieldRepo.GetByIdAsync(fieldId);
        return field != null ? MapToDto(field) : null;
    }

    public async Task<SurveyFieldDto> CreateFieldAsync(Guid surveyId, CreateFieldDto dto)
    {
        var sortOrder = dto.SortOrder ?? (await _fieldRepo.GetMaxSortOrderAsync(surveyId) + 1);

        var field = new SurveyField
        {
            SurveyId = surveyId,
            FieldType = dto.FieldType,
            Label = dto.Label,
            Description = dto.Description,
            Placeholder = dto.Placeholder,
            IsRequired = dto.IsRequired,
            SortOrder = sortOrder,
            Options = dto.Options,
            Validation = dto.Validation,
            SectionTitle = dto.SectionTitle,
            DefaultValue = dto.DefaultValue,
            MatrixRows = dto.MatrixRows,
            MatrixColumns = dto.MatrixColumns
        };

        var created = await _fieldRepo.CreateAsync(field);
        return MapToDto(created);
    }

    public async Task<SurveyFieldDto?> UpdateFieldAsync(Guid fieldId, UpdateFieldDto dto)
    {
        var field = await _fieldRepo.GetByIdAsync(fieldId);
        if (field == null) return null;

        if (dto.FieldType.HasValue) field.FieldType = dto.FieldType.Value;
        if (dto.Label != null) field.Label = dto.Label;
        if (dto.Description != null) field.Description = dto.Description;
        if (dto.Placeholder != null) field.Placeholder = dto.Placeholder;
        if (dto.IsRequired.HasValue) field.IsRequired = dto.IsRequired.Value;
        if (dto.Options != null) field.Options = dto.Options;
        if (dto.Validation != null) field.Validation = dto.Validation;
        if (dto.SectionTitle != null) field.SectionTitle = dto.SectionTitle;
        if (dto.DefaultValue != null) field.DefaultValue = dto.DefaultValue;
        if (dto.MatrixRows != null) field.MatrixRows = dto.MatrixRows;
        if (dto.MatrixColumns != null) field.MatrixColumns = dto.MatrixColumns;

        await _fieldRepo.UpdateAsync(field);
        return MapToDto(field);
    }

    public async Task<bool> DeleteFieldAsync(Guid fieldId)
    {
        var field = await _fieldRepo.GetByIdAsync(fieldId);
        if (field == null) return false;

        await _fieldRepo.DeleteAsync(field);
        return true;
    }

    public async Task<bool> ReorderFieldsAsync(Guid surveyId, ReorderFieldsDto dto)
    {
        foreach (var item in dto.Fields)
        {
            var field = await _fieldRepo.GetByIdAsync(item.FieldId);
            if (field != null && field.SurveyId == surveyId)
            {
                field.SortOrder = item.SortOrder;
                await _fieldRepo.UpdateAsync(field);
            }
        }
        return true;
    }

    // Dependencies
    public async Task<FieldDependencyDto> CreateDependencyAsync(Guid fieldId, CreateDependencyDto dto)
    {
        var dependency = new SurveyFieldDependency
        {
            FieldId = fieldId,
            DependsOnFieldId = dto.DependsOnFieldId,
            Condition = dto.Condition,
            Value = dto.Value,
            Action = dto.Action,
            OptionMap = dto.OptionMap,
            LogicGroupId = dto.LogicGroupId,
            LogicOperator = dto.LogicOperator
        };

        var created = await _fieldRepo.CreateDependencyAsync(dependency);
        return new FieldDependencyDto
        {
            Id = created.Id,
            FieldId = created.FieldId,
            DependsOnFieldId = created.DependsOnFieldId,
            Condition = created.Condition,
            Value = created.Value,
            Action = created.Action,
            OptionMap = created.OptionMap,
            LogicGroupId = created.LogicGroupId,
            LogicOperator = created.LogicOperator
        };
    }

    public async Task<bool> DeleteDependencyAsync(Guid dependencyId)
    {
        var dep = await _fieldRepo.GetDependencyByIdAsync(dependencyId);
        if (dep == null) return false;

        await _fieldRepo.DeleteDependencyAsync(dep);
        return true;
    }

    public async Task<IEnumerable<FieldDependencyDto>> GetDependenciesBySurveyAsync(Guid surveyId)
    {
        var deps = await _fieldRepo.GetDependenciesBySurveyAsync(surveyId);
        return deps.Select(d => new FieldDependencyDto
        {
            Id = d.Id,
            FieldId = d.FieldId,
            DependsOnFieldId = d.DependsOnFieldId,
            Condition = d.Condition,
            Value = d.Value,
            Action = d.Action,
            OptionMap = d.OptionMap,
            LogicGroupId = d.LogicGroupId,
            LogicOperator = d.LogicOperator
        });
    }

    /// <summary>
    /// Validates the dependency graph for circular references.
    /// Returns null if valid, or an error message if circular.
    /// </summary>
    public async Task<string?> ValidateDependencies(Guid surveyId)
    {
        var fields = await _fieldRepo.GetBySurveyIdWithDependenciesAsync(surveyId);
        var fieldList = fields.ToList();

        // Build adjacency list: fieldId -> list of fields it depends on
        var graph = new Dictionary<Guid, List<Guid>>();
        foreach (var field in fieldList)
        {
            graph[field.Id] = field.Dependencies.Select(d => d.DependsOnFieldId).ToList();
        }

        // DFS-based cycle detection
        var visited = new HashSet<Guid>();
        var inStack = new HashSet<Guid>();

        foreach (var fieldId in graph.Keys)
        {
            if (HasCycle(fieldId, graph, visited, inStack))
            {
                return "Circular dependency detected. Please check your field logic rules.";
            }
        }

        return null;
    }

    private static bool HasCycle(Guid node, Dictionary<Guid, List<Guid>> graph, HashSet<Guid> visited, HashSet<Guid> inStack)
    {
        if (inStack.Contains(node)) return true;
        if (visited.Contains(node)) return false;

        visited.Add(node);
        inStack.Add(node);

        if (graph.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (HasCycle(neighbor, graph, visited, inStack))
                    return true;
            }
        }

        inStack.Remove(node);
        return false;
    }

    private static SurveyFieldDto MapToDto(SurveyField f) => new()
    {
        Id = f.Id,
        SurveyId = f.SurveyId,
        FieldType = f.FieldType,
        Label = f.Label,
        Description = f.Description,
        Placeholder = f.Placeholder,
        IsRequired = f.IsRequired,
        SortOrder = f.SortOrder,
        Options = f.Options,
        Validation = f.Validation,
        SectionTitle = f.SectionTitle,
        DefaultValue = f.DefaultValue,
        MatrixRows = f.MatrixRows,
        MatrixColumns = f.MatrixColumns,
        Dependencies = f.Dependencies?.Select(d => new FieldDependencyDto
        {
            Id = d.Id,
            FieldId = d.FieldId,
            DependsOnFieldId = d.DependsOnFieldId,
            Condition = d.Condition,
            Value = d.Value,
            Action = d.Action,
            OptionMap = d.OptionMap,
            LogicGroupId = d.LogicGroupId,
            LogicOperator = d.LogicOperator
        }).ToList() ?? new()
    };
}
