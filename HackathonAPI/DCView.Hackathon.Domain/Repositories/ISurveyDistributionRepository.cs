using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface ISurveyDistributionRepository
{
    Task<SurveyDistribution?> GetByIdAsync(Guid id);
    Task<SurveyDistribution?> GetByTokenAsync(string token);
    Task<SurveyDistribution?> GetByShortCodeAsync(string shortCode);
    Task<SurveyDistribution?> GetByParticipantAndSurveyAsync(Guid participantId, Guid surveyId);
    Task<IEnumerable<SurveyDistribution>> GetBySurveyIdAsync(Guid surveyId);
    Task<SurveyDistribution> CreateAsync(SurveyDistribution distribution);
    Task CreateBulkAsync(IEnumerable<SurveyDistribution> distributions);
    Task UpdateAsync(SurveyDistribution distribution);

    // Reminder logs
    Task<SurveyReminderLog> CreateReminderLogAsync(SurveyReminderLog log);
    Task<IEnumerable<SurveyReminderLog>> GetReminderLogsByDistributionAsync(Guid distributionId);
    Task<int> GetReminderCountAsync(Guid distributionId);

    // Email settings
    Task<SurveyEmailSettings?> GetEmailSettingsAsync(Guid surveyId);
    Task<SurveyEmailSettings> CreateEmailSettingsAsync(SurveyEmailSettings settings);
    Task UpdateEmailSettingsAsync(SurveyEmailSettings settings);

    Task SaveChangesAsync(CancellationToken ct = default);
}
