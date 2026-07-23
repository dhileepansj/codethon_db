using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Repositories;

public interface ISurveyParticipantRepository
{
    Task<SurveyParticipant?> GetByIdAsync(Guid id);
    Task<SurveyParticipant?> GetByEmailAndSurveyAsync(string email, Guid surveyId);
    Task<IEnumerable<SurveyParticipant>> GetBySurveyIdAsync(Guid surveyId);
    Task<IEnumerable<SurveyParticipant>> GetBySurveyAndStatusAsync(Guid surveyId, params SurveyParticipantStatus[] statuses);
    Task<SurveyParticipant> CreateAsync(SurveyParticipant participant);
    Task CreateBulkAsync(IEnumerable<SurveyParticipant> participants);
    Task UpdateAsync(SurveyParticipant participant);
    Task DeleteAsync(SurveyParticipant participant);

    // Status log
    Task<SurveyParticipantStatusLog?> GetStatusLogAsync(Guid participantId);
    Task CreateStatusLogAsync(SurveyParticipantStatusLog log);
    Task UpdateStatusLogAsync(SurveyParticipantStatusLog log);

    Task<int> CountBySurveyAsync(Guid surveyId);
    Task<int> CountBySurveyAndStatusAsync(Guid surveyId, SurveyParticipantStatus status);

    Task SaveChangesAsync(CancellationToken ct = default);
}
