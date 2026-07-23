namespace DCView.Hackathon.Application.DTOs.Survey;

public class SurveyDashboardDto
{
    public Guid SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalParticipants { get; set; }
    public int Responded { get; set; }
    public int Pending { get; set; }
    public int Declined { get; set; }
    public int Reminded { get; set; }
    public int NotSent { get; set; }
    public double ResponseRate { get; set; }
}

public class FieldAnalyticsDto
{
    public Guid FieldId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public int TotalAnswers { get; set; }

    /// <summary>
    /// For choice fields: breakdown of each option count.
    /// </summary>
    public List<OptionCountDto>? OptionBreakdown { get; set; }

    /// <summary>
    /// For rating/scale fields: average value.
    /// </summary>
    public double? AverageValue { get; set; }

    /// <summary>
    /// For text fields: list of responses (paginated).
    /// </summary>
    public List<string>? TextResponses { get; set; }
}

public class OptionCountDto
{
    public string Option { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
