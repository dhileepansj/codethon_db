using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface ISurveyFieldRepository
{
    Task<SurveyField?> GetByIdAsync(Guid id);
    Task<IEnumerable<SurveyField>> GetBySurveyIdAsync(Guid surveyId);
    Task<IEnumerable<SurveyField>> GetBySurveyIdWithDependenciesAsync(Guid surveyId);
    Task<SurveyField> CreateAsync(SurveyField field);
    Task UpdateAsync(SurveyField field);
    Task DeleteAsync(SurveyField field);
    Task<int> GetMaxSortOrderAsync(Guid surveyId);

    // Dependencies
    Task<SurveyFieldDependency> CreateDependencyAsync(SurveyFieldDependency dependency);
    Task DeleteDependencyAsync(SurveyFieldDependency dependency);
    Task<SurveyFieldDependency?> GetDependencyByIdAsync(Guid id);
    Task<IEnumerable<SurveyFieldDependency>> GetDependenciesByFieldIdAsync(Guid fieldId);
    Task<IEnumerable<SurveyFieldDependency>> GetDependenciesBySurveyAsync(Guid surveyId);

    Task SaveChangesAsync(CancellationToken ct = default);
}
