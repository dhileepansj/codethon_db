using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Domain.Repositories;

public interface ISurveyOtpRepository
{
    Task<SurveyOtp?> GetLatestByParticipantAndSurveyAsync(Guid participantId, Guid surveyId);
    Task<SurveyOtp> CreateAsync(SurveyOtp otp);
    Task UpdateAsync(SurveyOtp otp);
    Task InvalidateAllForParticipantAsync(Guid participantId, Guid surveyId);

    Task SaveChangesAsync(CancellationToken ct = default);
}
