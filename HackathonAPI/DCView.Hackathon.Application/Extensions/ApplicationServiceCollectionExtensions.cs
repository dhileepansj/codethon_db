using Microsoft.Extensions.DependencyInjection;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Application.Services;
using DCView.Hackathon.Application.Services.Engines;

namespace DCView.Hackathon.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IHackathonService, HackathonService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISchemaExplorerService, SchemaExplorerService>();
        services.AddScoped<IFileManagerService, FileManagerService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IAiDetectionService, AiDetectionService>();

        // Database engine abstractions
        services.AddScoped<SqlServerEngine>();
        services.AddScoped<OracleEngine>();
        services.AddScoped<IDbEngineFactory, DbEngineFactory>();

        // MCQ Service
        services.AddScoped<IMcqService, McqService>();

        return services;
    }
}
