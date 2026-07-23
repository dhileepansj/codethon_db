using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface ISurveyResponseRepository
{
    Task<SurveyResponse?> GetByIdAsync(Guid id);
    Task<SurveyResponse?> GetByIdWithAnswersAsync(Guid id);
    Task<IEnumerable<SurveyResponse>> GetBySurveyIdAsync(Guid surveyId, int page = 1, int pageSize = 50);
    Task<SurveyResponse?> GetByParticipantAndSurveyAsync(Guid participantId, Guid surveyId);
    Task<SurveyResponse> CreateAsync(SurveyResponse response);
    Task<int> CountBySurveyAsync(Guid surveyId);
    Task<bool> HasRespondedAsync(Guid participantId, Guid surveyId);

    Task SaveChangesAsync(CancellationToken ct = default);
}
