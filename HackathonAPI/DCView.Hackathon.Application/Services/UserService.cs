using DCView.Hackathon.Shared.Helpers;
using DCView.Hackathon.Application.DTOs.Admin;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<UserDto> CreateUserAsync(CreateUserDto request, string createdBy)
    {
        if (await _userRepo.ExistsAsync(request.UserID))
            throw new InvalidOperationException($"User '{request.UserID}' already exists.");

        var user = new User
        {
            UserID = request.UserID,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            FullName = request.FullName,
            Email = request.Email,
            Role = "Participant",
            IsActive = true,
            CreatedBy = createdBy,
            CreatedDate = DateTimeHelper.Now
        };

        await _userRepo.CreateAsync(user);
        return MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> BulkCreateUsersAsync(IEnumerable<CreateUserDto> requests, string createdBy)
    {
        var results = new List<UserDto>();
        foreach (var req in requests)
        {
            try
            {
                var dto = await CreateUserAsync(req, createdBy);
                results.Add(dto);
            }
            catch
            {
                // Skip duplicates in bulk creation
            }
        }
        return results;
    }

    public async Task<IEnumerable<UserDto>> GetAllParticipantsAsync()
    {
        var users = await _userRepo.GetAllParticipantsAsync();
        return users.Select(MapToDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<bool> UpdateUserAsync(string userId, UpdateUserDto request, string modifiedBy)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return false;

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Email != null) user.Email = request.Email;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        user.ModifiedBy = modifiedBy;
        user.ModifiedDate = DateTimeHelper.Now;

        await _userRepo.UpdateAsync(user);
        return true;
    }

    public async Task<bool> DeactivateUserAsync(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return false;

        user.IsActive = false;
        user.ModifiedDate = DateTimeHelper.Now;
        await _userRepo.UpdateAsync(user);
        return true;
    }

    private static UserDto MapToDto(User user) => new UserDto
    {
        Id = user.Id,
        UserID = user.UserID,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        MustChangePassword = user.MustChangePassword,
        PasswordResetRequested = user.PasswordResetRequested,
        CreatedDate = user.CreatedDate,
        LastLoginAt = user.LastLoginAt,
        LoginCount = user.LoginCount,
        Session = user.Session != null ? new SessionSummaryDto
        {
            IsActive = user.Session.IsActive && (user.Session.ExpiresAt == null || user.Session.ExpiresAt > DateTimeHelper.Now),
            IsExpired = user.Session.IsActive && user.Session.ExpiresAt.HasValue && user.Session.ExpiresAt < DateTimeHelper.Now,
            DatabaseCreated = user.Session.DatabaseCreated,
            DatabaseName = user.Session.DatabaseName,
            StartedAt = user.Session.StartedAt,
            ExpiresAt = user.Session.ExpiresAt
        } : null
    };
}

