using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Application.DTOs.Mcq;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.API.Controllers;

[Route("api/mcq")]
[ApiController]
[Authorize]
public class McqController : ControllerBase
{
    private readonly IMcqService _mcqService;

    public McqController(IMcqService mcqService) => _mcqService = mcqService;

    // ═══════════════════════════════════════════════════════════════
    // ADMIN: Assessment Management
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("assessments")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetAllAssessments()
    {
        var result = await _mcqService.GetAllAssessmentsAsync();
        return Ok(result);
    }

    [HttpGet("assessments/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetAssessment(int id)
    {
        var result = await _mcqService.GetAssessmentByIdAsync(id);
        if (result == null) return NotFound(new { message = "Assessment not found" });
        return Ok(result);
    }

    [HttpPost("assessments")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required" });

        var validTypes = new[] { "SQL", "MCQ", "ManualTesting" };
        if (!validTypes.Contains(request.Type))
            return BadRequest(new { message = "Type must be 'SQL', 'MCQ', or 'ManualTesting'" });

        var admin = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        var result = await _mcqService.CreateAssessmentAsync(request, admin);
        return Ok(result);
    }

    [HttpPut("assessments/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateAssessment(int id, [FromBody] UpdateAssessmentDto request)
    {
        var admin = User.FindFirst(ClaimTypes.Name)?.Value ?? "SuperAdmin";
        var success = await _mcqService.UpdateAssessmentAsync(id, request, admin);
        if (!success) return NotFound(new { message = "Assessment not found" });
        return Ok(new { message = "Assessment updated" });
    }

    [HttpDelete("assessments/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteAssessment(int id)
    {
        var success = await _mcqService.DeleteAssessmentAsync(id);
        if (!success) return NotFound(new { message = "Assessment not found" });
        return Ok(new { message = "Assessment and all its questions deleted" });
    }

    // ═══════════════════════════════════════════════════════════════
    // ADMIN: Question Bank
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("assessments/{assessmentId:int}/questions")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetQuestions(int assessmentId)
    {
        var result = await _mcqService.GetQuestionsByAssessmentAsync(assessmentId);
        return Ok(result);
    }

    [HttpPost("assessments/{assessmentId:int}/questions")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddQuestion(int assessmentId, [FromBody] CreateMcqQuestionDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { message = "Question text is required" });

        var result = await _mcqService.AddQuestionAsync(assessmentId, request);
        return Ok(result);
    }

    [HttpPost("assessments/{assessmentId:int}/questions/bulk")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> BulkUploadQuestions(int assessmentId, [FromBody] List<CreateMcqQuestionDto> questions)
    {
        if (questions == null || questions.Count == 0)
            return BadRequest(new { message = "At least one question is required" });

        var count = await _mcqService.BulkUploadQuestionsAsync(assessmentId, questions);
        return Ok(new { message = $"{count} questions uploaded successfully", count });
    }

    [HttpPut("questions/{questionId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] CreateMcqQuestionDto request)
    {
        var success = await _mcqService.UpdateQuestionAsync(questionId, request);
        if (!success) return NotFound(new { message = "Question not found" });
        return Ok(new { message = "Question updated" });
    }

    [HttpDelete("questions/{questionId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        var success = await _mcqService.DeleteQuestionAsync(questionId);
        if (!success) return NotFound(new { message = "Question not found" });
        return Ok(new { message = "Question deleted" });
    }

    // ═══════════════════════════════════════════════════════════════
    // ADMIN: Results
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("assessments/{assessmentId:int}/results")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetResults(int assessmentId)
    {
        var results = await _mcqService.GetAllTestResultsAsync(assessmentId);
        return Ok(results);
    }

    [HttpGet("assessments/{assessmentId:int}/review/{userLoginId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetUserReview(int assessmentId, string userLoginId)
    {
        var db = HttpContext.RequestServices.GetRequiredService<DCView.Hackathon.Infrastructure.Data.HackathonDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserID == userLoginId.ToUpper());
        if (user == null) return NotFound(new { message = "User not found" });

        var test = await db.McqTests
            .Include(t => t.Answers).ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.AssessmentId == assessmentId && t.IsSubmitted);

        if (test == null) return NotFound(new { message = "No submitted test found for this user" });

        var questions = test.Answers.OrderBy(a => a.QuestionIndex).Select(a => new
        {
            questionIndex = a.QuestionIndex,
            question = a.Question.Question,
            optionA = a.Question.OptionA,
            optionB = a.Question.OptionB,
            optionC = a.Question.OptionC,
            optionD = a.Question.OptionD,
            correctAnswer = a.Question.CorrectAnswer,
            selectedAnswer = a.SelectedAnswer,
            isCorrect = a.IsCorrect,
            marksAwarded = a.MarksAwarded,
            complexity = a.Question.Complexity,
            category = a.Question.Category,
            explanation = a.Question.Explanation
        }).ToList();

        return Ok(new
        {
            userLoginId = user.UserID,
            fullName = user.FullName,
            score = test.Score,
            maxScore = test.MaxScore,
            percentage = test.Percentage,
            correct = test.Correct,
            wrong = test.Wrong,
            skipped = test.Skipped,
            totalQuestions = test.TotalQuestions,
            timeSpentSeconds = test.TimeSpentSeconds,
            submittedAt = test.SubmittedAt,
            passed = test.Passed,
            questions
        });
    }

    [HttpGet("assessments/{assessmentId:int}/respondents")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetRespondents(int assessmentId)
    {
        var db = HttpContext.RequestServices.GetRequiredService<DCView.Hackathon.Infrastructure.Data.HackathonDbContext>();
        var respondents = await db.McqTests
            .Include(t => t.User)
            .Where(t => t.AssessmentId == assessmentId && t.IsSubmitted)
            .OrderBy(t => t.User.UserID)
            .Select(t => new { userLoginId = t.User.UserID, fullName = t.User.FullName, score = t.Score, maxScore = t.MaxScore, percentage = t.Percentage })
            .ToListAsync();
        return Ok(respondents);
    }

    [HttpGet("assessments/{assessmentId:int}/results/user/{userId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetUserDetailedResult(int assessmentId, string userId)
    {
        var db = HttpContext.RequestServices.GetRequiredService<DCView.Hackathon.Infrastructure.Data.HackathonDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserID == userId.ToUpper());
        if (user == null) return NotFound(new { message = "User not found" });

        var test = await db.McqTests
            .Include(t => t.Assessment)
            .Include(t => t.Answers).ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.AssessmentId == assessmentId && t.IsSubmitted);

        if (test == null) return NotFound(new { message = "No submitted test found for this user" });

        var result = new
        {
            userID = user.UserID,
            fullName = user.FullName,
            score = test.Score,
            maxScore = test.MaxScore,
            percentage = test.Percentage,
            correct = test.Correct,
            wrong = test.Wrong,
            skipped = test.Skipped,
            totalQuestions = test.TotalQuestions,
            passed = test.Passed,
            startedAt = test.StartedAt,
            submittedAt = test.SubmittedAt,
            timeSpentSeconds = test.TimeSpentSeconds,
            answers = test.Answers.OrderBy(a => a.QuestionIndex).Select(a => new
            {
                questionIndex = a.QuestionIndex,
                question = a.Question.Question,
                optionA = a.Question.OptionA,
                optionB = a.Question.OptionB,
                optionC = a.Question.OptionC,
                optionD = a.Question.OptionD,
                correctAnswer = a.Question.CorrectAnswer,
                selectedAnswer = a.SelectedAnswer,
                isCorrect = a.IsCorrect,
                marksAwarded = a.MarksAwarded,
                complexity = a.Question.Complexity,
                category = a.Question.Category
            })
        };

        return Ok(result);
    }

    [HttpGet("assessments/{assessmentId:int}/results/download")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DownloadResults(int assessmentId)
    {
        var results = await _mcqService.GetAllTestResultsAsync(assessmentId);
        var resultList = results.ToList();

        if (resultList.Count == 0)
            return BadRequest(new { message = "No results to export" });

        // Build CSV
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("S.No,User ID,Full Name,Score,Max Score,Percentage,Correct,Wrong,Skipped,Total Questions,Passed,Auto Submitted,Time Spent (min),Started At,Submitted At");

        for (int i = 0; i < resultList.Count; i++)
        {
            var r = resultList[i];
            var timeMins = r.TimeSpentSeconds.HasValue ? Math.Round(r.TimeSpentSeconds.Value / 60.0, 1).ToString() : "";
            sb.AppendLine($"{i + 1},\"{r.UserID}\",\"{r.FullName ?? ""}\",{r.Score},{r.MaxScore},{r.Percentage},{r.Correct},{r.Wrong},{r.Skipped},{r.TotalQuestions},{(r.Passed == true ? "Yes" : r.Passed == false ? "No" : "N/A")},{(r.IsAutoSubmitted ? "Yes" : "No")},{timeMins},{r.StartedAt:yyyy-MM-dd HH:mm},{r.SubmittedAt:yyyy-MM-dd HH:mm}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var assessmentTitle = resultList.FirstOrDefault()?.AssessmentTitle ?? "MCQ";
        return File(bytes, "text/csv", $"MCQ_Results_{assessmentTitle}_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("assessments/{assessmentId:int}/results/download-detailed")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DownloadDetailedResults(int assessmentId)
    {
        var db = HttpContext.RequestServices.GetRequiredService<DCView.Hackathon.Infrastructure.Data.HackathonDbContext>();

        var tests = await db.McqTests
            .Include(t => t.User)
            .Include(t => t.Answers).ThenInclude(a => a.Question)
            .Where(t => t.AssessmentId == assessmentId && t.IsSubmitted)
            .OrderBy(t => t.User.UserID)
            .ToListAsync();

        if (tests.Count == 0)
            return BadRequest(new { message = "No results to export" });

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("User ID,Full Name,Score,Percentage,Q.No,Question,Selected Answer,Correct Answer,Is Correct,Marks Awarded,Complexity,Category");

        foreach (var test in tests)
        {
            foreach (var answer in test.Answers.OrderBy(a => a.QuestionIndex))
            {
                var q = answer.Question;
                var selectedText = answer.SelectedAnswer != null
                    ? $"{answer.SelectedAnswer}. {GetOptionText(q, answer.SelectedAnswer)}"
                    : "(Skipped)";
                var correctText = $"{q.CorrectAnswer}. {GetOptionText(q, q.CorrectAnswer)}";

                sb.AppendLine($"\"{test.User.UserID}\",\"{test.User.FullName ?? ""}\",{test.Score},{test.Percentage},{answer.QuestionIndex},\"{EscapeCsv(q.Question)}\",\"{EscapeCsv(selectedText)}\",\"{EscapeCsv(correctText)}\",{(answer.IsCorrect == true ? "Yes" : answer.IsCorrect == false ? "No" : "Skipped")},{answer.MarksAwarded},\"{q.Complexity}\",\"{q.Category ?? ""}\"");
            }
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var assessment = await db.Set<DCView.Hackathon.Domain.Entities.Assessment>().FindAsync(assessmentId);
        return File(bytes, "text/csv", $"MCQ_Detailed_{assessment?.Title ?? "MCQ"}_{DateTime.Now:yyyyMMdd}.csv");
    }

    private static string GetOptionText(DCView.Hackathon.Domain.Entities.McqQuestion q, string option)
    {
        return option switch
        {
            "A" => q.OptionA,
            "B" => q.OptionB,
            "C" => q.OptionC,
            "D" => q.OptionD,
            _ => ""
        };
    }

    private static string EscapeCsv(string text)
    {
        return text.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
    }

    // ═══════════════════════════════════════════════════════════════
    // PARTICIPANT: Test Flow
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("test/info")]
    [Authorize(Roles = "Participant,SuperAdmin")]
    public async Task<IActionResult> GetTestInfo()
    {
        var userId = GetCurrentUserId();
        var info = await _mcqService.GetTestInfoAsync(userId);
        return Ok(info);
    }

    [HttpPost("test/start")]
    [Authorize(Roles = "Participant,SuperAdmin")]
    public async Task<IActionResult> StartTest()
    {
        var userId = GetCurrentUserId();
        var result = await _mcqService.StartTestAsync(userId);
        return Ok(result);
    }

    [HttpGet("test/status")]
    [Authorize(Roles = "Participant,SuperAdmin")]
    public async Task<IActionResult> GetTestStatus()
    {
        var userId = GetCurrentUserId();
        var status = await _mcqService.GetTestStatusAsync(userId);
        return Ok(status);
    }

    [HttpGet("test/questions/{questionIndex:int}")]
    [Authorize(Roles = "Participant,SuperAdmin")]
    public async Task<IActionResult> GetQuestion(int questionIndex)
    {
        var userId = GetCurrentUserId();
        var question = await _mcqService.GetQuestionAsync(userId, questionIndex);
        if (question == null) return NotFound(new { message = "Question not found" });
        return Ok(question);
    }

    [HttpGet("test/questions")]
    [Authorize(Roles = "Participant,SuperAdmin")]
    public async Task<IActionResult> GetAllQuestions()
    {
        var userId = GetCurrentUserId();
        var questions = await _mcqService.GetAllQuestionsAsync(userId);
        return Ok(questions);
    }

    [HttpPost("test/answer")]
    [Authorize(Roles = "Participant,SuperAdmin")]
    public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerDto request)
    {
        var userId = GetCurrentUserId();
        var success = await _mcqService.SaveAnswerAsync(userId, request);
        if (!success) return NotFound(new { message = "Question not found in your test" });
        return Ok(new { message = "Answer saved" });
    }

    [HttpPost("test/flag")]
    [Authorize(Roles = "Participant,SuperAdmin")]
    public async Task<IActionResult> FlagQuestion([FromBody] FlagDto request)
    {
        var userId = GetCurrentUserId();
        var success = await _mcqService.FlagQuestionAsync(userId, request.QuestionId, request.IsFlagged);
        if (!success) return NotFound(new { message = "Question not found in your test" });
        return Ok(new { message = request.IsFlagged ? "Question flagged" : "Flag removed" });
    }

    [HttpPost("test/submit")]
    [Authorize(Roles = "Participant,SuperAdmin")]
    public async Task<IActionResult> SubmitTest([FromBody] SubmitTestDto? request)
    {
        var userId = GetCurrentUserId();
        var result = await _mcqService.SubmitTestAsync(userId, request?.IsAutoSubmit ?? false);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim!);
    }
}

public class FlagDto
{
    public int QuestionId { get; set; }
    public bool IsFlagged { get; set; }
}

public class SubmitTestDto
{
    public bool IsAutoSubmit { get; set; }
}
