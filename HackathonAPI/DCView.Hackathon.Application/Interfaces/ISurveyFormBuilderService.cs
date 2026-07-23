using DCView.Hackathon.Application.DTOs.Survey;

namespace DCView.Hackathon.Application.Interfaces;

public interface ISurveyFormBuilderService
{
    Task<IEnumerable<SurveyFieldDto>> GetFieldsAsync(Guid surveyId);
    Task<SurveyFieldDto?> GetFieldByIdAsync(Guid fieldId);
    Task<SurveyFieldDto> CreateFieldAsync(Guid surveyId, CreateFieldDto dto);
    Task<SurveyFieldDto?> UpdateFieldAsync(Guid fieldId, UpdateFieldDto dto);
    Task<bool> DeleteFieldAsync(Guid fieldId);
    Task<bool> ReorderFieldsAsync(Guid surveyId, ReorderFieldsDto dto);

    // Dependencies
    Task<FieldDependencyDto> CreateDependencyAsync(Guid fieldId, CreateDependencyDto dto);
    Task<bool> DeleteDependencyAsync(Guid dependencyId);
    Task<IEnumerable<FieldDependencyDto>> GetDependenciesBySurveyAsync(Guid surveyId);
    Task<string?> ValidateDependencies(Guid surveyId);
}
