namespace DCView.Hackathon.Application.DTOs.ManualTest;

// ─── Scenario DTOs ───────────────────────────────────────────────

public class ManualTestScenarioDto
{
    public int Id { get; set; }
    public int SNo { get; set; }
    public string ScenarioId { get; set; } = string.Empty;
    public string? Scenario { get; set; }
    public string? Description { get; set; }
    public string? MustTest { get; set; }
    public int SortOrder { get; set; }
    public int TestCaseCount { get; set; }
}

public class SaveScenarioDto
{
    public int? Id { get; set; }
    public int SNo { get; set; }
    public string ScenarioId { get; set; } = string.Empty;
    public string? Scenario { get; set; }
    public string? Description { get; set; }
    public string? MustTest { get; set; }
    public int SortOrder { get; set; }
}

// ─── Test Case DTOs ──────────────────────────────────────────────

public class ManualTestCaseDto
{
    public int Id { get; set; }
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

public class SaveTestCaseDto
{
    public int? Id { get; set; }
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

// ─── Full workspace data ─────────────────────────────────────────

public class ManualTestWorkspaceDto
{
    public int AssessmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? UseCaseHtml { get; set; }
    public int? DurationMinutes { get; set; }
    public List<ManualTestScenarioDto> Scenarios { get; set; } = new();
}

public class ManualTestSubmissionDto
{
    public List<ManualTestScenarioDto> Scenarios { get; set; } = new();
    public List<ManualTestCaseDto> TestCases { get; set; } = new();
    public string UserID { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
