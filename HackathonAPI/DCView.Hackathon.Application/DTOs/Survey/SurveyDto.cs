using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Application.DTOs.Survey;

public class SurveyDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SurveyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool AllowMultiple { get; set; }
    public bool IsAnonymous { get; set; }
    public string? ThankYouMessage { get; set; }
    public int TotalParticipants { get; set; }
    public int TotalResponses { get; set; }
    public int FieldCount { get; set; }
}

public class SurveyDetailDto : SurveyDto
{
    public List<SurveyFieldDto> Fields { get; set; } = new();
    public SurveyEmailSettingsDto? EmailSettings { get; set; }
}

public class CreateSurveyDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool AllowMultiple { get; set; } = false;
    public bool IsAnonymous { get; set; } = false;
    public string? ThankYouMessage { get; set; }
}

public class UpdateSurveyDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool? AllowMultiple { get; set; }
    public bool? IsAnonymous { get; set; }
    public string? ThankYouMessage { get; set; }
}

public class UpdateSurveyStatusDto
{
    public SurveyStatus Status { get; set; }
}
