using DCView.Hackathon.Application.DTOs.Survey;

namespace DCView.Hackathon.Application.Interfaces;

public interface ISurveyService
{
    Task<IEnumerable<SurveyDto>> GetAllSurveysAsync();
    Task<SurveyDetailDto?> GetSurveyByIdAsync(Guid id);
    Task<SurveyDto> CreateSurveyAsync(CreateSurveyDto dto, int userId);
    Task<SurveyDto?> UpdateSurveyAsync(Guid id, UpdateSurveyDto dto);
    Task<bool> DeleteSurveyAsync(Guid id);
    Task<SurveyDto?> UpdateStatusAsync(Guid id, UpdateSurveyStatusDto dto);
    Task<SurveyDto?> CloneSurveyAsync(Guid id, int userId);
}
