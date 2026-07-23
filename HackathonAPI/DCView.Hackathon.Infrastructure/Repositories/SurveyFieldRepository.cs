using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class SurveyFieldRepository : ISurveyFieldRepository
{
    private readonly HackathonDbContext _db;

    public SurveyFieldRepository(HackathonDbContext db) => _db = db;

    public async Task<SurveyField?> GetByIdAsync(Guid id)
        => await _db.SurveyFields
            .Include(f => f.Dependencies)
            .FirstOrDefaultAsync(f => f.Id == id);

    public async Task<IEnumerable<SurveyField>> GetBySurveyIdAsync(Guid surveyId)
        => await _db.SurveyFields
            .Where(f => f.SurveyId == surveyId)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

    public async Task<IEnumerable<SurveyField>> GetBySurveyIdWithDependenciesAsync(Guid surveyId)
        => await _db.SurveyFields
            .Include(f => f.Dependencies)
            .Where(f => f.SurveyId == surveyId)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

    public async Task<SurveyField> CreateAsync(SurveyField field)
    {
        _db.SurveyFields.Add(field);
        await _db.SaveChangesAsync();
        return field;
    }

    public async Task UpdateAsync(SurveyField field)
    {
        _db.SurveyFields.Update(field);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(SurveyField field)
    {
        _db.SurveyFields.Remove(field);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetMaxSortOrderAsync(Guid surveyId)
    {
        var max = await _db.SurveyFields
            .Where(f => f.SurveyId == surveyId)
            .MaxAsync(f => (int?)f.SortOrder);
        return max ?? 0;
    }

    // Dependencies
    public async Task<SurveyFieldDependency> CreateDependencyAsync(SurveyFieldDependency dependency)
    {
        _db.SurveyFieldDependencies.Add(dependency);
        await _db.SaveChangesAsync();
        return dependency;
    }

    public async Task DeleteDependencyAsync(SurveyFieldDependency dependency)
    {
        _db.SurveyFieldDependencies.Remove(dependency);
        await _db.SaveChangesAsync();
    }

    public async Task<SurveyFieldDependency?> GetDependencyByIdAsync(Guid id)
        => await _db.SurveyFieldDependencies.FirstOrDefaultAsync(d => d.Id == id);

    public async Task<IEnumerable<SurveyFieldDependency>> GetDependenciesByFieldIdAsync(Guid fieldId)
        => await _db.SurveyFieldDependencies
            .Where(d => d.FieldId == fieldId)
            .ToListAsync();

    public async Task<IEnumerable<SurveyFieldDependency>> GetDependenciesBySurveyAsync(Guid surveyId)
        => await _db.SurveyFieldDependencies
            .Include(d => d.Field)
            .Include(d => d.DependsOnField)
            .Where(d => d.Field != null && d.Field.SurveyId == surveyId)
            .ToListAsync();

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
