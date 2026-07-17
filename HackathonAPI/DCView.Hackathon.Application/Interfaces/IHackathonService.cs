using DCView.Hackathon.Application.DTOs.Hackathon;

namespace DCView.Hackathon.Application.Interfaces;

public interface IHackathonService
{
    Task<CreateDatabaseResultDto> CreateDatabaseAsync(int userId);
    Task<ExecuteResultDto> ExecuteAsync(int userId, ExecuteRequestDto request);
    Task<SessionStatusDto> GetSessionStatusAsync(int userId);
}
