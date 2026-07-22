namespace DCView.Hackathon.Application.DTOs.Mcq;

// ─── Assessment DTOs ─────────────────────────────────────────────

public class AssessmentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public int MaxMarks { get; set; }
    public int SimplePercentage { get; set; }
    public int MediumPercentage { get; set; }
    public int ComplexPercentage { get; set; }
    public int SimpleMarks { get; set; }
    public int MediumMarks { get; set; }
    public int ComplexMarks { get; set; }
    public bool NegativeMarking { get; set; }
    public decimal NegativeMarkValue { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public bool ShowResultImmediately { get; set; }
    public int PassPercentage { get; set; }
    public bool AllowNavigation { get; set; }
    public bool AllowReview { get; set; }
    public bool AutoSubmitOnTimeout { get; set; }
    public bool OneQuestionPerPage { get; set; }
    public bool ShowComplexity { get; set; }
    public bool ShowMarks { get; set; }
    public bool IsActive { get; set; }
    public int QuestionBankCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateAssessmentDto
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "MCQ";
    public string SubType { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public int TotalQuestions { get; set; } = 30;
    public int MaxMarks { get; set; } = 45;
    public int SimplePercentage { get; set; } = 60;
    public int MediumPercentage { get; set; } = 30;
    public int ComplexPercentage { get; set; } = 10;
    public int SimpleMarks { get; set; } = 1;
    public int MediumMarks { get; set; } = 2;
    public int ComplexMarks { get; set; } = 3;
    public bool NegativeMarking { get; set; } = false;
    public decimal NegativeMarkValue { get; set; } = 0;
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleOptions { get; set; } = true;
    public bool ShowResultImmediately { get; set; } = false;
    public int PassPercentage { get; set; } = 0;
    public bool AllowNavigation { get; set; } = true;
    public bool AllowReview { get; set; } = true;
    public bool AutoSubmitOnTimeout { get; set; } = true;
    public bool OneQuestionPerPage { get; set; } = true;
    public bool ShowComplexity { get; set; } = true;
    public bool ShowMarks { get; set; } = true;
}

public class UpdateAssessmentDto : CreateAssessmentDto
{
    public bool IsActive { get; set; } = true;
}

// ─── Question DTOs ───────────────────────────────────────────────

public class McqQuestionDto
{
    public int Id { get; set; }
    public int SNo { get; set; }
    public string Question { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Complexity { get; set; } = string.Empty;
    public int Marks { get; set; }
    public string? Category { get; set; }
    public string? Explanation { get; set; }
    public bool IsActive { get; set; }
}

public class CreateMcqQuestionDto
{
    public int SNo { get; set; }
    public string Question { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Complexity { get; set; } = "Simple";
    public int? Marks { get; set; }
    public string? Category { get; set; }
    public string? Explanation { get; set; }
}

// ─── Test Result DTOs (Admin view) ──────────────────────────────

public class McqTestResultDto
{
    public int TestId { get; set; }
    public string UserID { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int TotalQuestions { get; set; }
    public int Attempted { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int Skipped { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage { get; set; }
    public bool? Passed { get; set; }
    public bool IsAutoSubmitted { get; set; }
    public int? TimeSpentSeconds { get; set; }
}
