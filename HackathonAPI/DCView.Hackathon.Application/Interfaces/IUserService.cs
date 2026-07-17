using DCView.Hackathon.Application.DTOs.Admin;

namespace DCView.Hackathon.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserDto request, string createdBy);
    Task<IEnumerable<UserDto>> BulkCreateUsersAsync(IEnumerable<CreateUserDto> requests, string createdBy);
    Task<IEnumerable<UserDto>> GetAllParticipantsAsync();
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<bool> UpdateUserAsync(string userId, UpdateUserDto request, string modifiedBy);
    Task<bool> DeactivateUserAsync(string userId);
}
