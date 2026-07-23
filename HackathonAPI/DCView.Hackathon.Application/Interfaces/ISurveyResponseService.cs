using DCView.Hackathon.Application.DTOs.Survey;

namespace DCView.Hackathon.Application.Interfaces;

public interface ISurveyResponseService
{
    /// <summary>
    /// Verify that the email exists in participant list and is eligible to respond.
    /// </summary>
    Task<VerifyEmailResponseDto> VerifyEmailAsync(string token, VerifyEmailRequestDto dto);

    /// <summary>
    /// Generate and send OTP to the participant's email.
    /// </summary>
    Task<SendOtpResponseDto> SendOtpAsync(string token, SendOtpRequestDto dto, string? ipAddress);

    /// <summary>
    /// Verify OTP and return participant info + session token.
    /// </summary>
    Task<VerifyOtpResponseDto> VerifyOtpAsync(string token, VerifyOtpRequestDto dto, string? ipAddress);

    /// <summary>
    /// Get the survey form for a verified participant.
    /// </summary>
    Task<SurveyFormDto?> GetSurveyFormAsync(string token, string sessionToken);

    /// <summary>
    /// Submit the survey response.
    /// </summary>
    Task<(bool Success, string Message)> SubmitResponseAsync(string token, string sessionToken, SubmitSurveyResponseDto dto, string? ipAddress, string? userAgent);
}
