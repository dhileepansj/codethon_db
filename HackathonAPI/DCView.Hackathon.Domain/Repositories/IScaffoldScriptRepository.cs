using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface IScaffoldScriptRepository
{
    Task<IEnumerable<ScaffoldScript>> GetAllActiveAsync();
    Task<IEnumerable<ScaffoldScript>> GetAllAsync();
    Task<ScaffoldScript?> GetByIdAsync(int id);
    Task<ScaffoldScript> CreateAsync(ScaffoldScript script);
    Task UpdateAsync(ScaffoldScript script);
    Task DeleteAsync(int id);
}
