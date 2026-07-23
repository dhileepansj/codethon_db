using DCView.Hackathon.Application.DTOs.Survey;

namespace DCView.Hackathon.Application.Interfaces;

public interface ISurveyDashboardService
{
    Task<SurveyDashboardDto?> GetDashboardAsync(Guid surveyId);
    Task<IEnumerable<SurveyResponseDto>> GetResponsesAsync(Guid surveyId, int page = 1, int pageSize = 50);
    Task<SurveyResponseDto?> GetResponseDetailAsync(Guid responseId);
    Task<IEnumerable<FieldAnalyticsDto>> GetAnalyticsAsync(Guid surveyId);
    Task<byte[]> ExportResponsesAsync(Guid surveyId);
}
