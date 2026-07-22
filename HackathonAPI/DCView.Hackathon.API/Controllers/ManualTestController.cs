using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Application.DTOs.ManualTest;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Infrastructure.Data;
using DCView.Hackathon.Shared.Helpers;

namespace DCView.Hackathon.API.Controllers;

[Route("api/manual-test")]
[ApiController]
[Authorize]
public class ManualTestController : ControllerBase
{
    private readonly HackathonDbContext _db;

    public ManualTestController(HackathonDbContext db) => _db = db;

    // ═══════════════════════════════════════════════════════════════
    // PARTICIPANT: Workspace
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Get workspace data (assessment info + use case + existing scenarios)</summary>
    [HttpGet("workspace")]
    [Authorize(Roles = "Participant,SuperAdmin,Admin")]
    public async Task<IActionResult> GetWorkspace()
    {
        var userId = GetUserId();
        var user = await _db.Users.Include(u => u.Assessment).FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Assessment == null || user.Assessment.Type != "ManualTesting")
            return BadRequest(new { message = "No manual testing assessment assigned." });

        var assessment = user.Assessment;

        var scenarios = await _db.ManualTestScenarios
            .Where(s => s.UserId == userId && s.AssessmentId == assessment.Id)
            .OrderBy(s => s.SortOrder)
            .Select(s => new ManualTestScenarioDto
            {
                Id = s.Id, SNo = s.SNo, ScenarioId = s.ScenarioId, Scenario = s.Scenario,
                Description = s.Description, MustTest = s.MustTest,
                SortOrder = s.SortOrder, TestCaseCount = s.TestCases.Count
            })
            .ToListAsync();

        return Ok(new ManualTestWorkspaceDto
        {
            AssessmentId = assessment.Id,
            Title = assessment.Title,
            UseCaseHtml = assessment.UseCaseHtml,
            DurationMinutes = assessment.DurationMinutes,
            Scenarios = scenarios
        });
    }

    // ─── Scenarios CRUD ──────────────────────────────────────────

    [HttpPost("scenarios")]
    [Authorize(Roles = "Participant,SuperAdmin,Admin")]
    public async Task<IActionResult> SaveScenario([FromBody] SaveScenarioDto dto)
    {
        var userId = GetUserId();
        var user = await _db.Users.Include(u => u.Assessment).FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Assessment == null) return BadRequest(new { message = "No assessment assigned." });

        if (dto.Id.HasValue && dto.Id > 0)
        {
            // Update existing
            var scenario = await _db.ManualTestScenarios.FirstOrDefaultAsync(s => s.Id == dto.Id && s.UserId == userId);
            if (scenario == null) return NotFound(new { message = "Scenario not found" });

            scenario.SNo = dto.SNo;
            scenario.ScenarioId = dto.ScenarioId;
            scenario.Scenario = dto.Scenario;
            scenario.Description = dto.Description;
            scenario.MustTest = dto.MustTest;
            scenario.SortOrder = dto.SortOrder;
            scenario.ModifiedDate = DateTimeHelper.Now;
            await _db.SaveChangesAsync();

            return Ok(new { id = scenario.Id, message = "Scenario updated" });
        }
        else
        {
            // Create new
            var scenario = new ManualTestScenario
            {
                UserId = userId,
                AssessmentId = user.Assessment.Id,
                SNo = dto.SNo,
                ScenarioId = dto.ScenarioId,
                Scenario = dto.Scenario,
                Description = dto.Description,
                MustTest = dto.MustTest,
                SortOrder = dto.SortOrder,
                CreatedDate = DateTimeHelper.Now
            };
            _db.ManualTestScenarios.Add(scenario);
            await _db.SaveChangesAsync();

            return Ok(new { id = scenario.Id, message = "Scenario created" });
        }
    }

    [HttpDelete("scenarios/{id:int}")]
    [Authorize(Roles = "Participant,SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteScenario(int id)
    {
        var userId = GetUserId();
        var scenario = await _db.ManualTestScenarios
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (scenario == null) return NotFound(new { message = "Scenario not found" });

        // Delete test cases first
        var testCases = await _db.ManualTestCases.Where(c => c.ScenarioDbId == id).ToListAsync();
        if (testCases.Count > 0)
            _db.ManualTestCases.RemoveRange(testCases);

        _db.ManualTestScenarios.Remove(scenario);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Scenario deleted with {testCases.Count} test case(s)" });
    }

    // ─── Test Cases CRUD ─────────────────────────────────────────

    [HttpGet("scenarios/{scenarioId:int}/cases")]
    [Authorize(Roles = "Participant,SuperAdmin,Admin")]
    public async Task<IActionResult> GetTestCases(int scenarioId)
    {
        var userId = GetUserId();
        var scenario = await _db.ManualTestScenarios.FirstOrDefaultAsync(s => s.Id == scenarioId && s.UserId == userId);
        if (scenario == null) return NotFound(new { message = "Scenario not found" });

        var cases = await _db.ManualTestCases
            .Where(c => c.ScenarioDbId == scenarioId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new ManualTestCaseDto
            {
                Id = c.Id, ScenarioDbId = c.ScenarioDbId, SNo = c.SNo,
                ScenarioId = c.ScenarioId, TestCaseId = c.TestCaseId,
                TestCaseDescription = c.TestCaseDescription, StepNo = c.StepNo,
                InputSpecification = c.InputSpecification,
                InputTestData = c.InputTestData,
                ExpectedResult = c.ExpectedResult, SortOrder = c.SortOrder
            })
            .ToListAsync();

        return Ok(cases);
    }

    [HttpPost("cases")]
    [Authorize(Roles = "Participant,SuperAdmin,Admin")]
    public async Task<IActionResult> SaveTestCase([FromBody] SaveTestCaseBatchDto dto)
    {
        var userId = GetUserId();
        var scenario = await _db.ManualTestScenarios.FirstOrDefaultAsync(s => s.Id == dto.ScenarioDbId && s.UserId == userId);
        if (scenario == null) return NotFound(new { message = "Scenario not found" });

        if (dto.Id.HasValue && dto.Id > 0)
        {
            var tc = await _db.ManualTestCases.FirstOrDefaultAsync(c => c.Id == dto.Id && c.ScenarioDbId == dto.ScenarioDbId);
            if (tc == null) return NotFound(new { message = "Test case not found" });

            tc.SNo = dto.SNo;
            tc.ScenarioId = dto.ScenarioId;
            tc.TestCaseId = dto.TestCaseId;
            tc.TestCaseDescription = dto.TestCaseDescription;
            tc.StepNo = dto.StepNo;
            tc.InputSpecification = dto.InputSpecification;
            tc.InputTestData = dto.InputTestData;
            tc.ExpectedResult = dto.ExpectedResult;
            tc.SortOrder = dto.SortOrder;
            tc.ModifiedDate = DateTimeHelper.Now;
            await _db.SaveChangesAsync();

            return Ok(new { id = tc.Id, message = "Test case updated" });
        }
        else
        {
            var tc = new ManualTestCase
            {
                ScenarioDbId = dto.ScenarioDbId,
                SNo = dto.SNo,
                ScenarioId = dto.ScenarioId,
                TestCaseId = dto.TestCaseId,
                TestCaseDescription = dto.TestCaseDescription,
                StepNo = dto.StepNo,
                InputSpecification = dto.InputSpecification,
                InputTestData = dto.InputTestData,
                ExpectedResult = dto.ExpectedResult,
                SortOrder = dto.SortOrder,
                CreatedDate = DateTimeHelper.Now
            };
            _db.ManualTestCases.Add(tc);
            await _db.SaveChangesAsync();

            return Ok(new { id = tc.Id, message = "Test case created" });
        }
    }

    [HttpDelete("cases/{id:int}")]
    [Authorize(Roles = "Participant,SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteTestCase(int id)
    {
        var userId = GetUserId();
        var tc = await _db.ManualTestCases
            .Include(c => c.Scenario)
            .FirstOrDefaultAsync(c => c.Id == id && c.Scenario.UserId == userId);
        if (tc == null) return NotFound(new { message = "Test case not found" });

        _db.ManualTestCases.Remove(tc);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Test case deleted" });
    }

    // ─── Submit ────────────────────────────────────────────────────

    [HttpPost("submit")]
    [Authorize(Roles = "Participant,SuperAdmin,Admin")]
    public async Task<IActionResult> Submit()
    {
        var userId = GetUserId();
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.UserId == userId);
        if (session == null) return NotFound(new { message = "Session not found" });
        if (session.IsSubmitted) return BadRequest(new { message = "Already submitted" });

        session.IsSubmitted = true;
        session.SubmittedAt = DateTimeHelper.Now;

        // Audit log
        var user = await _db.Users.FindAsync(userId);
        _db.Set<SubmissionAuditLog>().Add(new SubmissionAuditLog
        {
            UserId = userId,
            UserLoginId = user?.UserID ?? "",
            Action = "Submitted",
            AssessmentType = "ManualTesting",
            PerformedBy = user?.UserID,
            EventTime = DateTimeHelper.Now
        });

        await _db.SaveChangesAsync();

        return Ok(new { message = "Submitted successfully" });
    }

    [HttpGet("submission-status")]
    [Authorize(Roles = "Participant,SuperAdmin,Admin")]
    public async Task<IActionResult> GetSubmissionStatus()
    {
        var userId = GetUserId();
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.UserId == userId);
        return Ok(new { isSubmitted = session?.IsSubmitted ?? false, submittedAt = session?.SubmittedAt });
    }

    // ═══════════════════════════════════════════════════════════════
    // ADMIN: Use Case Management
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("assessments/{assessmentId:int}/usecase")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetUseCase(int assessmentId)
    {
        var assessment = await _db.Set<Assessment>().FindAsync(assessmentId);
        if (assessment == null) return NotFound(new { message = "Assessment not found" });
        return Ok(new { assessmentId, title = assessment.Title, useCaseHtml = assessment.UseCaseHtml });
    }

    [HttpPost("assessments/{assessmentId:int}/usecase")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SaveUseCase(int assessmentId, [FromBody] SaveUseCaseDto request)
    {
        var assessment = await _db.Set<Assessment>().FindAsync(assessmentId);
        if (assessment == null) return NotFound(new { message = "Assessment not found" });

        assessment.UseCaseHtml = request.HtmlContent;
        assessment.ModifiedDate = DateTimeHelper.Now;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Use case saved" });
    }

    [HttpPost("assessments/{assessmentId:int}/usecase/upload")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadUseCaseHtml(int assessmentId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "HTML file is required" });

        using var reader = new StreamReader(file.OpenReadStream());
        var htmlContent = await reader.ReadToEndAsync();

        var assessment = await _db.Set<Assessment>().FindAsync(assessmentId);
        if (assessment == null) return NotFound(new { message = "Assessment not found" });

        assessment.UseCaseHtml = htmlContent;
        assessment.ModifiedDate = DateTimeHelper.Now;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Use case uploaded", fileName = file.FileName });
    }

    // ═══════════════════════════════════════════════════════════════
    // ADMIN: View & Export Submissions
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("submissions/{assessmentId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetAllSubmissions(int assessmentId)
    {
        var submissions = await _db.ManualTestScenarios
            .Include(s => s.User)
            .Include(s => s.TestCases)
            .Where(s => s.AssessmentId == assessmentId)
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                userId = g.Key,
                userLoginId = g.First().User.UserID,
                fullName = g.First().User.FullName,
                scenarioCount = g.Count(),
                testCaseCount = g.Sum(s => s.TestCases.Count)
            })
            .ToListAsync();

        return Ok(submissions);
    }

    [HttpGet("submissions/{assessmentId:int}/user/{userId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetUserSubmission(int assessmentId, string userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
        if (user == null) return NotFound(new { message = "User not found" });

        var scenarios = await _db.ManualTestScenarios
            .Include(s => s.TestCases)
            .Where(s => s.UserId == user.Id && s.AssessmentId == assessmentId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return Ok(new ManualTestSubmissionDto
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Scenarios = scenarios.Select(s => new ManualTestScenarioDto
            {
                Id = s.Id, SNo = s.SNo, ScenarioId = s.ScenarioId, Scenario = s.Scenario,
                Description = s.Description, MustTest = s.MustTest,
                SortOrder = s.SortOrder, TestCaseCount = s.TestCases.Count
            }).ToList(),
            TestCases = scenarios.SelectMany(s => s.TestCases.Select(c => new ManualTestCaseDto
            {
                Id = c.Id, ScenarioDbId = c.ScenarioDbId, SNo = c.SNo,
                ScenarioId = c.ScenarioId, TestCaseId = c.TestCaseId,
                TestCaseDescription = c.TestCaseDescription, StepNo = c.StepNo,
                InputSpecification = c.InputSpecification,
                InputTestData = c.InputTestData,
                ExpectedResult = c.ExpectedResult, SortOrder = c.SortOrder
            })).ToList()
        });
    }

    [HttpGet("submissions/{assessmentId:int}/export")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ExportAll(int assessmentId)
    {
        var scenarios = await _db.ManualTestScenarios
            .Include(s => s.User)
            .Include(s => s.TestCases)
            .Where(s => s.AssessmentId == assessmentId)
            .OrderBy(s => s.User.UserID).ThenBy(s => s.SortOrder)
            .ToListAsync();

        if (scenarios.Count == 0)
            return BadRequest(new { message = "No submissions to export" });

        var sb = new StringBuilder();
        sb.AppendLine("User ID,Full Name,S.No,Scenario ID,Scenario,Scenario Description,Must Test,Test Case ID,Test Case Description,Step No,Test Step / Input Specification,Input/Test Data,Expected Result");

        foreach (var s in scenarios)
        {
            if (s.TestCases.Count == 0)
            {
                sb.AppendLine($"\"{s.User.UserID}\",\"{s.User.FullName ?? ""}\",{s.SNo},\"{s.ScenarioId}\",\"{s.Scenario ?? ""}\",\"{s.Description ?? ""}\",\"{s.MustTest ?? ""}\",,,,,,");
            }
            else
            {
                foreach (var tc in s.TestCases.OrderBy(c => c.SortOrder))
                {
                    sb.AppendLine($"\"{s.User.UserID}\",\"{s.User.FullName ?? ""}\",{tc.SNo},\"{s.ScenarioId}\",\"{s.Scenario ?? ""}\",\"{s.Description ?? ""}\",\"{s.MustTest ?? ""}\",\"{tc.TestCaseId}\",\"{tc.TestCaseDescription ?? ""}\",\"{tc.StepNo}\",\"{tc.InputSpecification ?? ""}\",\"{tc.InputTestData ?? ""}\",\"{tc.ExpectedResult ?? ""}\"");
                }
            }
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"ManualTest_Export_{DateTimeHelper.Now:yyyyMMdd}.csv");
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public class SaveTestCaseBatchDto
{
    public int? Id { get; set; }
    public int ScenarioDbId { get; set; }
    public int SNo { get; set; }
    public string? ScenarioId { get; set; }
    public string TestCaseId { get; set; } = string.Empty;
    public string? TestCaseDescription { get; set; }
    public string StepNo { get; set; } = string.Empty;
    public string? InputSpecification { get; set; }
    public string? InputTestData { get; set; }
    public string? ExpectedResult { get; set; }
    public int SortOrder { get; set; }
}

public class SaveUseCaseDto
{
    public string HtmlContent { get; set; } = string.Empty;
}
