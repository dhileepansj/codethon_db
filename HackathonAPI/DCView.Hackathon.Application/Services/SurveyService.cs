using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.Application.Services;

public class SurveyService : ISurveyService
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly ISurveyFieldRepository _fieldRepo;
    private readonly ISurveyParticipantRepository _participantRepo;
    private readonly ISurveyResponseRepository _responseRepo;

    public SurveyService(
        ISurveyRepository surveyRepo,
        ISurveyFieldRepository fieldRepo,
        ISurveyParticipantRepository participantRepo,
        ISurveyResponseRepository responseRepo)
    {
        _surveyRepo = surveyRepo;
        _fieldRepo = fieldRepo;
        _participantRepo = participantRepo;
        _responseRepo = responseRepo;
    }

    public async Task<IEnumerable<SurveyDto>> GetAllSurveysAsync()
    {
        var surveys = await _surveyRepo.GetAllAsync();
        var dtos = new List<SurveyDto>();

        foreach (var s in surveys)
        {
            var totalParticipants = await _participantRepo.CountBySurveyAsync(s.Id);
            var totalResponses = await _responseRepo.CountBySurveyAsync(s.Id);

            dtos.Add(MapToDto(s, totalParticipants, totalResponses));
        }

        return dtos;
    }

    public async Task<SurveyDetailDto?> GetSurveyByIdAsync(Guid id)
    {
        var survey = await _surveyRepo.GetByIdWithFieldsAsync(id);
        if (survey == null) return null;

        var totalParticipants = await _participantRepo.CountBySurveyAsync(id);
        var totalResponses = await _responseRepo.CountBySurveyAsync(id);

        return new SurveyDetailDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Status = survey.Status,
            CreatedAt = survey.CreatedAt,
            UpdatedAt = survey.UpdatedAt,
            StartsAt = survey.StartsAt,
            ExpiresAt = survey.ExpiresAt,
            AllowMultiple = survey.AllowMultiple,
            IsAnonymous = survey.IsAnonymous,
            ThankYouMessage = survey.ThankYouMessage,
            TotalParticipants = totalParticipants,
            TotalResponses = totalResponses,
            FieldCount = survey.Fields.Count,
            Fields = survey.Fields.Select(f => new SurveyFieldDto
            {
                Id = f.Id,
                SurveyId = f.SurveyId,
                FieldType = f.FieldType,
                Label = f.Label,
                Description = f.Description,
                Placeholder = f.Placeholder,
                IsRequired = f.IsRequired,
                SortOrder = f.SortOrder,
                Options = f.Options,
                Validation = f.Validation,
                SectionTitle = f.SectionTitle,
                DefaultValue = f.DefaultValue,
                MatrixRows = f.MatrixRows,
                MatrixColumns = f.MatrixColumns,
                Dependencies = f.Dependencies.Select(d => new FieldDependencyDto
                {
                    Id = d.Id,
                    FieldId = d.FieldId,
                    DependsOnFieldId = d.DependsOnFieldId,
                    Condition = d.Condition,
                    Value = d.Value,
                    Action = d.Action,
                    OptionMap = d.OptionMap,
                    LogicGroupId = d.LogicGroupId,
                    LogicOperator = d.LogicOperator
                }).ToList()
            }).ToList(),
            EmailSettings = survey.EmailSettings != null ? new SurveyEmailSettingsDto
            {
                Id = survey.EmailSettings.Id,
                SurveyId = survey.EmailSettings.SurveyId,
                IncludeRmByDefault = survey.EmailSettings.IncludeRmByDefault,
                IncludeVhByDefault = survey.EmailSettings.IncludeVhByDefault,
                AdditionalCcEmails = survey.EmailSettings.AdditionalCcEmails,
                EmailSubject = survey.EmailSettings.EmailSubject,
                EmailBody = survey.EmailSettings.EmailBody,
                ReminderEnabled = survey.EmailSettings.ReminderEnabled,
                ReminderDays = survey.EmailSettings.ReminderDays,
                MaxReminders = survey.EmailSettings.MaxReminders
            } : null
        };
    }

    public async Task<SurveyDto> CreateSurveyAsync(CreateSurveyDto dto, int userId)
    {
        var survey = new Survey
        {
            Title = dto.Title,
            Description = dto.Description,
            StartsAt = dto.StartsAt,
            ExpiresAt = dto.ExpiresAt,
            AllowMultiple = dto.AllowMultiple,
            IsAnonymous = dto.IsAnonymous,
            ThankYouMessage = dto.ThankYouMessage,
            CreatedByUserId = userId,
            Status = SurveyStatus.Draft
        };

        var created = await _surveyRepo.CreateAsync(survey);
        return MapToDto(created, 0, 0);
    }

    public async Task<SurveyDto?> UpdateSurveyAsync(Guid id, UpdateSurveyDto dto)
    {
        var survey = await _surveyRepo.GetByIdAsync(id);
        if (survey == null) return null;

        if (dto.Title != null) survey.Title = dto.Title;
        if (dto.Description != null) survey.Description = dto.Description;
        if (dto.StartsAt.HasValue) survey.StartsAt = dto.StartsAt;
        if (dto.ExpiresAt.HasValue) survey.ExpiresAt = dto.ExpiresAt;
        if (dto.AllowMultiple.HasValue) survey.AllowMultiple = dto.AllowMultiple.Value;
        if (dto.IsAnonymous.HasValue) survey.IsAnonymous = dto.IsAnonymous.Value;
        if (dto.ThankYouMessage != null) survey.ThankYouMessage = dto.ThankYouMessage;

        await _surveyRepo.UpdateAsync(survey);

        var totalParticipants = await _participantRepo.CountBySurveyAsync(id);
        var totalResponses = await _responseRepo.CountBySurveyAsync(id);
        return MapToDto(survey, totalParticipants, totalResponses);
    }

    public async Task<bool> DeleteSurveyAsync(Guid id)
    {
        var survey = await _surveyRepo.GetByIdAsync(id);
        if (survey == null) return false;

        survey.IsDeleted = true;
        await _surveyRepo.UpdateAsync(survey);
        return true;
    }

    public async Task<SurveyDto?> UpdateStatusAsync(Guid id, UpdateSurveyStatusDto dto)
    {
        var survey = await _surveyRepo.GetByIdAsync(id);
        if (survey == null) return null;

        survey.Status = dto.Status;
        await _surveyRepo.UpdateAsync(survey);

        var totalParticipants = await _participantRepo.CountBySurveyAsync(id);
        var totalResponses = await _responseRepo.CountBySurveyAsync(id);
        return MapToDto(survey, totalParticipants, totalResponses);
    }

    public async Task<SurveyDto?> CloneSurveyAsync(Guid id, int userId)
    {
        var source = await _surveyRepo.GetByIdWithFieldsAsync(id);
        if (source == null) return null;

        var clone = new Survey
        {
            Title = $"{source.Title} (Copy)",
            Description = source.Description,
            AllowMultiple = source.AllowMultiple,
            IsAnonymous = source.IsAnonymous,
            ThankYouMessage = source.ThankYouMessage,
            CreatedByUserId = userId,
            Status = SurveyStatus.Draft
        };

        var created = await _surveyRepo.CreateAsync(clone);

        // Clone fields
        foreach (var field in source.Fields.OrderBy(f => f.SortOrder))
        {
            var clonedField = new SurveyField
            {
                SurveyId = created.Id,
                FieldType = field.FieldType,
                Label = field.Label,
                Description = field.Description,
                Placeholder = field.Placeholder,
                IsRequired = field.IsRequired,
                SortOrder = field.SortOrder,
                Options = field.Options,
                Validation = field.Validation,
                SectionTitle = field.SectionTitle,
                DefaultValue = field.DefaultValue,
                MatrixRows = field.MatrixRows,
                MatrixColumns = field.MatrixColumns
            };
            await _fieldRepo.CreateAsync(clonedField);
        }

        // Clone email settings
        if (source.EmailSettings != null)
        {
            var clonedSettings = new SurveyEmailSettings
            {
                SurveyId = created.Id,
                IncludeRmByDefault = source.EmailSettings.IncludeRmByDefault,
                IncludeVhByDefault = source.EmailSettings.IncludeVhByDefault,
                AdditionalCcEmails = source.EmailSettings.AdditionalCcEmails,
                EmailSubject = source.EmailSettings.EmailSubject,
                EmailBody = source.EmailSettings.EmailBody,
                ReminderEnabled = source.EmailSettings.ReminderEnabled,
                ReminderDays = source.EmailSettings.ReminderDays,
                MaxReminders = source.EmailSettings.MaxReminders
            };
            // Settings will be saved via the distribution service
        }

        return MapToDto(created, 0, 0);
    }

    private static SurveyDto MapToDto(Survey s, int totalParticipants, int totalResponses) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Description = s.Description,
        Status = s.Status,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        StartsAt = s.StartsAt,
        ExpiresAt = s.ExpiresAt,
        AllowMultiple = s.AllowMultiple,
        IsAnonymous = s.IsAnonymous,
        ThankYouMessage = s.ThankYouMessage,
        TotalParticipants = totalParticipants,
        TotalResponses = totalResponses,
        FieldCount = s.Fields?.Count ?? 0
    };
}
