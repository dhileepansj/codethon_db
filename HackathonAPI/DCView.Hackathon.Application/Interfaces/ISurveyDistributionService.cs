using DCView.Hackathon.Application.DTOs.Survey;
using Microsoft.AspNetCore.Http;

namespace DCView.Hackathon.Application.Interfaces;

public interface ISurveyDistributionService
{
    // Participants
    Task<IEnumerable<SurveyParticipantDto>> GetParticipantsAsync(Guid surveyId);
    Task<IEnumerable<SurveyParticipantDto>> GetPendingParticipantsAsync(Guid surveyId);
    Task<BulkUploadResultDto> BulkUploadAsync(Guid surveyId, Stream fileStream, string fileName);
    Task<bool> DeleteParticipantAsync(Guid participantId);

    // Decline
    Task<bool> DeclineParticipantAsync(Guid participantId, DeclineParticipantDto dto, IFormFile? attachment, int adminUserId);
    Task<bool> ResetParticipantStatusAsync(Guid participantId);

    // Email
    Task<SurveyEmailSettingsDto?> GetEmailSettingsAsync(Guid surveyId);
    Task<SurveyEmailSettingsDto> UpdateEmailSettingsAsync(Guid surveyId, UpdateEmailSettingsDto dto);

    // Distribution
    Task<int> DistributeAsync(Guid surveyId);
    Task<int> SendReminderAsync(Guid surveyId, SendReminderDto dto);
    Task<byte[]> GetParticipantTemplateAsync();
}
