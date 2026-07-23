using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Application.DTOs.Survey;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.API.Controllers;

/// <summary>
/// Public-facing controller for survey respondents (no JWT auth required).
/// Authentication is handled via token + OTP.
/// </summary>
[Route("api/survey-respond/{token}")]
[ApiController]
public class SurveyRespondController : ControllerBase
{
    private readonly ISurveyResponseService _responseService;

    public SurveyRespondController(ISurveyResponseService responseService)
    {
        _responseService = responseService;
    }

    /// <summary>
    /// Step 1: Verify that the email belongs to a valid participant.
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(string token, [FromBody] VerifyEmailRequestDto dto)
    {
        var result = await _responseService.VerifyEmailAsync(token, dto);
        if (!result.IsValid)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Step 2: Send OTP to the participant's email.
    /// </summary>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp(string token, [FromBody] SendOtpRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _responseService.SendOtpAsync(token, dto, ipAddress);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Step 3: Verify OTP and get participant info + session token.
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(string token, [FromBody] VerifyOtpRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _responseService.VerifyOtpAsync(token, dto, ipAddress);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Step 4: Get the survey form (requires session token from OTP verification).
    /// </summary>
    [HttpGet("form")]
    public async Task<IActionResult> GetForm(string token, [FromHeader(Name = "X-Session-Token")] string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            return Unauthorized(new { message = "Session token required. Please verify your OTP first." });

        var form = await _responseService.GetSurveyFormAsync(token, sessionToken);
        if (form == null)
            return NotFound(new { message = "Survey not found or invalid token." });
        return Ok(form);
    }

    /// <summary>
    /// Step 5: Submit the survey response.
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> Submit(string token, [FromHeader(Name = "X-Session-Token")] string sessionToken,
        [FromBody] SubmitSurveyResponseDto dto)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            return Unauthorized(new { message = "Session token required." });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, message) = await _responseService.SubmitResponseAsync(token, sessionToken, dto, ipAddress, userAgent);
        if (!success)
            return Conflict(new { message });
        return Ok(new { message });
    }
}
