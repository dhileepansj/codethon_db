using System.Text;
using System.Text.Json;
using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Enums;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.Application.Services;

public class SurveyDashboardService : ISurveyDashboardService
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly ISurveyParticipantRepository _participantRepo;
    private readonly ISurveyResponseRepository _responseRepo;
    private readonly ISurveyFieldRepository _fieldRepo;

    public SurveyDashboardService(
        ISurveyRepository surveyRepo,
        ISurveyParticipantRepository participantRepo,
        ISurveyResponseRepository responseRepo,
        ISurveyFieldRepository fieldRepo)
    {
        _surveyRepo = surveyRepo;
        _participantRepo = participantRepo;
        _responseRepo = responseRepo;
        _fieldRepo = fieldRepo;
    }

    public async Task<SurveyDashboardDto?> GetDashboardAsync(Guid surveyId)
    {
        var survey = await _surveyRepo.GetByIdAsync(surveyId);
        if (survey == null) return null;

        var total = await _participantRepo.CountBySurveyAsync(surveyId);
        var responded = await _participantRepo.CountBySurveyAndStatusAsync(surveyId, SurveyParticipantStatus.Responded);
        var pending = await _participantRepo.CountBySurveyAndStatusAsync(surveyId, SurveyParticipantStatus.Sent);
        var declined = await _participantRepo.CountBySurveyAndStatusAsync(surveyId, SurveyParticipantStatus.Declined);
        var reminded = await _participantRepo.CountBySurveyAndStatusAsync(surveyId, SurveyParticipantStatus.Reminded);
        var notSent = await _participantRepo.CountBySurveyAndStatusAsync(surveyId, SurveyParticipantStatus.Pending);

        return new SurveyDashboardDto
        {
            SurveyId = surveyId,
            Title = survey.Title,
            TotalParticipants = total,
            Responded = responded,
            Pending = pending,
            Declined = declined,
            Reminded = reminded,
            NotSent = notSent,
            ResponseRate = total > 0 ? Math.Round((double)responded / total * 100, 1) : 0
        };
    }

    public async Task<IEnumerable<SurveyResponseDto>> GetResponsesAsync(Guid surveyId, int page = 1, int pageSize = 50)
    {
        var responses = await _responseRepo.GetBySurveyIdAsync(surveyId, page, pageSize);

        return responses.Select(r => new SurveyResponseDto
        {
            Id = r.Id,
            SurveyId = r.SurveyId,
            EmployeeId = r.Participant?.EmployeeId,
            EmployeeName = r.Participant?.EmployeeName,
            EmployeeEmail = r.Participant?.EmployeeEmail,
            SubmittedAt = r.SubmittedAt,
            TimeTakenSeconds = r.TimeTakenSeconds
        });
    }

    public async Task<SurveyResponseDto?> GetResponseDetailAsync(Guid responseId)
    {
        var response = await _responseRepo.GetByIdWithAnswersAsync(responseId);
        if (response == null) return null;

        return new SurveyResponseDto
        {
            Id = response.Id,
            SurveyId = response.SurveyId,
            EmployeeId = response.Participant?.EmployeeId,
            EmployeeName = response.Participant?.EmployeeName,
            EmployeeEmail = response.Participant?.EmployeeEmail,
            SubmittedAt = response.SubmittedAt,
            TimeTakenSeconds = response.TimeTakenSeconds,
            Answers = response.Answers.Select(a => new ResponseAnswerDto
            {
                FieldId = a.FieldId,
                FieldLabel = a.Field?.Label,
                FieldType = a.Field?.FieldType.ToString(),
                Value = a.Value,
                FileUrl = a.FileUrl
            }).ToList()
        };
    }

    public async Task<IEnumerable<FieldAnalyticsDto>> GetAnalyticsAsync(Guid surveyId)
    {
        var fields = await _fieldRepo.GetBySurveyIdAsync(surveyId);
        var responses = await _responseRepo.GetBySurveyIdAsync(surveyId, 1, 10000); // All responses
        var responseIds = responses.Select(r => r.Id).ToList();

        var analytics = new List<FieldAnalyticsDto>();

        foreach (var field in fields)
        {
            // Skip layout fields
            if (field.FieldType == SurveyFieldType.Section || field.FieldType == SurveyFieldType.Paragraph)
                continue;

            var fieldAnalytics = new FieldAnalyticsDto
            {
                FieldId = field.Id,
                Label = field.Label,
                FieldType = field.FieldType.ToString()
            };

            // Get answers for this field from all responses
            var responseWithAnswers = new List<string>();
            foreach (var r in responses)
            {
                var fullResponse = await _responseRepo.GetByIdWithAnswersAsync(r.Id);
                var answer = fullResponse?.Answers.FirstOrDefault(a => a.FieldId == field.Id);
                if (answer?.Value != null)
                {
                    responseWithAnswers.Add(answer.Value);
                }
            }

            fieldAnalytics.TotalAnswers = responseWithAnswers.Count;

            switch (field.FieldType)
            {
                case SurveyFieldType.Dropdown:
                case SurveyFieldType.Radio:
                case SurveyFieldType.Checkbox:
                case SurveyFieldType.MultiSelect:
                case SurveyFieldType.YesNo:
                    fieldAnalytics.OptionBreakdown = GetOptionBreakdown(responseWithAnswers, fieldAnalytics.TotalAnswers);
                    break;

                case SurveyFieldType.Rating:
                case SurveyFieldType.Scale:
                case SurveyFieldType.Number:
                    fieldAnalytics.AverageValue = GetAverageValue(responseWithAnswers);
                    break;

                case SurveyFieldType.ShortText:
                case SurveyFieldType.LongText:
                    fieldAnalytics.TextResponses = responseWithAnswers.Take(50).ToList();
                    break;
            }

            analytics.Add(fieldAnalytics);
        }

        return analytics;
    }

    public async Task<byte[]> ExportResponsesAsync(Guid surveyId)
    {
        var fields = (await _fieldRepo.GetBySurveyIdAsync(surveyId))
            .Where(f => f.FieldType != SurveyFieldType.Section && f.FieldType != SurveyFieldType.Paragraph)
            .ToList();

        var responses = await _responseRepo.GetBySurveyIdAsync(surveyId, 1, 100000);
        var sb = new StringBuilder();

        // Header row
        sb.Append("EmployeeId,EmployeeName,EmployeeEmail,SubmittedAt");
        foreach (var field in fields)
        {
            sb.Append($",\"{EscapeCsv(field.Label)}\"");
        }
        sb.AppendLine();

        // Data rows
        foreach (var response in responses)
        {
            var fullResponse = await _responseRepo.GetByIdWithAnswersAsync(response.Id);
            if (fullResponse == null) continue;

            sb.Append($"{EscapeCsv(fullResponse.Participant?.EmployeeId ?? "")},");
            sb.Append($"{EscapeCsv(fullResponse.Participant?.EmployeeName ?? "")},");
            sb.Append($"{EscapeCsv(fullResponse.Participant?.EmployeeEmail ?? "")},");
            sb.Append($"{fullResponse.SubmittedAt:yyyy-MM-dd HH:mm:ss}");

            foreach (var field in fields)
            {
                var answer = fullResponse.Answers.FirstOrDefault(a => a.FieldId == field.Id);
                sb.Append($",\"{EscapeCsv(answer?.Value ?? "")}\"");
            }
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // Helper methods
    private static List<OptionCountDto> GetOptionBreakdown(List<string> answers, int total)
    {
        var counts = new Dictionary<string, int>();

        foreach (var answer in answers)
        {
            // Handle multi-select (JSON array)
            if (answer.StartsWith('['))
            {
                try
                {
                    var options = JsonSerializer.Deserialize<List<string>>(answer);
                    if (options != null)
                    {
                        foreach (var opt in options)
                        {
                            counts[opt] = counts.GetValueOrDefault(opt) + 1;
                        }
                    }
                }
                catch
                {
                    counts[answer] = counts.GetValueOrDefault(answer) + 1;
                }
            }
            else
            {
                counts[answer] = counts.GetValueOrDefault(answer) + 1;
            }
        }

        return counts.Select(kv => new OptionCountDto
        {
            Option = kv.Key,
            Count = kv.Value,
            Percentage = total > 0 ? Math.Round((double)kv.Value / total * 100, 1) : 0
        }).OrderByDescending(o => o.Count).ToList();
    }

    private static double? GetAverageValue(List<string> answers)
    {
        var numbers = answers
            .Where(a => double.TryParse(a, out _))
            .Select(a => double.Parse(a))
            .ToList();

        return numbers.Count > 0 ? Math.Round(numbers.Average(), 2) : null;
    }

    private static string EscapeCsv(string value)
        => value.Replace("\"", "\"\"");
}
