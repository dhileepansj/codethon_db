namespace DCView.Hackathon.Application.DTOs.Survey;

public class SurveyResponseDto
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public string? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeEmail { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int? TimeTakenSeconds { get; set; }
    public List<ResponseAnswerDto> Answers { get; set; } = new();
}

public class ResponseAnswerDto
{
    public Guid FieldId { get; set; }
    public string? FieldLabel { get; set; }
    public string? FieldType { get; set; }
    public string? Value { get; set; }
    public string? FileUrl { get; set; }
}

public class SubmitSurveyResponseDto
{
    public List<SubmitAnswerDto> Answers { get; set; } = new();
    public int? TimeTakenSeconds { get; set; }
}

public class SubmitAnswerDto
{
    public Guid FieldId { get; set; }
    public string? Value { get; set; }
    public string? FileUrl { get; set; }
}
