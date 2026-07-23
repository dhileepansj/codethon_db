using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Application.DTOs.Survey;

public class SurveyParticipantDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string? RmName { get; set; }
    public string? RmEmail { get; set; }
    public string? VhName { get; set; }
    public string? VhEmail { get; set; }
    public SurveyParticipantStatus Status { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? LastSentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public int ReminderCount { get; set; }
    public DeclineInfoDto? DeclineInfo { get; set; }
}

public class DeclineInfoDto
{
    public DeclinedByType? DeclinedBy { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentPath { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public string? MarkedByUserName { get; set; }
}

public class DeclineParticipantDto
{
    public DeclinedByType DeclinedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class BulkUploadResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<BulkUploadErrorDto> Errors { get; set; } = new();
}

public class BulkUploadErrorDto
{
    public int Row { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
