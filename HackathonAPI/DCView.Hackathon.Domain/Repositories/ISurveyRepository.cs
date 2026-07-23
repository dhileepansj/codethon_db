using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Repositories;

public interface ISurveyRepository
{
    Task<Survey?> GetByIdAsync(Guid id);
    Task<Survey?> GetByIdWithFieldsAsync(Guid id);
    Task<IEnumerable<Survey>> GetAllAsync(bool includeDeleted = false);
    Task<Survey> CreateAsync(Survey survey);
    Task UpdateAsync(Survey survey);
    Task<bool> ExistsAsync(Guid id);
    Task SaveChangesAsync(CancellationToken ct = default);
}
