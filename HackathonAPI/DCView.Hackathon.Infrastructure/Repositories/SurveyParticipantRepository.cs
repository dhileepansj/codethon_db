using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class SurveyParticipantRepository : ISurveyParticipantRepository
{
    private readonly HackathonDbContext _db;

    public SurveyParticipantRepository(HackathonDbContext db) => _db = db;

    public async Task<SurveyParticipant?> GetByIdAsync(Guid id)
        => await _db.SurveyParticipants
            .Include(p => p.Distribution)
            .Include(p => p.StatusLog)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<SurveyParticipant?> GetByEmailAndSurveyAsync(string email, Guid surveyId)
        => await _db.SurveyParticipants
            .Include(p => p.Distribution)
            .Include(p => p.StatusLog)
            .FirstOrDefaultAsync(p => p.EmployeeEmail.ToLower() == email.ToLower() && p.SurveyId == surveyId);

    public async Task<IEnumerable<SurveyParticipant>> GetBySurveyIdAsync(Guid surveyId)
        => await _db.SurveyParticipants
            .Include(p => p.Distribution)
            .Include(p => p.StatusLog)
            .Where(p => p.SurveyId == surveyId)
            .OrderBy(p => p.EmployeeName)
            .ToListAsync();

    public async Task<IEnumerable<SurveyParticipant>> GetBySurveyAndStatusAsync(Guid surveyId, params SurveyParticipantStatus[] statuses)
        => await _db.SurveyParticipants
            .Include(p => p.Distribution)
            .Where(p => p.SurveyId == surveyId && statuses.Contains(p.Status))
            .OrderBy(p => p.EmployeeName)
            .ToListAsync();

    public async Task<SurveyParticipant> CreateAsync(SurveyParticipant participant)
    {
        _db.SurveyParticipants.Add(participant);
        await _db.SaveChangesAsync();
        return participant;
    }

    public async Task CreateBulkAsync(IEnumerable<SurveyParticipant> participants)
    {
        _db.SurveyParticipants.AddRange(participants);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SurveyParticipant participant)
    {
        _db.SurveyParticipants.Update(participant);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(SurveyParticipant participant)
    {
        _db.SurveyParticipants.Remove(participant);
        await _db.SaveChangesAsync();
    }

    // Status log
    public async Task<SurveyParticipantStatusLog?> GetStatusLogAsync(Guid participantId)
        => await _db.SurveyParticipantStatusLogs
            .FirstOrDefaultAsync(l => l.ParticipantId == participantId);

    public async Task CreateStatusLogAsync(SurveyParticipantStatusLog log)
    {
        _db.SurveyParticipantStatusLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusLogAsync(SurveyParticipantStatusLog log)
    {
        _db.SurveyParticipantStatusLogs.Update(log);
        await _db.SaveChangesAsync();
    }

    public async Task<int> CountBySurveyAsync(Guid surveyId)
        => await _db.SurveyParticipants.CountAsync(p => p.SurveyId == surveyId);

    public async Task<int> CountBySurveyAndStatusAsync(Guid surveyId, SurveyParticipantStatus status)
        => await _db.SurveyParticipants.CountAsync(p => p.SurveyId == surveyId && p.Status == status);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
