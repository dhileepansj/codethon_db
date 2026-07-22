using DCView.Hackathon.Application.DTOs.Mcq;

namespace DCView.Hackathon.Application.Interfaces;

/// <summary>
/// Service for MCQ assessment management (admin) and test execution (participant).
/// </summary>
public interface IMcqService
{
    // ─── Admin: Assessment CRUD ──────────────────────────────────
    Task<IEnumerable<AssessmentDto>> GetAllAssessmentsAsync();
    Task<AssessmentDto?> GetAssessmentByIdAsync(int id);
    Task<AssessmentDto> CreateAssessmentAsync(CreateAssessmentDto dto, string createdBy);
    Task<bool> UpdateAssessmentAsync(int id, UpdateAssessmentDto dto, string modifiedBy);
    Task<bool> DeleteAssessmentAsync(int id);

    // ─── Admin: Question Bank ────────────────────────────────────
    Task<IEnumerable<McqQuestionDto>> GetQuestionsByAssessmentAsync(int assessmentId);
    Task<McqQuestionDto> AddQuestionAsync(int assessmentId, CreateMcqQuestionDto dto);
    Task<int> BulkUploadQuestionsAsync(int assessmentId, IEnumerable<CreateMcqQuestionDto> questions);
    Task<bool> UpdateQuestionAsync(int questionId, CreateMcqQuestionDto dto);
    Task<bool> DeleteQuestionAsync(int questionId);

    // ─── Admin: Results ──────────────────────────────────────────
    Task<IEnumerable<McqTestResultDto>> GetAllTestResultsAsync(int assessmentId);
    Task<McqTestResultDto?> GetUserTestResultAsync(int userId, int assessmentId);

    // ─── Participant: Test Flow ──────────────────────────────────
    Task<McqTestInfoDto> GetTestInfoAsync(int userId);
    Task<StartTestResultDto> StartTestAsync(int userId);
    Task<McqTestStatusDto> GetTestStatusAsync(int userId);
    Task<McqQuestionForTestDto?> GetQuestionAsync(int userId, int questionIndex);
    Task<IEnumerable<McqQuestionForTestDto>> GetAllQuestionsAsync(int userId);
    Task<bool> SaveAnswerAsync(int userId, SaveAnswerDto dto);
    Task<bool> FlagQuestionAsync(int userId, int questionId, bool flagged);
    Task<McqSubmitResultDto> SubmitTestAsync(int userId, bool isAutoSubmit = false);
}
