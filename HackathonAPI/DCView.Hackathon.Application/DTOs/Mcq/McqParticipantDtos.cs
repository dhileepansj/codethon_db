namespace DCView.Hackathon.Application.DTOs.Mcq;

// ─── Participant-facing DTOs ─────────────────────────────────────

/// <summary>
/// Info shown to participant before starting the test.
/// </summary>
public class McqTestInfoDto
{
    public int AssessmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int DurationMinutes { get; set; }
    public int MaxMarks { get; set; }
    public bool NegativeMarking { get; set; }
    public decimal NegativeMarkValue { get; set; }
    public bool AllowNavigation { get; set; }
    public bool AllowReview { get; set; }
    public bool OneQuestionPerPage { get; set; }
    public bool ShowComplexity { get; set; }
    public bool ShowMarks { get; set; }
    public int SimpleCount { get; set; }
    public int MediumCount { get; set; }
    public int ComplexCount { get; set; }
    public int SimpleMarks { get; set; }
    public int MediumMarks { get; set; }
    public int ComplexMarks { get; set; }

    /// <summary>Whether the participant already has a test in progress</summary>
    public bool HasExistingTest { get; set; }
    public int? ExistingTestId { get; set; }
    /// <summary>Whether the participant has already submitted their test</summary>
    public bool IsAlreadySubmitted { get; set; }
    public decimal? SubmittedScore { get; set; }
    public decimal? SubmittedMaxScore { get; set; }
    public decimal? SubmittedPercentage { get; set; }
    public bool? SubmittedPassed { get; set; }
}

/// <summary>
/// A single question as presented to the participant (no correct answer exposed).
/// </summary>
public class McqQuestionForTestDto
{
    public int QuestionId { get; set; }
    public int QuestionIndex { get; set; }
    public string Question { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string Complexity { get; set; } = string.Empty;
    public int Marks { get; set; }
    public string? Category { get; set; }

    /// <summary>The participant's current answer (null if not answered)</summary>
    public string? SelectedAnswer { get; set; }
    /// <summary>Whether the participant flagged this for review</summary>
    public bool IsFlagged { get; set; }
}

/// <summary>
/// Response when starting a test.
/// </summary>
public class StartTestResultDto
{
    public int TestId { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int DurationMinutes { get; set; }
}

/// <summary>
/// Request to save an answer.
/// </summary>
public class SaveAnswerDto
{
    public int QuestionId { get; set; }
    /// <summary>"A", "B", "C", "D", or null to clear</summary>
    public string? SelectedAnswer { get; set; }
    /// <summary>Time spent on this question in seconds</summary>
    public int? TimeTakenSeconds { get; set; }
    /// <summary>Whether to flag for review</summary>
    public bool? IsFlagged { get; set; }
}

/// <summary>
/// Test status during an active test.
/// </summary>
public class McqTestStatusDto
{
    public int TestId { get; set; }
    public bool IsInProgress { get; set; }
    public bool IsSubmitted { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? RemainingSeconds { get; set; }
    public int TotalQuestions { get; set; }
    public int Answered { get; set; }
    public int Flagged { get; set; }
    /// <summary>Navigation panel data: question index → answered/flagged status</summary>
    public List<QuestionNavItem> NavigationPanel { get; set; } = new();
}

public class QuestionNavItem
{
    public int QuestionIndex { get; set; }
    public int QuestionId { get; set; }
    public bool IsAnswered { get; set; }
    public bool IsFlagged { get; set; }
    public string Complexity { get; set; } = string.Empty;
}

/// <summary>
/// Final result shown to participant after submission.
/// </summary>
public class McqSubmitResultDto
{
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int Skipped { get; set; }
    public int TotalQuestions { get; set; }
    public bool? Passed { get; set; }
    public int? TimeSpentSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
    /// <summary>Whether the score breakdown should be shown to the participant</summary>
    public bool ShowScores { get; set; } = false;

    /// <summary>Only populated if ShowResultImmediately is true</summary>
    public List<McqAnswerReviewDto>? DetailedResults { get; set; }
}

public class McqAnswerReviewDto
{
    public int QuestionIndex { get; set; }
    public string Question { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? SelectedAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public decimal MarksAwarded { get; set; }
    public string? Explanation { get; set; }
}
