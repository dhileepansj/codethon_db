using Microsoft.Extensions.DependencyInjection;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Repositories;

namespace DCView.Hackathon.Infrastructure.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IExecutionHistoryRepository, ExecutionHistoryRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IFolderRepository, FolderRepository>();
        services.AddScoped<IHackathonConfigRepository, HackathonConfigRepository>();
        services.AddScoped<ITabSwitchLogRepository, TabSwitchLogRepository>();
        services.AddScoped<IAiDetectionLogRepository, AiDetectionLogRepository>();
        services.AddScoped<IAiDetectionSettingsRepository, AiDetectionSettingsRepository>();
        services.AddScoped<IAiBlockedSaveRepository, AiBlockedSaveRepository>();
        services.AddScoped<ISubmissionFileRepository, SubmissionFileRepository>();
        services.AddScoped<IPasswordChangeLogRepository, PasswordChangeLogRepository>();
        services.AddScoped<ISecuritySettingsRepository, SecuritySettingsRepository>();
        services.AddScoped<IScaffoldScriptRepository, ScaffoldScriptRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        return services;
    }
}
