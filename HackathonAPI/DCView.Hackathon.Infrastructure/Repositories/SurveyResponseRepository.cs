using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class SurveyResponseRepository : ISurveyResponseRepository
{
    private readonly HackathonDbContext _db;

    public SurveyResponseRepository(HackathonDbContext db) => _db = db;

    public async Task<SurveyResponse?> GetByIdAsync(Guid id)
        => await _db.SurveyResponses
            .Include(r => r.Participant)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<SurveyResponse?> GetByIdWithAnswersAsync(Guid id)
        => await _db.SurveyResponses
            .Include(r => r.Answers)
                .ThenInclude(a => a.Field)
            .Include(r => r.Participant)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<SurveyResponse>> GetBySurveyIdAsync(Guid surveyId, int page = 1, int pageSize = 50)
        => await _db.SurveyResponses
            .Include(r => r.Participant)
            .Where(r => r.SurveyId == surveyId)
            .OrderByDescending(r => r.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<SurveyResponse?> GetByParticipantAndSurveyAsync(Guid participantId, Guid surveyId)
        => await _db.SurveyResponses
            .FirstOrDefaultAsync(r => r.ParticipantId == participantId && r.SurveyId == surveyId);

    public async Task<SurveyResponse> CreateAsync(SurveyResponse response)
    {
        _db.SurveyResponses.Add(response);
        await _db.SaveChangesAsync();
        return response;
    }

    public async Task<int> CountBySurveyAsync(Guid surveyId)
        => await _db.SurveyResponses.CountAsync(r => r.SurveyId == surveyId);

    public async Task<bool> HasRespondedAsync(Guid participantId, Guid surveyId)
        => await _db.SurveyResponses.AnyAsync(r => r.ParticipantId == participantId && r.SurveyId == surveyId);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
