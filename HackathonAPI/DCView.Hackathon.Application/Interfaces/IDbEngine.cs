using DCView.Hackathon.Application.DTOs.Hackathon;
using DCView.Hackathon.Application.DTOs.Schema;
using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Application.Interfaces;

/// <summary>
/// Abstraction over a participant database engine (SQL Server, Oracle, etc.).
/// Each implementation handles connection, database/schema creation, query execution,
/// schema exploration, and guardrails for a specific DB vendor.
/// </summary>
public interface IDbEngine
{
    /// <summary>
    /// Creates a participant database (SQL Server) or schema/user (Oracle).
    /// Returns the database/schema name and login credentials.
    /// </summary>
    Task<DbCreationResult> CreateParticipantDatabaseAsync(
        HackathonConfig config,
        string userId,
        string adminPassword,
        string encryptionKey);

    /// <summary>
    /// Executes SQL batches against the participant's database.
    /// Handles batch splitting, guardrails, and result pagination.
    /// </summary>
    Task<ExecuteResultDto> ExecuteAsync(
        HackathonConfig config,
        HackathonSession session,
        string userId,
        string loginPassword,
        ExecuteRequestDto request);

    /// <summary>
    /// Executes scaffold scripts against a newly created participant database.
    /// </summary>
    Task ExecuteScaffoldScriptsAsync(
        HackathonConfig config,
        string databaseName,
        string adminPassword,
        IEnumerable<ScaffoldScript> scripts);

    // ─── Schema Explorer Methods ─────────────────────────────────

    Task<DatabaseOverviewDto> GetOverviewAsync(string connectionString);
    Task<IEnumerable<TableInfoDto>> GetTablesAsync(string connectionString);
    Task<IEnumerable<ColumnInfoDto>> GetTableColumnsAsync(string connectionString, string tableName);
    Task<IEnumerable<IndexInfoDto>> GetTableIndexesAsync(string connectionString, string tableName);
    Task<TableDataDto> GetTableDataAsync(string connectionString, string tableName, int page, int pageSize);
    Task<IEnumerable<DbObjectDto>> GetViewsAsync(string connectionString);
    Task<string?> GetViewDefinitionAsync(string connectionString, string viewName);
    Task<IEnumerable<DbObjectDto>> GetProceduresAsync(string connectionString);
    Task<string?> GetProcedureDefinitionAsync(string connectionString, string procName);
    Task<IEnumerable<DbObjectDto>> GetFunctionsAsync(string connectionString);
    Task<string?> GetFunctionDefinitionAsync(string connectionString, string funcName);
    Task<IEnumerable<DbObjectDto>> GetTriggersAsync(string connectionString);
    Task<string?> GetTriggerDefinitionAsync(string connectionString, string triggerName);

    /// <summary>
    /// Builds the connection string for a participant's database.
    /// </summary>
    string BuildParticipantConnectionString(HackathonConfig config, HackathonSession session, string loginPassword);

    /// <summary>
    /// Validates a SQL batch against engine-specific blocked patterns.
    /// Returns null if safe, or a violation message.
    /// </summary>
    string? ValidateBatchSafety(string batch);

    /// <summary>
    /// Splits SQL text into executable batches (e.g., GO for SQL Server, / for Oracle).
    /// </summary>
    List<string> SplitBatches(string sql);
}

/// <summary>
/// Result of creating a participant database/schema.
/// </summary>
public class DbCreationResult
{
    public string DatabaseName { get; set; } = string.Empty;
    public string LoginName { get; set; } = string.Empty;
    public string LoginPassword { get; set; } = string.Empty;
}
