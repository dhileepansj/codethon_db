using System.Text.RegularExpressions;
using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;
using DCView.Hackathon.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DCView.Hackathon.Application.Services;

public class SurveyDistributionService : ISurveyDistributionService
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly ISurveyParticipantRepository _participantRepo;
    private readonly ISurveyDistributionRepository _distributionRepo;
    private readonly ISurveyEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<SurveyDistributionService> _logger;

    public SurveyDistributionService(
        ISurveyRepository surveyRepo,
        ISurveyParticipantRepository participantRepo,
        ISurveyDistributionRepository distributionRepo,
        ISurveyEmailService emailService,
        IConfiguration config,
        ILogger<SurveyDistributionService> logger)
    {
        _surveyRepo = surveyRepo;
        _participantRepo = participantRepo;
        _distributionRepo = distributionRepo;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    public async Task<IEnumerable<SurveyParticipantDto>> GetParticipantsAsync(Guid surveyId)
    {
        var participants = await _participantRepo.GetBySurveyIdAsync(surveyId);
        var result = new List<SurveyParticipantDto>();

        foreach (var p in participants)
        {
            var dto = MapParticipantToDto(p);
            result.Add(dto);
        }

        return result;
    }

    public async Task<IEnumerable<SurveyParticipantDto>> GetPendingParticipantsAsync(Guid surveyId)
    {
        var participants = await _participantRepo.GetBySurveyAndStatusAsync(
            surveyId, SurveyParticipantStatus.Sent, SurveyParticipantStatus.Reminded);

        return participants.Select(MapParticipantToDto);
    }

    public async Task<BulkUploadResultDto> BulkUploadAsync(Guid surveyId, Stream fileStream, string fileName)
    {
        var result = new BulkUploadResultDto();
        var participants = new List<SurveyParticipant>();
        var batchId = Guid.NewGuid();
        var rowNumber = 1;

        using var reader = new StreamReader(fileStream);
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            result.Errors.Add(new BulkUploadErrorDto { Row = 0, Field = "File", Message = "File is empty" });
            return result;
        }

        var headers = headerLine.Split(',').Select(h => h.Trim().ToLower()).ToList();

        // Validate required headers
        var requiredHeaders = new[] { "employeeid", "employeename", "employeeemail" };
        foreach (var rh in requiredHeaders)
        {
            if (!headers.Contains(rh))
            {
                result.Errors.Add(new BulkUploadErrorDto { Row = 0, Field = rh, Message = $"Required column '{rh}' not found" });
            }
        }

        if (result.Errors.Count > 0) return result;

        var empIdIdx = headers.IndexOf("employeeid");
        var empNameIdx = headers.IndexOf("employeename");
        var empEmailIdx = headers.IndexOf("employeeemail");
        var rmNameIdx = headers.IndexOf("rmname");
        var rmEmailIdx = headers.IndexOf("rmemail");
        var vhNameIdx = headers.IndexOf("vhname");
        var vhEmailIdx = headers.IndexOf("vhemail");

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = ParseCsvLine(line);
            result.TotalRows++;

            var empId = GetValue(cols, empIdIdx);
            var empName = GetValue(cols, empNameIdx);
            var empEmail = GetValue(cols, empEmailIdx);
            var rmName = GetValue(cols, rmNameIdx);
            var rmEmail = GetValue(cols, rmEmailIdx);
            var vhName = GetValue(cols, vhNameIdx);
            var vhEmail = GetValue(cols, vhEmailIdx);

            // Validate row
            var hasError = false;
            if (string.IsNullOrWhiteSpace(empId))
            {
                result.Errors.Add(new BulkUploadErrorDto { Row = rowNumber, Field = "EmployeeId", Message = "Employee ID is required" });
                hasError = true;
            }
            if (string.IsNullOrWhiteSpace(empName))
            {
                result.Errors.Add(new BulkUploadErrorDto { Row = rowNumber, Field = "EmployeeName", Message = "Employee Name is required" });
                hasError = true;
            }
            if (string.IsNullOrWhiteSpace(empEmail) || !IsValidEmail(empEmail))
            {
                result.Errors.Add(new BulkUploadErrorDto { Row = rowNumber, Field = "EmployeeEmail", Message = "Valid email is required" });
                hasError = true;
            }
            if (!string.IsNullOrWhiteSpace(rmEmail) && !IsValidEmail(rmEmail))
            {
                result.Errors.Add(new BulkUploadErrorDto { Row = rowNumber, Field = "RmEmail", Message = "Invalid RM email format" });
                hasError = true;
            }
            if (!string.IsNullOrWhiteSpace(vhEmail) && !IsValidEmail(vhEmail))
            {
                result.Errors.Add(new BulkUploadErrorDto { Row = rowNumber, Field = "VhEmail", Message = "Invalid VH email format" });
                hasError = true;
            }

            if (hasError)
            {
                result.ErrorCount++;
                continue;
            }

            // Check duplicate within this upload
            if (participants.Any(p => p.EmployeeEmail.Equals(empEmail, StringComparison.OrdinalIgnoreCase)))
            {
                result.Errors.Add(new BulkUploadErrorDto { Row = rowNumber, Field = "EmployeeEmail", Message = "Duplicate email in upload" });
                result.ErrorCount++;
                continue;
            }

            // Check duplicate in existing participants
            var existing = await _participantRepo.GetByEmailAndSurveyAsync(empEmail!, surveyId);
            if (existing != null)
            {
                result.Errors.Add(new BulkUploadErrorDto { Row = rowNumber, Field = "EmployeeEmail", Message = "Participant already exists in this survey" });
                result.ErrorCount++;
                continue;
            }

            participants.Add(new SurveyParticipant
            {
                SurveyId = surveyId,
                EmployeeId = empId!,
                EmployeeName = empName!,
                EmployeeEmail = empEmail!,
                RmName = rmName,
                RmEmail = rmEmail,
                VhName = vhName,
                VhEmail = vhEmail,
                BatchId = batchId,
                Status = SurveyParticipantStatus.Pending
            });

            result.SuccessCount++;
        }

        if (participants.Count > 0)
        {
            await _participantRepo.CreateBulkAsync(participants);
        }

        return result;
    }

    public async Task<bool> DeleteParticipantAsync(Guid participantId)
    {
        var participant = await _participantRepo.GetByIdAsync(participantId);
        if (participant == null) return false;

        await _participantRepo.DeleteAsync(participant);
        return true;
    }

    public async Task<bool> DeclineParticipantAsync(Guid participantId, DeclineParticipantDto dto, IFormFile? attachment, int adminUserId)
    {
        var participant = await _participantRepo.GetByIdAsync(participantId);
        if (participant == null) return false;

        participant.Status = SurveyParticipantStatus.Declined;
        await _participantRepo.UpdateAsync(participant);

        string? attachmentPath = null;
        if (attachment != null && attachment.Length > 0)
        {
            var uploadsDir = Path.Combine("uploads", "survey-declines");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(attachment.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await attachment.CopyToAsync(stream);
            attachmentPath = filePath;
        }

        var statusLog = new SurveyParticipantStatusLog
        {
            SurveyId = participant.SurveyId,
            ParticipantId = participantId,
            DeclinedBy = dto.DeclinedBy,
            DeclineReason = dto.Reason,
            DeclineAttachmentPath = attachmentPath,
            DeclinedAt = DateTime.UtcNow,
            MarkedByUserId = adminUserId
        };

        await _participantRepo.CreateStatusLogAsync(statusLog);
        return true;
    }

    public async Task<bool> ResetParticipantStatusAsync(Guid participantId)
    {
        var participant = await _participantRepo.GetByIdAsync(participantId);
        if (participant == null) return false;

        participant.Status = participant.Distribution?.SentAt != null
            ? SurveyParticipantStatus.Sent
            : SurveyParticipantStatus.Pending;

        await _participantRepo.UpdateAsync(participant);
        return true;
    }

    public async Task<SurveyEmailSettingsDto?> GetEmailSettingsAsync(Guid surveyId)
    {
        var settings = await _distributionRepo.GetEmailSettingsAsync(surveyId);
        if (settings == null) return null;

        return new SurveyEmailSettingsDto
        {
            Id = settings.Id,
            SurveyId = settings.SurveyId,
            IncludeRmByDefault = settings.IncludeRmByDefault,
            IncludeVhByDefault = settings.IncludeVhByDefault,
            AdditionalCcEmails = settings.AdditionalCcEmails,
            EmailSubject = settings.EmailSubject,
            EmailBody = settings.EmailBody,
            ReminderEnabled = settings.ReminderEnabled,
            ReminderDays = settings.ReminderDays,
            MaxReminders = settings.MaxReminders
        };
    }

    public async Task<SurveyEmailSettingsDto> UpdateEmailSettingsAsync(Guid surveyId, UpdateEmailSettingsDto dto)
    {
        var existing = await _distributionRepo.GetEmailSettingsAsync(surveyId);

        if (existing == null)
        {
            var settings = new SurveyEmailSettings
            {
                SurveyId = surveyId,
                IncludeRmByDefault = dto.IncludeRmByDefault,
                IncludeVhByDefault = dto.IncludeVhByDefault,
                EmailMode = (SurveyEmailMode)dto.EmailMode,
                AdditionalCcEmails = dto.AdditionalCcEmails,
                EmailSubject = dto.EmailSubject,
                EmailBody = dto.EmailBody,
                ReminderEnabled = dto.ReminderEnabled,
                ReminderDays = dto.ReminderDays,
                MaxReminders = dto.MaxReminders
            };

            var created = await _distributionRepo.CreateEmailSettingsAsync(settings);
            return MapEmailSettingsToDto(created);
        }

        existing.IncludeRmByDefault = dto.IncludeRmByDefault;
        existing.IncludeVhByDefault = dto.IncludeVhByDefault;
        existing.EmailMode = (SurveyEmailMode)dto.EmailMode;
        existing.AdditionalCcEmails = dto.AdditionalCcEmails;
        existing.EmailSubject = dto.EmailSubject;
        existing.EmailBody = dto.EmailBody;
        existing.ReminderEnabled = dto.ReminderEnabled;
        existing.ReminderDays = dto.ReminderDays;
        existing.MaxReminders = dto.MaxReminders;

        await _distributionRepo.UpdateEmailSettingsAsync(existing);
        return MapEmailSettingsToDto(existing);
    }

    public async Task<int> DistributeAsync(Guid surveyId)
    {
        var survey = await _surveyRepo.GetByIdAsync(surveyId);
        if (survey == null) return 0;

        // Block sending if survey is not Active
        if (survey.Status != SurveyStatus.Active)
            throw new InvalidOperationException($"Cannot send invitations. Survey must be Active (current status: {survey.Status}).");

        var emailSettings = await _distributionRepo.GetEmailSettingsAsync(surveyId);
        var participants = await _participantRepo.GetBySurveyAndStatusAsync(surveyId, SurveyParticipantStatus.Pending);
        var participantList = participants.ToList();

        if (participantList.Count == 0) return 0;

        var baseUrl = _config["SurveyEmail:FrontendBaseUrl"]
            ?? _config["FrontendBaseUrl"]
            ?? "http://localhost:5173/novaccodelab";

        var emailMode = emailSettings?.EmailMode ?? SurveyEmailMode.SingleBulk;

        // Step 1: Create all distribution records
        var distributions = new List<(SurveyDistribution dist, SurveyParticipant participant)>();

        foreach (var participant in participantList)
        {
            var existing = await _distributionRepo.GetByParticipantAndSurveyAsync(participant.Id, surveyId);
            if (existing != null) continue;

            var token = Guid.NewGuid().ToString("N");
            var distribution = new SurveyDistribution
            {
                SurveyId = surveyId,
                ParticipantId = participant.Id,
                Token = token,
                IncludeRm = emailSettings?.IncludeRmByDefault ?? false,
                IncludeVh = emailSettings?.IncludeVhByDefault ?? false,
                CcEmails = emailSettings?.AdditionalCcEmails,
                SentAt = DateTime.UtcNow,
                EmailStatus = SurveyEmailStatus.Pending
            };

            await _distributionRepo.CreateAsync(distribution);
            distributions.Add((distribution, participant));
        }

        if (distributions.Count == 0) return 0;

        int sentCount;

        switch (emailMode)
        {
            case SurveyEmailMode.SingleBulk:
                sentCount = await DistributeSingleBulkAsync(survey, emailSettings, distributions, baseUrl);
                break;
            case SurveyEmailMode.IndividualWithSummary:
                sentCount = await DistributeIndividualAsync(survey, emailSettings, distributions, baseUrl, includeCcOnEach: false);
                await SendManagerSummariesAsync(survey, distributions);
                break;
            case SurveyEmailMode.IndividualWithCc:
            default:
                sentCount = await DistributeIndividualAsync(survey, emailSettings, distributions, baseUrl, includeCcOnEach: true);
                break;
        }

        _logger.LogInformation("Distribution complete ({Mode}): {Sent}/{Total} for survey {SurveyId}",
            emailMode, sentCount, distributions.Count, surveyId);

        return sentCount;
    }

    /// <summary>
    /// Mode A: Single email with all participants in TO, all RMs/VHs in CC.
    /// </summary>
    private async Task<int> DistributeSingleBulkAsync(
        Survey survey, SurveyEmailSettings? emailSettings,
        List<(SurveyDistribution dist, SurveyParticipant participant)> distributions, string baseUrl)
    {
        var deadline = survey.ExpiresAt?.ToString("dd MMM yyyy") ?? "No deadline";

        // Build a generic email body (no personalized link since it's bulk)
        var variables = new Dictionary<string, string>
        {
            ["EmployeeName"] = "Team",
            ["SurveyTitle"] = survey.Title,
            ["SurveyLink"] = $"{baseUrl}/survey/{{your-unique-link}}",
            ["Deadline"] = deadline,
            ["RmName"] = "",
            ["VhName"] = "",
        };

        var subject = SurveyEmailTemplateBuilder.RenderSubject(emailSettings?.EmailSubject, variables);

        // Build HTML body with a note that individual links are sent separately
        var bodyHtml = SurveyEmailTemplateBuilder.RenderInvitationBody(emailSettings?.EmailBody, variables)
            .Replace("{{your-unique-link}}", "[individual links sent to each participant]");

        // Collect all TO recipients
        var toRecipients = distributions
            .Select(d => (d.participant.EmployeeEmail, d.participant.EmployeeName))
            .ToList();

        // Collect all unique CC emails (RMs + VHs + additional)
        var ccEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (emailSettings?.IncludeRmByDefault == true)
        {
            foreach (var (_, p) in distributions)
                if (!string.IsNullOrWhiteSpace(p.RmEmail)) ccEmails.Add(p.RmEmail);
        }

        if (emailSettings?.IncludeVhByDefault == true)
        {
            foreach (var (_, p) in distributions)
                if (!string.IsNullOrWhiteSpace(p.VhEmail)) ccEmails.Add(p.VhEmail);
        }

        if (!string.IsNullOrWhiteSpace(emailSettings?.AdditionalCcEmails))
        {
            foreach (var cc in emailSettings.AdditionalCcEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                ccEmails.Add(cc);
        }

        var bulkMessage = new BulkSurveyEmailMessage
        {
            ToRecipients = toRecipients,
            CcEmails = ccEmails.ToList(),
            Subject = subject,
            HtmlBody = bodyHtml,
        };

        var sent = await _emailService.SendBulkInvitationAsync(bulkMessage);

        // Update all distribution records
        foreach (var (dist, participant) in distributions)
        {
            dist.EmailStatus = sent ? SurveyEmailStatus.Sent : SurveyEmailStatus.Failed;
            await _distributionRepo.UpdateAsync(dist);

            if (sent)
            {
                participant.Status = SurveyParticipantStatus.Sent;
                await _participantRepo.UpdateAsync(participant);
            }
        }

        // Also send individual personalized links to each participant (so they have their unique URL)
        if (sent)
        {
            const int batchSize = 10;
            for (var i = 0; i < distributions.Count; i += batchSize)
            {
                var batch = distributions.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(async item =>
                {
                    var (dist, participant) = item;
                    var surveyLink = $"{baseUrl}/survey/{dist.Token}";
                    var personalVars = new Dictionary<string, string>
                    {
                        ["EmployeeName"] = participant.EmployeeName,
                        ["SurveyTitle"] = survey.Title,
                        ["SurveyLink"] = surveyLink,
                        ["Deadline"] = deadline,
                    };
                    var personalSubject = $"Your personal survey link: {survey.Title}";
                    var personalBody = SurveyEmailTemplateBuilder.RenderInvitationBody(null, personalVars);

                    await _emailService.SendInvitationAsync(new SurveyEmailMessage
                    {
                        ToEmail = participant.EmployeeEmail,
                        ToName = participant.EmployeeName,
                        IncludeRm = false,
                        IncludeVh = false,
                        Subject = personalSubject,
                        HtmlBody = personalBody,
                        SurveyTitle = survey.Title,
                        SurveyLink = surveyLink,
                        Deadline = deadline,
                    });
                });
                await Task.WhenAll(tasks);
            }
        }

        return sent ? distributions.Count : 0;
    }

    /// <summary>
    /// Mode B & C: Individual emails per participant.
    /// </summary>
    private async Task<int> DistributeIndividualAsync(
        Survey survey, SurveyEmailSettings? emailSettings,
        List<(SurveyDistribution dist, SurveyParticipant participant)> distributions,
        string baseUrl, bool includeCcOnEach)
    {
        const int batchSize = 10;
        var sentCount = 0;
        var deadline = survey.ExpiresAt?.ToString("dd MMM yyyy") ?? "No deadline";

        for (var i = 0; i < distributions.Count; i += batchSize)
        {
            var batch = distributions.Skip(i).Take(batchSize).ToList();

            var tasks = batch.Select(async item =>
            {
                var (distribution, participant) = item;
                var surveyLink = $"{baseUrl}/survey/{distribution.Token}";

                var variables = new Dictionary<string, string>
                {
                    ["EmployeeName"] = participant.EmployeeName,
                    ["SurveyTitle"] = survey.Title,
                    ["SurveyLink"] = surveyLink,
                    ["Deadline"] = deadline,
                    ["RmName"] = participant.RmName ?? "",
                    ["VhName"] = participant.VhName ?? "",
                };

                var subject = SurveyEmailTemplateBuilder.RenderSubject(emailSettings?.EmailSubject, variables);
                var body = SurveyEmailTemplateBuilder.RenderInvitationBody(emailSettings?.EmailBody, variables);

                var emailMessage = new SurveyEmailMessage
                {
                    ToEmail = participant.EmployeeEmail,
                    ToName = participant.EmployeeName,
                    RmEmail = participant.RmEmail,
                    VhEmail = participant.VhEmail,
                    IncludeRm = includeCcOnEach && (emailSettings?.IncludeRmByDefault ?? false),
                    IncludeVh = includeCcOnEach && (emailSettings?.IncludeVhByDefault ?? false),
                    AdditionalCcEmails = includeCcOnEach ? emailSettings?.AdditionalCcEmails : null,
                    Subject = subject,
                    HtmlBody = body,
                    SurveyTitle = survey.Title,
                    SurveyLink = surveyLink,
                    Deadline = deadline,
                };

                var sent = await _emailService.SendInvitationAsync(emailMessage);

                distribution.EmailStatus = sent ? SurveyEmailStatus.Sent : SurveyEmailStatus.Failed;
                await _distributionRepo.UpdateAsync(distribution);

                if (sent)
                {
                    participant.Status = SurveyParticipantStatus.Sent;
                    await _participantRepo.UpdateAsync(participant);
                    Interlocked.Increment(ref sentCount);
                }
            });

            await Task.WhenAll(tasks);
        }

        return sentCount;
    }

    /// <summary>
    /// Mode B: Send summary notification to each unique RM/VH.
    /// </summary>
    private async Task SendManagerSummariesAsync(
        Survey survey, List<(SurveyDistribution dist, SurveyParticipant participant)> distributions)
    {
        // Group by RM
        var rmGroups = distributions
            .Where(d => !string.IsNullOrWhiteSpace(d.participant.RmEmail))
            .GroupBy(d => d.participant.RmEmail!.ToLower())
            .ToList();

        foreach (var group in rmGroups)
        {
            var rmEmail = group.Key;
            var rmName = group.First().participant.RmName ?? "Manager";
            var reportees = group.Select(g => g.participant.EmployeeName).ToList();
            await _emailService.SendManagerSummaryAsync(rmEmail, rmName, survey.Title, reportees);
        }

        // Group by VH
        var vhGroups = distributions
            .Where(d => !string.IsNullOrWhiteSpace(d.participant.VhEmail))
            .GroupBy(d => d.participant.VhEmail!.ToLower())
            .ToList();

        foreach (var group in vhGroups)
        {
            var vhEmail = group.Key;
            var vhName = group.First().participant.VhName ?? "Vertical Head";
            var reportees = group.Select(g => g.participant.EmployeeName).ToList();
            await _emailService.SendManagerSummaryAsync(vhEmail, vhName, survey.Title, reportees);
        }
    }

    public async Task<int> SendReminderAsync(Guid surveyId, SendReminderDto dto)
    {
        var survey = await _surveyRepo.GetByIdAsync(surveyId);
        if (survey == null) return 0;

        // Block reminders if survey is not Active
        if (survey.Status != SurveyStatus.Active)
            throw new InvalidOperationException($"Cannot send reminders. Survey must be Active (current status: {survey.Status}).");

        var emailSettings = await _distributionRepo.GetEmailSettingsAsync(surveyId);
        var baseUrl = _config["SurveyEmail:FrontendBaseUrl"]
            ?? _config["FrontendBaseUrl"]
            ?? "http://localhost:5173/novaccodelab";

        var emailMode = emailSettings?.EmailMode ?? SurveyEmailMode.SingleBulk;

        // Gather eligible participants
        var eligibleItems = new List<(SurveyDistribution dist, SurveyParticipant participant)>();

        foreach (var participantId in dto.ParticipantIds)
        {
            var participant = await _participantRepo.GetByIdAsync(participantId);
            if (participant == null || participant.Status == SurveyParticipantStatus.Responded
                || participant.Status == SurveyParticipantStatus.Declined)
                continue;

            var distribution = await _distributionRepo.GetByParticipantAndSurveyAsync(participantId, surveyId);
            if (distribution == null) continue;

            eligibleItems.Add((distribution, participant));
        }

        if (eligibleItems.Count == 0) return 0;

        var sentCount = 0;
        var deadline = survey.ExpiresAt?.ToString("dd MMM yyyy") ?? "No deadline";

        if (emailMode == SurveyEmailMode.SingleBulk)
        {
            // Send ONE bulk reminder email with all participants in TO, RMs/VHs in CC
            var toRecipients = eligibleItems.Select(i => (i.participant.EmployeeEmail, i.participant.EmployeeName)).ToList();
            var ccEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (emailSettings?.IncludeRmByDefault == true)
                foreach (var (_, p) in eligibleItems)
                    if (!string.IsNullOrWhiteSpace(p.RmEmail)) ccEmails.Add(p.RmEmail);

            if (emailSettings?.IncludeVhByDefault == true)
                foreach (var (_, p) in eligibleItems)
                    if (!string.IsNullOrWhiteSpace(p.VhEmail)) ccEmails.Add(p.VhEmail);

            if (!string.IsNullOrWhiteSpace(emailSettings?.AdditionalCcEmails))
                foreach (var cc in emailSettings.AdditionalCcEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    ccEmails.Add(cc);

            var variables = new Dictionary<string, string>
            {
                ["EmployeeName"] = "Team",
                ["SurveyTitle"] = survey.Title,
                ["SurveyLink"] = $"{baseUrl}/survey/[your-link]",
                ["Deadline"] = deadline,
            };

            var subject = SurveyEmailTemplateBuilder.RenderReminderSubject(emailSettings?.EmailSubject, variables);
            var body = SurveyEmailTemplateBuilder.RenderReminderBody(emailSettings?.EmailBody, variables);

            var bulkMessage = new BulkSurveyEmailMessage
            {
                ToRecipients = toRecipients,
                CcEmails = ccEmails.ToList(),
                Subject = subject,
                HtmlBody = body,
            };

            var sent = await _emailService.SendBulkInvitationAsync(bulkMessage);

            // Log reminder for each participant
            foreach (var (dist, participant) in eligibleItems)
            {
                var reminderCount = await _distributionRepo.GetReminderCountAsync(dist.Id);
                await _distributionRepo.CreateReminderLogAsync(new SurveyReminderLog
                {
                    DistributionId = dist.Id,
                    ReminderNumber = reminderCount + 1,
                    SentAt = DateTime.UtcNow,
                    EmailStatus = sent ? SurveyEmailStatus.Sent : SurveyEmailStatus.Failed
                });

                if (sent)
                {
                    participant.Status = SurveyParticipantStatus.Reminded;
                    await _participantRepo.UpdateAsync(participant);
                }
            }

            sentCount = sent ? eligibleItems.Count : 0;
        }
        else
        {
            // Mode B or C: Send individual reminders
            bool includeCcOnEach = emailMode == SurveyEmailMode.IndividualWithCc;

            foreach (var (distribution, participant) in eligibleItems)
            {
                var reminderCount = await _distributionRepo.GetReminderCountAsync(distribution.Id);
                var surveyLink = $"{baseUrl}/survey/{distribution.Token}";

                var variables = new Dictionary<string, string>
                {
                    ["EmployeeName"] = participant.EmployeeName,
                    ["SurveyTitle"] = survey.Title,
                    ["SurveyLink"] = surveyLink,
                    ["Deadline"] = deadline,
                    ["RmName"] = participant.RmName ?? "",
                    ["VhName"] = participant.VhName ?? "",
                };

                var subject = SurveyEmailTemplateBuilder.RenderReminderSubject(emailSettings?.EmailSubject, variables);
                var body = SurveyEmailTemplateBuilder.RenderReminderBody(emailSettings?.EmailBody, variables);

                var emailMessage = new SurveyEmailMessage
                {
                    ToEmail = participant.EmployeeEmail,
                    ToName = participant.EmployeeName,
                    RmEmail = participant.RmEmail,
                    VhEmail = participant.VhEmail,
                    IncludeRm = includeCcOnEach && (emailSettings?.IncludeRmByDefault ?? false),
                    IncludeVh = includeCcOnEach && (emailSettings?.IncludeVhByDefault ?? false),
                    AdditionalCcEmails = includeCcOnEach ? emailSettings?.AdditionalCcEmails : null,
                    Subject = subject,
                    HtmlBody = body,
                    SurveyTitle = survey.Title,
                    SurveyLink = surveyLink,
                    Deadline = deadline,
                };

                var sent = await _emailService.SendReminderAsync(emailMessage);

                await _distributionRepo.CreateReminderLogAsync(new SurveyReminderLog
                {
                    DistributionId = distribution.Id,
                    ReminderNumber = reminderCount + 1,
                    SentAt = DateTime.UtcNow,
                    EmailStatus = sent ? SurveyEmailStatus.Sent : SurveyEmailStatus.Failed
                });

                if (sent)
                {
                    participant.Status = SurveyParticipantStatus.Reminded;
                    await _participantRepo.UpdateAsync(participant);
                    sentCount++;
                }
            }

            // Mode B: Send summary to managers
            if (emailMode == SurveyEmailMode.IndividualWithSummary)
            {
                await SendManagerSummariesAsync(survey, eligibleItems);
            }
        }

        _logger.LogInformation("Reminder ({Mode}): {Sent}/{Total} for survey {SurveyId}",
            emailMode, sentCount, eligibleItems.Count, surveyId);

        return sentCount;
    }

    public Task<byte[]> GetParticipantTemplateAsync()
    {
        var csvContent = "EmployeeId,EmployeeName,EmployeeEmail,RmName,RmEmail,VhName,VhEmail\n";
        csvContent += "EMP001,John Doe,john.doe@company.com,Jane Smith,jane.smith@company.com,Mike Ross,mike.ross@company.com\n";
        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(csvContent));
    }

    // Helper methods
    private static string? GetValue(List<string> cols, int idx)
        => idx >= 0 && idx < cols.Count ? cols[idx].Trim() : null;

    private static bool IsValidEmail(string email)
        => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = "";

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += ch;
            }
        }
        result.Add(current);
        return result;
    }

    private static SurveyParticipantDto MapParticipantToDto(SurveyParticipant p) => new()
    {
        Id = p.Id,
        EmployeeId = p.EmployeeId,
        EmployeeName = p.EmployeeName,
        EmployeeEmail = p.EmployeeEmail,
        RmName = p.RmName,
        RmEmail = p.RmEmail,
        VhName = p.VhName,
        VhEmail = p.VhEmail,
        Status = p.Status,
        UploadedAt = p.UploadedAt,
        LastSentAt = p.Distribution?.SentAt,
        RespondedAt = p.Distribution?.RespondedAt,
        ReminderCount = p.Distribution?.Reminders?.Count ?? 0,
        DeclineInfo = p.StatusLog != null ? new DeclineInfoDto
        {
            DeclinedBy = p.StatusLog.DeclinedBy,
            Reason = p.StatusLog.DeclineReason,
            AttachmentPath = p.StatusLog.DeclineAttachmentPath,
            DeclinedAt = p.StatusLog.DeclinedAt
        } : null
    };

    private static SurveyEmailSettingsDto MapEmailSettingsToDto(SurveyEmailSettings s) => new()
    {
        Id = s.Id,
        SurveyId = s.SurveyId,
        IncludeRmByDefault = s.IncludeRmByDefault,
        IncludeVhByDefault = s.IncludeVhByDefault,
        EmailMode = (int)s.EmailMode,
        AdditionalCcEmails = s.AdditionalCcEmails,
        EmailSubject = s.EmailSubject,
        EmailBody = s.EmailBody,
        ReminderEnabled = s.ReminderEnabled,
        ReminderDays = s.ReminderDays,
        MaxReminders = s.MaxReminders
    };
}
