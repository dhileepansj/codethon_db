using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class SurveyDistributionRepository : ISurveyDistributionRepository
{
    private readonly HackathonDbContext _db;

    public SurveyDistributionRepository(HackathonDbContext db) => _db = db;

    public async Task<SurveyDistribution?> GetByIdAsync(Guid id)
        => await _db.SurveyDistributions
            .Include(d => d.Participant)
            .Include(d => d.Survey)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<SurveyDistribution?> GetByTokenAsync(string token)
        => await _db.SurveyDistributions
            .Include(d => d.Participant)
            .Include(d => d.Survey)
                .ThenInclude(s => s!.Fields.OrderBy(f => f.SortOrder))
                    .ThenInclude(f => f.Dependencies)
            .FirstOrDefaultAsync(d => d.Token == token);

    public async Task<SurveyDistribution?> GetByParticipantAndSurveyAsync(Guid participantId, Guid surveyId)
        => await _db.SurveyDistributions
            .FirstOrDefaultAsync(d => d.ParticipantId == participantId && d.SurveyId == surveyId);

    public async Task<IEnumerable<SurveyDistribution>> GetBySurveyIdAsync(Guid surveyId)
        => await _db.SurveyDistributions
            .Include(d => d.Participant)
            .Include(d => d.Reminders)
            .Where(d => d.SurveyId == surveyId)
            .ToListAsync();

    public async Task<SurveyDistribution> CreateAsync(SurveyDistribution distribution)
    {
        _db.SurveyDistributions.Add(distribution);
        await _db.SaveChangesAsync();
        return distribution;
    }

    public async Task CreateBulkAsync(IEnumerable<SurveyDistribution> distributions)
    {
        _db.SurveyDistributions.AddRange(distributions);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SurveyDistribution distribution)
    {
        _db.SurveyDistributions.Update(distribution);
        await _db.SaveChangesAsync();
    }

    // Reminder logs
    public async Task<SurveyReminderLog> CreateReminderLogAsync(SurveyReminderLog log)
    {
        _db.SurveyReminderLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    public async Task<IEnumerable<SurveyReminderLog>> GetReminderLogsByDistributionAsync(Guid distributionId)
        => await _db.SurveyReminderLogs
            .Where(r => r.DistributionId == distributionId)
            .OrderByDescending(r => r.SentAt)
            .ToListAsync();

    public async Task<int> GetReminderCountAsync(Guid distributionId)
        => await _db.SurveyReminderLogs.CountAsync(r => r.DistributionId == distributionId);

    // Email settings
    public async Task<SurveyEmailSettings?> GetEmailSettingsAsync(Guid surveyId)
        => await _db.SurveyEmailSettings.FirstOrDefaultAsync(s => s.SurveyId == surveyId);

    public async Task<SurveyEmailSettings> CreateEmailSettingsAsync(SurveyEmailSettings settings)
    {
        _db.SurveyEmailSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    public async Task UpdateEmailSettingsAsync(SurveyEmailSettings settings)
    {
        _db.SurveyEmailSettings.Update(settings);
        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
