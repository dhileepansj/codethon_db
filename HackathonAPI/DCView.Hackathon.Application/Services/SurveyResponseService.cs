using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;
using DCView.Hackathon.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace DCView.Hackathon.Application.Services;

public class SurveyResponseService : ISurveyResponseService
{
    private readonly ISurveyDistributionRepository _distributionRepo;
    private readonly ISurveyParticipantRepository _participantRepo;
    private readonly ISurveyResponseRepository _responseRepo;
    private readonly ISurveyOtpRepository _otpRepo;
    private readonly ISurveyFieldRepository _fieldRepo;
    private readonly ISurveyEmailService _emailService;
    private readonly ILogger<SurveyResponseService> _logger;

    private const int OTP_EXPIRY_MINUTES = 5;
    private const int MAX_OTP_ATTEMPTS = 3;
    private const int MAX_OTP_RESENDS = 3;
    private const int LOCKOUT_MINUTES = 15;

    public SurveyResponseService(
        ISurveyDistributionRepository distributionRepo,
        ISurveyParticipantRepository participantRepo,
        ISurveyResponseRepository responseRepo,
        ISurveyOtpRepository otpRepo,
        ISurveyFieldRepository fieldRepo,
        ISurveyEmailService emailService,
        ILogger<SurveyResponseService> logger)
    {
        _distributionRepo = distributionRepo;
        _participantRepo = participantRepo;
        _responseRepo = responseRepo;
        _otpRepo = otpRepo;
        _fieldRepo = fieldRepo;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<VerifyEmailResponseDto> VerifyEmailAsync(string token, VerifyEmailRequestDto dto)
    {
        var distribution = await _distributionRepo.GetByTokenAsync(token);
        if (distribution == null)
            return new VerifyEmailResponseDto { IsValid = false, Message = "Invalid survey link." };

        var survey = distribution.Survey;
        if (survey == null || survey.Status != SurveyStatus.Active)
            return new VerifyEmailResponseDto { IsValid = false, Message = "This survey is no longer active." };

        if (survey.ExpiresAt.HasValue && survey.ExpiresAt.Value < DateTime.UtcNow)
            return new VerifyEmailResponseDto { IsValid = false, Message = "This survey has expired." };

        // Check email matches the distribution's participant
        var participant = distribution.Participant;
        if (participant == null || !participant.EmployeeEmail.Equals(dto.Email, StringComparison.OrdinalIgnoreCase))
            return new VerifyEmailResponseDto { IsValid = false, Message = "This email is not registered for this survey. Contact your administrator." };

        // Check if already responded
        if (participant.Status == SurveyParticipantStatus.Responded && !survey.AllowMultiple)
            return new VerifyEmailResponseDto { IsValid = false, Message = "You have already submitted your response for this survey." };

        // Check if declined
        if (participant.Status == SurveyParticipantStatus.Declined)
            return new VerifyEmailResponseDto { IsValid = false, Message = "Your participation has been marked as declined. Contact your administrator if this is incorrect." };

        return new VerifyEmailResponseDto
        {
            IsValid = true,
            Message = "Email verified. OTP will be sent.",
            MaskedEmail = MaskEmail(participant.EmployeeEmail)
        };
    }

    public async Task<SendOtpResponseDto> SendOtpAsync(string token, SendOtpRequestDto dto, string? ipAddress)
    {
        var distribution = await _distributionRepo.GetByTokenAsync(token);
        if (distribution?.Participant == null)
            return new SendOtpResponseDto { Success = false, Message = "Invalid survey link." };

        var participant = distribution.Participant;
        if (!participant.EmployeeEmail.Equals(dto.Email, StringComparison.OrdinalIgnoreCase))
            return new SendOtpResponseDto { Success = false, Message = "Email does not match." };

        // Check existing OTP and rate limits
        var existingOtp = await _otpRepo.GetLatestByParticipantAndSurveyAsync(participant.Id, distribution.SurveyId);
        if (existingOtp != null)
        {
            if (existingOtp.LockedUntil.HasValue && existingOtp.LockedUntil.Value > DateTime.UtcNow)
            {
                var remaining = (int)(existingOtp.LockedUntil.Value - DateTime.UtcNow).TotalMinutes;
                return new SendOtpResponseDto { Success = false, Message = $"Too many attempts. Try again in {remaining + 1} minutes." };
            }

            if (existingOtp.ResendCount >= MAX_OTP_RESENDS && existingOtp.CreatedAt > DateTime.UtcNow.AddMinutes(-OTP_EXPIRY_MINUTES))
                return new SendOtpResponseDto { Success = false, Message = "Maximum resend limit reached. Wait for the current OTP to expire." };
        }

        // Invalidate previous OTPs
        await _otpRepo.InvalidateAllForParticipantAsync(participant.Id, distribution.SurveyId);

        // Generate new OTP
        var otpCode = GenerateOtp();
        var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);

        var otp = new SurveyOtp
        {
            ParticipantId = participant.Id,
            SurveyId = distribution.SurveyId,
            OtpHash = otpHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
            IpAddress = ipAddress,
            ResendCount = (existingOtp?.ResendCount ?? 0) + 1
        };

        await _otpRepo.CreateAsync(otp);

        // Send OTP via email
        var surveyTitle = distribution.Survey?.Title ?? "Survey";
        var emailSent = await _emailService.SendOtpAsync(
            participant.EmployeeEmail,
            participant.EmployeeName,
            otpCode,
            surveyTitle);

        if (!emailSent)
        {
            _logger.LogWarning("Failed to send OTP email to {Email}", participant.EmployeeEmail);
        }

        _logger.LogInformation("OTP generated for {Email}, survey: {SurveyTitle}",
            MaskEmail(participant.EmployeeEmail), surveyTitle);

        return new SendOtpResponseDto
        {
            Success = true,
            Message = "OTP sent successfully.",
            MaskedEmail = MaskEmail(participant.EmployeeEmail),
            ExpiresInSeconds = OTP_EXPIRY_MINUTES * 60
        };
    }

    public async Task<VerifyOtpResponseDto> VerifyOtpAsync(string token, VerifyOtpRequestDto dto, string? ipAddress)
    {
        var distribution = await _distributionRepo.GetByTokenAsync(token);
        if (distribution?.Participant == null)
            return new VerifyOtpResponseDto { Success = false, Message = "Invalid survey link." };

        var participant = distribution.Participant;
        if (!participant.EmployeeEmail.Equals(dto.Email, StringComparison.OrdinalIgnoreCase))
            return new VerifyOtpResponseDto { Success = false, Message = "Email does not match." };

        var otp = await _otpRepo.GetLatestByParticipantAndSurveyAsync(participant.Id, distribution.SurveyId);
        if (otp == null)
            return new VerifyOtpResponseDto { Success = false, Message = "No OTP found. Please request a new one." };

        // Check lockout
        if (otp.LockedUntil.HasValue && otp.LockedUntil.Value > DateTime.UtcNow)
        {
            var remaining = (int)(otp.LockedUntil.Value - DateTime.UtcNow).TotalMinutes;
            return new VerifyOtpResponseDto { Success = false, Message = $"Account locked. Try again in {remaining + 1} minutes." };
        }

        // Check expiry
        if (otp.ExpiresAt < DateTime.UtcNow)
            return new VerifyOtpResponseDto { Success = false, Message = "OTP has expired. Please request a new one." };

        // Check already verified
        if (otp.IsVerified)
            return new VerifyOtpResponseDto { Success = false, Message = "OTP already used. Please request a new one." };

        // Verify OTP
        if (!BCrypt.Net.BCrypt.Verify(dto.Otp, otp.OtpHash))
        {
            otp.Attempts++;
            if (otp.Attempts >= MAX_OTP_ATTEMPTS)
            {
                otp.LockedUntil = DateTime.UtcNow.AddMinutes(LOCKOUT_MINUTES);
            }
            await _otpRepo.UpdateAsync(otp);

            var remaining = MAX_OTP_ATTEMPTS - otp.Attempts;
            var message = remaining > 0
                ? $"Invalid OTP. {remaining} attempt(s) remaining."
                : $"Too many failed attempts. Account locked for {LOCKOUT_MINUTES} minutes.";

            return new VerifyOtpResponseDto { Success = false, Message = message };
        }

        // Mark OTP as verified
        otp.IsVerified = true;
        await _otpRepo.UpdateAsync(otp);

        // Generate session token for subsequent requests
        var sessionToken = Guid.NewGuid().ToString("N");

        return new VerifyOtpResponseDto
        {
            Success = true,
            Message = "OTP verified successfully.",
            SessionToken = sessionToken,
            ParticipantInfo = new ParticipantInfoDto
            {
                EmployeeId = participant.EmployeeId,
                EmployeeName = participant.EmployeeName,
                EmployeeEmail = participant.EmployeeEmail
            }
        };
    }

    public async Task<SurveyFormDto?> GetSurveyFormAsync(string token, string sessionToken)
    {
        var distribution = await _distributionRepo.GetByTokenAsync(token);
        if (distribution?.Survey == null || distribution.Participant == null)
            return null;

        var survey = distribution.Survey;
        var participant = distribution.Participant;
        var fields = await _fieldRepo.GetBySurveyIdWithDependenciesAsync(survey.Id);

        return new SurveyFormDto
        {
            SurveyId = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Participant = new ParticipantInfoDto
            {
                EmployeeId = participant.EmployeeId,
                EmployeeName = participant.EmployeeName,
                EmployeeEmail = participant.EmployeeEmail
            },
            Fields = fields.Select(f => new SurveyFieldDto
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
                Dependencies = f.Dependencies?.Select(d => new FieldDependencyDto
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
                }).ToList() ?? new()
            }).ToList(),
            ThankYouMessage = survey.ThankYouMessage
        };
    }

    public async Task<(bool Success, string Message)> SubmitResponseAsync(
        string token, string sessionToken, SubmitSurveyResponseDto dto, string? ipAddress, string? userAgent)
    {
        var distribution = await _distributionRepo.GetByTokenAsync(token);
        if (distribution?.Participant == null || distribution.Survey == null)
            return (false, "Invalid survey link.");

        var survey = distribution.Survey;
        var participant = distribution.Participant;

        // Check if survey is active
        if (survey.Status != SurveyStatus.Active)
            return (false, "This survey is no longer active.");

        // Check one-time submission
        if (!survey.AllowMultiple)
        {
            var hasResponded = await _responseRepo.HasRespondedAsync(participant.Id, survey.Id);
            if (hasResponded)
                return (false, "You have already submitted your response for this survey.");
        }

        // Create response
        var response = new SurveyResponse
        {
            SurveyId = survey.Id,
            ParticipantId = participant.Id,
            DistributionId = distribution.Id,
            SubmittedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            TimeTakenSeconds = dto.TimeTakenSeconds,
            Answers = dto.Answers.Select(a => new SurveyResponseAnswer
            {
                FieldId = a.FieldId,
                Value = a.Value,
                FileUrl = a.FileUrl
            }).ToList()
        };

        await _responseRepo.CreateAsync(response);

        // Update distribution
        distribution.RespondedAt = DateTime.UtcNow;
        await _distributionRepo.UpdateAsync(distribution);

        // Update participant status
        participant.Status = SurveyParticipantStatus.Responded;
        await _participantRepo.UpdateAsync(participant);

        _logger.LogInformation("Survey response submitted by {Email} for survey {SurveyId}",
            participant.EmployeeEmail, survey.Id);

        return (true, survey.ThankYouMessage ?? "Thank you for your response!");
    }

    // Helper methods
    private static string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;

        var local = parts[0];
        var masked = local.Length > 3
            ? $"{local[..2]}***{local[^1]}"
            : $"{local[0]}***";

        return $"{masked}@{parts[1]}";
    }
}
