using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class SurveyRepository : ISurveyRepository
{
    private readonly HackathonDbContext _db;

    public SurveyRepository(HackathonDbContext db) => _db = db;

    public async Task<Survey?> GetByIdAsync(Guid id)
        => await _db.Surveys.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

    public async Task<Survey?> GetByIdWithFieldsAsync(Guid id)
        => await _db.Surveys
            .Include(s => s.Fields.OrderBy(f => f.SortOrder))
                .ThenInclude(f => f.Dependencies)
            .Include(s => s.EmailSettings)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

    public async Task<IEnumerable<Survey>> GetAllAsync(bool includeDeleted = false)
        => await _db.Surveys
            .Where(s => includeDeleted || !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public async Task<Survey> CreateAsync(Survey survey)
    {
        _db.Surveys.Add(survey);
        await _db.SaveChangesAsync();
        return survey;
    }

    public async Task UpdateAsync(Survey survey)
    {
        survey.UpdatedAt = DateTime.UtcNow;
        _db.Surveys.Update(survey);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await _db.Surveys.AnyAsync(s => s.Id == id && !s.IsDeleted);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
