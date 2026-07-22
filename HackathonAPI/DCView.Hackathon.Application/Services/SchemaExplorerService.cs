using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DCView.Hackathon.Application.DTOs.Schema;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Shared.Helpers;

namespace DCView.Hackathon.Application.Services;

public class SchemaExplorerService : ISchemaExplorerService
{
    private readonly ISessionRepository _sessionRepo;
    private readonly IHackathonConfigRepository _configRepo;
    private readonly IUserRepository _userRepo;
    private readonly IDbEngineFactory _engineFactory;
    private readonly IConfiguration _config;

    public SchemaExplorerService(
        ISessionRepository sessionRepo,
        IHackathonConfigRepository configRepo,
        IUserRepository userRepo,
        IDbEngineFactory engineFactory,
        IConfiguration config)
    {
        _sessionRepo = sessionRepo;
        _configRepo = configRepo;
        _userRepo = userRepo;
        _engineFactory = engineFactory;
        _config = config;
    }

    public async Task<DatabaseOverviewDto> GetOverviewAsync(int userId)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetOverviewAsync(connStr);
    }

    public async Task<IEnumerable<TableInfoDto>> GetTablesAsync(int userId)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetTablesAsync(connStr);
    }

    public async Task<IEnumerable<ColumnInfoDto>> GetTableColumnsAsync(int userId, string tableName)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetTableColumnsAsync(connStr, tableName);
    }

    public async Task<IEnumerable<IndexInfoDto>> GetTableIndexesAsync(int userId, string tableName)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetTableIndexesAsync(connStr, tableName);
    }

    public async Task<TableDataDto> GetTableDataAsync(int userId, string tableName, int page, int pageSize)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetTableDataAsync(connStr, tableName, page, pageSize);
    }

    public async Task<IEnumerable<DbObjectDto>> GetViewsAsync(int userId)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetViewsAsync(connStr);
    }

    public async Task<string?> GetViewDefinitionAsync(int userId, string viewName)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetViewDefinitionAsync(connStr, viewName);
    }

    public async Task<IEnumerable<DbObjectDto>> GetProceduresAsync(int userId)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetProceduresAsync(connStr);
    }

    public async Task<string?> GetProcedureDefinitionAsync(int userId, string procName)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetProcedureDefinitionAsync(connStr, procName);
    }

    public async Task<IEnumerable<DbObjectDto>> GetFunctionsAsync(int userId)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetFunctionsAsync(connStr);
    }

    public async Task<string?> GetFunctionDefinitionAsync(int userId, string funcName)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetFunctionDefinitionAsync(connStr, funcName);
    }

    public async Task<IEnumerable<DbObjectDto>> GetTriggersAsync(int userId)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetTriggersAsync(connStr);
    }

    public async Task<string?> GetTriggerDefinitionAsync(int userId, string triggerName)
    {
        var (engine, connStr) = await ResolveEngineAndConnectionAsync(userId);
        return await engine.GetTriggerDefinitionAsync(connStr, triggerName);
    }

    // ─── Private helpers ──────────────────────────────────────────

    private async Task<(IDbEngine Engine, string ConnectionString)> ResolveEngineAndConnectionAsync(int userId)
    {
        var session = await _sessionRepo.GetByUserIdAsync(userId);
        if (session == null)
            throw new InvalidOperationException("Session not found. Please contact the administrator.");

        if (!session.DatabaseCreated || string.IsNullOrEmpty(session.DbLoginPassword))
            throw new InvalidOperationException("Database not created yet. Please create your database first.");

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var hackConfig = await _configRepo.GetActiveConfigAsync(user.DbEnginePreference)
            ?? await _configRepo.GetActiveConfigAsync();
        if (hackConfig == null)
            throw new InvalidOperationException("Hackathon server is not configured. Please contact the administrator.");

        string encKey = _config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption key is not configured.");

        string loginPassword;
        try
        {
            loginPassword = EncryptionHelper.Decrypt(session.DbLoginPassword, encKey);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decrypt database credentials. The database may need to be reset. ({ex.Message})");
        }

        var engine = _engineFactory.GetEngine(hackConfig.DbEngineType);
        var connStr = engine.BuildParticipantConnectionString(hackConfig, session, loginPassword);

        return (engine, connStr);
    }
}
