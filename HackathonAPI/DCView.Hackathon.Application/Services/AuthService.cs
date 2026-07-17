using DCView.Hackathon.Shared.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using DCView.Hackathon.Application.DTOs.Auth;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordChangeLogRepository _passwordLogRepo;
    private readonly ISecuritySettingsRepository _securityRepo;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepo,
        IPasswordChangeLogRepository passwordLogRepo,
        ISecuritySettingsRepository securityRepo,
        IConfiguration config)
    {
        _userRepo = userRepo;
        _passwordLogRepo = passwordLogRepo;
        _securityRepo = securityRepo;
        _config = config;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepo.GetByUserIDAsync(request.UserID);
        if (user == null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        // Track login
        user.LastLoginAt = DateTimeHelper.Now;
        user.LoginCount++;
        await _userRepo.UpdateAsync(user);

        var token = GenerateJwtToken(user.Id, user.UserID, user.Role);

        SessionInfoDto? sessionInfo = null;
        if (user.Session != null)
        {
            int? remaining = null;
            if (user.Session.ExpiresAt.HasValue)
            {
                remaining = (int)(user.Session.ExpiresAt.Value - DateTimeHelper.Now).TotalMinutes;
                if (remaining < 0) remaining = 0;
            }

            sessionInfo = new SessionInfoDto
            {
                IsActive = user.Session.IsActive,
                DatabaseCreated = user.Session.DatabaseCreated,
                DatabaseName = user.Session.DatabaseName,
                ExpiresAt = user.Session.ExpiresAt,
                RemainingMinutes = remaining
            };
        }

        return new LoginResponseDto
        {
            Token = token,
            UserID = user.UserID,
            Role = user.Role,
            FullName = user.FullName,
            MustChangePassword = user.MustChangePassword,
            Session = sessionInfo
        };
    }

    public async Task<bool> ChangePasswordAsync(int userId, string newPassword, string? changedByUserId = null, string? ipAddress = null)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return false;

        var settings = await _securityRepo.GetAsync();

        // ─── Validate password complexity ─────────────────────────
        var errors = ValidatePasswordComplexity(newPassword, settings);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));

        // ─── Old password cannot be same as new ───────────────────
        if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
            throw new InvalidOperationException("New password cannot be the same as your current password.");

        // ─── Password history check ──────────────────────────────
        if (settings.PasswordHistoryCount > 0)
        {
            var previousPasswords = await _passwordLogRepo.GetByUserIdAsync(userId, settings.PasswordHistoryCount);
            foreach (var prev in previousPasswords)
            {
                if (BCrypt.Net.BCrypt.Verify(newPassword, prev.PasswordHash))
                    throw new InvalidOperationException($"Cannot reuse any of your last {settings.PasswordHistoryCount} passwords.");
            }
        }

        // ─── Update password ─────────────────────────────────────
        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);
        user.PasswordHash = newHash;
        user.MustChangePassword = false;
        user.PasswordResetRequested = false;
        user.ModifiedDate = DateTimeHelper.Now;
        user.ModifiedBy = changedByUserId;
        await _userRepo.UpdateAsync(user);

        // ─── Log the change ──────────────────────────────────────
        await _passwordLogRepo.CreateAsync(new PasswordChangeLog
        {
            UserId = userId,
            PasswordHash = newHash,
            ChangedBy = changedByUserId != null ? "Admin" : "Self",
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTimeHelper.Now,
            IpAddress = ipAddress
        });

        return true;
    }

    // ─── Password Complexity Validation ──────────────────────────

    private static List<string> ValidatePasswordComplexity(string password, SecuritySettings settings)
    {
        var errors = new List<string>();

        if (password.Length < settings.MinLength)
            errors.Add($"Password must be at least {settings.MinLength} characters.");

        if (settings.MaxLength > 0 && password.Length > settings.MaxLength)
            errors.Add($"Password must not exceed {settings.MaxLength} characters.");

        if (settings.RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
            errors.Add("Password must contain at least one uppercase letter.");

        if (settings.RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
            errors.Add("Password must contain at least one lowercase letter.");

        if (settings.RequireDigit && !Regex.IsMatch(password, @"\d"))
            errors.Add("Password must contain at least one digit.");

        if (settings.RequireSpecialChar && !Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;':"",./<>?\\`~]"))
            errors.Add("Password must contain at least one special character.");

        return errors;
    }

    private string GenerateJwtToken(int id, string userId, string role)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, role)
        };

        var expiryHours = int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 8;

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
