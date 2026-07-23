using System.Security.Claims;
using System.Text;
using ClosedXML.Excel;
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

    [HttpGet("submissions/{assessmentId:int}/export-excel")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ExportAllExcel(int assessmentId)
    {
        var assessment = await _db.Set<Assessment>().FindAsync(assessmentId);
        if (assessment == null) return NotFound(new { message = "Assessment not found" });

        var scenarios = await _db.ManualTestScenarios
            .Include(s => s.User)
            .Include(s => s.TestCases)
            .Where(s => s.AssessmentId == assessmentId)
            .OrderBy(s => s.User.UserID).ThenBy(s => s.SortOrder)
            .ToListAsync();

        if (scenarios.Count == 0)
            return BadRequest(new { message = "No submissions to export" });

        using var workbook = new XLWorkbook();

        // ─── Sheet 1: Summary ───────────────────────────────────────
        var summarySheet = workbook.Worksheets.Add("Summary");

        // Title row
        summarySheet.Cell(1, 1).Value = assessment.Title ?? "Manual Testing Export";
        summarySheet.Range(1, 1, 1, 5).Merge();
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 16;
        summarySheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        summarySheet.Cell(2, 1).Value = $"Exported: {DateTimeHelper.Now:dd-MMM-yyyy HH:mm}";
        summarySheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
        summarySheet.Cell(2, 1).Style.Font.FontSize = 10;

        // Summary table headers (row 4)
        var summaryHeaders = new[] { "S.No", "User ID", "Full Name", "Scenarios", "Test Cases", "Status" };
        for (int i = 0; i < summaryHeaders.Length; i++)
        {
            var cell = summarySheet.Cell(4, i + 1);
            cell.Value = summaryHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        var groupedByUser = scenarios.GroupBy(s => s.UserId).ToList();
        int summaryRow = 5;
        int sNo = 1;
        foreach (var group in groupedByUser)
        {
            var firstScenario = group.First();
            var session = await _db.Sessions.FirstOrDefaultAsync(sess => sess.UserId == group.Key);

            summarySheet.Cell(summaryRow, 1).Value = sNo++;
            summarySheet.Cell(summaryRow, 2).Value = firstScenario.User.UserID;
            summarySheet.Cell(summaryRow, 3).Value = firstScenario.User.FullName ?? "";
            summarySheet.Cell(summaryRow, 4).Value = group.Count();
            summarySheet.Cell(summaryRow, 5).Value = group.Sum(s => s.TestCases.Count);
            summarySheet.Cell(summaryRow, 6).Value = session?.IsSubmitted == true ? "Submitted" : "In Progress";

            // Alternate row color
            if (summaryRow % 2 == 1)
            {
                summarySheet.Range(summaryRow, 1, summaryRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F7FC");
            }

            // Borders
            for (int col = 1; col <= 6; col++)
                summarySheet.Cell(summaryRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            summaryRow++;
        }

        summarySheet.Columns().AdjustToContents();
        summarySheet.Column(1).Width = 6;
        summarySheet.Column(4).Width = 12;
        summarySheet.Column(5).Width = 12;

        // ─── Sheet 2: All Scenarios & Test Cases (detailed) ────────
        var detailSheet = workbook.Worksheets.Add("Detailed Export");

        // Title
        detailSheet.Cell(1, 1).Value = $"{assessment.Title ?? "Manual Testing"} — Detailed Scenarios & Test Cases";
        detailSheet.Range(1, 1, 1, 13).Merge();
        detailSheet.Cell(1, 1).Style.Font.Bold = true;
        detailSheet.Cell(1, 1).Style.Font.FontSize = 14;

        // Headers (row 3)
        var detailHeaders = new[] {
            "User ID", "Full Name", "S.No", "Scenario ID", "Scenario",
            "Scenario Description", "Must Test", "Test Case ID",
            "Test Case Description", "Step No", "Test Step / Input Specification",
            "Input/Test Data", "Expected Result"
        };
        for (int i = 0; i < detailHeaders.Length; i++)
        {
            var cell = detailSheet.Cell(3, i + 1);
            cell.Value = detailHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Freeze header row
        detailSheet.SheetView.FreezeRows(3);

        int dataRow = 4;
        string? prevUserId = null;

        foreach (var scenario in scenarios)
        {
            var isNewUser = scenario.User.UserID != prevUserId;
            prevUserId = scenario.User.UserID;

            if (scenario.TestCases.Count == 0)
            {
                WriteDetailRow(detailSheet, dataRow, scenario, null, isNewUser);
                dataRow++;
            }
            else
            {
                bool firstCase = true;
                foreach (var tc in scenario.TestCases.OrderBy(c => c.SortOrder))
                {
                    WriteDetailRow(detailSheet, dataRow, scenario, tc, isNewUser && firstCase);
                    firstCase = false;
                    isNewUser = false;
                    dataRow++;
                }
            }
        }

        // Auto-fit with max widths
        detailSheet.Columns().AdjustToContents();
        // Cap wide columns
        if (detailSheet.Column(5).Width > 35) detailSheet.Column(5).Width = 35;
        if (detailSheet.Column(6).Width > 40) detailSheet.Column(6).Width = 40;
        if (detailSheet.Column(9).Width > 35) detailSheet.Column(9).Width = 35;
        if (detailSheet.Column(11).Width > 35) detailSheet.Column(11).Width = 35;
        if (detailSheet.Column(12).Width > 30) detailSheet.Column(12).Width = 30;
        if (detailSheet.Column(13).Width > 30) detailSheet.Column(13).Width = 30;

        // Enable auto-filter
        detailSheet.Range(3, 1, dataRow - 1, 13).SetAutoFilter();

        // ─── Per-user sheets (one per participant) ────────────────
        foreach (var group in groupedByUser)
        {
            var user = group.First().User;
            var sheetName = SanitizeSheetName(user.UserID);
            var userSheet = workbook.Worksheets.Add(sheetName);

            // User header
            userSheet.Cell(1, 1).Value = $"{user.FullName ?? user.UserID} — Test Scenarios & Cases";
            userSheet.Range(1, 1, 1, 7).Merge();
            userSheet.Cell(1, 1).Style.Font.Bold = true;
            userSheet.Cell(1, 1).Style.Font.FontSize = 13;

            // Scenarios table
            var scenarioHeaders = new[] { "S.No", "Scenario ID", "Scenario", "Description", "Must Test", "Pass/Fail", "Test Cases" };
            int row = 3;
            for (int i = 0; i < scenarioHeaders.Length; i++)
            {
                var cell = userSheet.Cell(row, i + 1);
                cell.Value = scenarioHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E75B6");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            row = 4;
            foreach (var s in group.OrderBy(x => x.SortOrder))
            {
                userSheet.Cell(row, 1).Value = s.SNo;
                userSheet.Cell(row, 2).Value = s.ScenarioId;
                userSheet.Cell(row, 3).Value = s.Scenario ?? "";
                userSheet.Cell(row, 4).Value = s.Description ?? "";
                userSheet.Cell(row, 5).Value = s.MustTest ?? "";
                userSheet.Cell(row, 6).Value = s.PassFail ?? "";
                userSheet.Cell(row, 7).Value = s.TestCases.Count;

                // Must Test highlighting
                if (string.Equals(s.MustTest, "Yes", StringComparison.OrdinalIgnoreCase))
                    userSheet.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml("#C00000");

                // Row borders
                for (int col = 1; col <= 7; col++)
                    userSheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                if (row % 2 == 0)
                    userSheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F7FC");

                row++;
            }

            // Test Cases table (below scenarios, with a gap)
            row += 2;
            userSheet.Cell(row, 1).Value = "Test Cases";
            userSheet.Cell(row, 1).Style.Font.Bold = true;
            userSheet.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            var tcHeaders = new[] { "S.No", "Scenario ID", "Test Case ID", "Description", "Step No", "Input Specification", "Input/Test Data", "Expected Result" };
            for (int i = 0; i < tcHeaders.Length; i++)
            {
                var cell = userSheet.Cell(row, i + 1);
                cell.Value = tcHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#548235");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            row++;

            foreach (var s in group.OrderBy(x => x.SortOrder))
            {
                foreach (var tc in s.TestCases.OrderBy(c => c.SortOrder))
                {
                    userSheet.Cell(row, 1).Value = tc.SNo;
                    userSheet.Cell(row, 2).Value = tc.ScenarioId ?? s.ScenarioId;
                    userSheet.Cell(row, 3).Value = tc.TestCaseId;
                    userSheet.Cell(row, 4).Value = tc.TestCaseDescription ?? "";
                    userSheet.Cell(row, 5).Value = tc.StepNo;
                    userSheet.Cell(row, 6).Value = tc.InputSpecification ?? "";
                    userSheet.Cell(row, 7).Value = tc.InputTestData ?? "";
                    userSheet.Cell(row, 8).Value = tc.ExpectedResult ?? "";

                    for (int col = 1; col <= 8; col++)
                        userSheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    if (row % 2 == 0)
                        userSheet.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F7FC");

                    row++;
                }
            }

            userSheet.Columns().AdjustToContents();
            if (userSheet.Column(3).Width > 30) userSheet.Column(3).Width = 30;
            if (userSheet.Column(4).Width > 35) userSheet.Column(4).Width = 35;
            if (userSheet.Column(6).Width > 35) userSheet.Column(6).Width = 35;
            if (userSheet.Column(7).Width > 30) userSheet.Column(7).Width = 30;
            if (userSheet.Column(8).Width > 30) userSheet.Column(8).Width = 30;
        }

        // Write to stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"ManualTest_Export_{DateTimeHelper.Now:yyyyMMdd_HHmm}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static void WriteDetailRow(IXLWorksheet sheet, int row, ManualTestScenario scenario, ManualTestCase? tc, bool showUser)
    {
        sheet.Cell(row, 1).Value = showUser ? scenario.User.UserID : "";
        sheet.Cell(row, 2).Value = showUser ? (scenario.User.FullName ?? "") : "";
        sheet.Cell(row, 3).Value = tc?.SNo ?? scenario.SNo;
        sheet.Cell(row, 4).Value = scenario.ScenarioId;
        sheet.Cell(row, 5).Value = scenario.Scenario ?? "";
        sheet.Cell(row, 6).Value = scenario.Description ?? "";
        sheet.Cell(row, 7).Value = scenario.MustTest ?? "";
        sheet.Cell(row, 8).Value = tc?.TestCaseId ?? "";
        sheet.Cell(row, 9).Value = tc?.TestCaseDescription ?? "";
        sheet.Cell(row, 10).Value = tc?.StepNo ?? "";
        sheet.Cell(row, 11).Value = tc?.InputSpecification ?? "";
        sheet.Cell(row, 12).Value = tc?.InputTestData ?? "";
        sheet.Cell(row, 13).Value = tc?.ExpectedResult ?? "";

        // Borders
        for (int col = 1; col <= 13; col++)
            sheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Alternate row shading
        if (row % 2 == 0)
            sheet.Range(row, 1, row, 13).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");

        // Wrap text for description columns
        sheet.Cell(row, 5).Style.Alignment.WrapText = true;
        sheet.Cell(row, 6).Style.Alignment.WrapText = true;
        sheet.Cell(row, 9).Style.Alignment.WrapText = true;
        sheet.Cell(row, 11).Style.Alignment.WrapText = true;
        sheet.Cell(row, 12).Style.Alignment.WrapText = true;
        sheet.Cell(row, 13).Style.Alignment.WrapText = true;
    }

    private static string SanitizeSheetName(string name)
    {
        // Excel sheet names: max 31 chars, no special chars []:*?/\
        var sanitized = new string(name.Where(c => c != '[' && c != ']' && c != ':' && c != '*' && c != '?' && c != '/' && c != '\\').ToArray());
        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
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
