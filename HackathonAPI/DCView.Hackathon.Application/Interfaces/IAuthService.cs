using DCView.Hackathon.Application.DTOs.Auth;

namespace DCView.Hackathon.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    Task<bool> ChangePasswordAsync(int userId, string newPassword, string? changedByUserId = null, string? ipAddress = null);
}
