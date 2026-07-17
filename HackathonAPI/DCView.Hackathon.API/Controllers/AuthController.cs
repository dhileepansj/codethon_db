using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Application.DTOs.Auth;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepo;

    public AuthController(IAuthService authService, IUserRepository userRepo)
    {
        _authService = authService;
        _userRepo = userRepo;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { message = "Invalid credentials" });
        return Ok(result);
    }

    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        var userId = User.FindFirst(ClaimTypes.Name)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ok(new { id, userId, role });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Password is required" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var success = await _authService.ChangePasswordAsync(userId, request.NewPassword, null, ipAddress);
        if (!success)
            return NotFound(new { message = "User not found" });

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserID))
            return BadRequest(new { message = "User ID is required" });

        var user = await _userRepo.GetByUserIDAsync(request.UserID);
        if (user == null || !user.IsActive)
            return Ok(new { message = "If the user exists, the request has been submitted to the administrator." });

        user.PasswordResetRequested = true;
        await _userRepo.UpdateAsync(user);

        return Ok(new { message = "Password reset request submitted. The administrator will reset your password and notify you." });
    }
}
