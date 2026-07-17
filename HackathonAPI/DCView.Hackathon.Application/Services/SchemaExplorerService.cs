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
    private readonly IConfiguration _config;

    public SchemaExplorerService(
        ISessionRepository sessionRepo,
        IHackathonConfigRepository configRepo,
        IUserRepository userRepo,
        IConfiguration config)
    {
        _sessionRepo = sessionRepo;
        _configRepo = configRepo;
        _userRepo = userRepo;
        _config = config;
    }

    public async Task<DatabaseOverviewDto> GetOverviewAsync(int userId)
    {
        using var conn = await GetUserConnectionAsync(userId);

        var overview = new DatabaseOverviewDto
        {
            DatabaseName = conn.Database
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE') AS TableCount,
                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.VIEWS) AS ViewCount,
                (SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0) AS ProcCount,
                (SELECT COUNT(*) FROM sys.objects WHERE type IN ('FN','IF','TF') AND is_ms_shipped = 0) AS FuncCount,
                (SELECT COUNT(*) FROM sys.triggers WHERE is_ms_shipped = 0) AS TriggerCount;";

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            overview.TableCount = reader.GetInt32(0);
            overview.ViewCount = reader.GetInt32(1);
            overview.ProcedureCount = reader.GetInt32(2);
            overview.FunctionCount = reader.GetInt32(3);
            overview.TriggerCount = reader.GetInt32(4);
        }
        reader.Close();

        // Get DB size
        using var sizeCmd = conn.CreateCommand();
        sizeCmd.CommandText = "EXEC sp_spaceused;";
        using var sizeReader = await sizeCmd.ExecuteReaderAsync();
        if (await sizeReader.ReadAsync())
        {
            var sizeStr = sizeReader["database_size"]?.ToString()?.Replace(" MB", "").Trim();
            if (decimal.TryParse(sizeStr, out var sizeMb))
                overview.SizeMB = sizeMb;
        }

        return overview;
    }

    public async Task<IEnumerable<TableInfoDto>> GetTablesAsync(int userId)
    {
        using var conn = await GetUserConnectionAsync(userId);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                t.TABLE_SCHEMA AS [Schema],
                t.TABLE_NAME AS TableName,
                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA) AS ColumnCount,
                ISNULL(p.rows, 0) AS [RowCount],
                o.create_date AS CreateDate
            FROM INFORMATION_SCHEMA.TABLES t
            LEFT JOIN sys.objects o ON o.name = t.TABLE_NAME AND o.type = 'U'
            LEFT JOIN sys.partitions p ON p.object_id = o.object_id AND p.index_id IN (0,1)
            WHERE t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME;";

        var tables = new List<TableInfoDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(new TableInfoDto
            {
                Schema = reader["Schema"].ToString()!,
                TableName = reader["TableName"].ToString()!,
                ColumnCount = Convert.ToInt32(reader["ColumnCount"]),
                RowCount = Convert.ToInt64(reader["RowCount"]),
                CreateDate = reader["CreateDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreateDate"]) : null
            });
        }

        return tables;
    }

    public async Task<IEnumerable<ColumnInfoDto>> GetTableColumnsAsync(int userId, string tableName)
    {
        using var conn = await GetUserConnectionAsync(userId);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE + CASE
                    WHEN c.CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN '(' + CAST(c.CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')'
                    WHEN c.NUMERIC_PRECISION IS NOT NULL AND c.DATA_TYPE NOT IN ('int','bigint','smallint','tinyint','bit')
                        THEN '(' + CAST(c.NUMERIC_PRECISION AS VARCHAR) + ',' + CAST(ISNULL(c.NUMERIC_SCALE,0) AS VARCHAR) + ')'
                    ELSE '' END AS DataType,
                c.CHARACTER_MAXIMUM_LENGTH AS MaxLength,
                CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IsNullable,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
                COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity,
                CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsForeignKey,
                fk.ReferencedTable AS ForeignKeyTable,
                c.COLUMN_DEFAULT AS DefaultValue,
                c.ORDINAL_POSITION
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
            LEFT JOIN (
                SELECT
                    ccu.TABLE_NAME, ccu.COLUMN_NAME,
                    kcu.TABLE_NAME AS ReferencedTable
                FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON rc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON rc.UNIQUE_CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
            ) fk ON fk.TABLE_NAME = c.TABLE_NAME AND fk.COLUMN_NAME = c.COLUMN_NAME
            WHERE c.TABLE_NAME = @TableName
            ORDER BY c.ORDINAL_POSITION;";
        cmd.Parameters.AddWithValue("@TableName", tableName);

        var columns = new List<ColumnInfoDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnInfoDto
            {
                ColumnName = reader["COLUMN_NAME"].ToString()!,
                DataType = reader["DataType"].ToString()!,
                MaxLength = reader["MaxLength"] != DBNull.Value ? Convert.ToInt32(reader["MaxLength"]) : null,
                IsNullable = Convert.ToBoolean(reader["IsNullable"]),
                IsPrimaryKey = Convert.ToBoolean(reader["IsPrimaryKey"]),
                IsIdentity = Convert.ToInt32(reader["IsIdentity"]) == 1,
                IsForeignKey = Convert.ToBoolean(reader["IsForeignKey"]),
                ForeignKeyTable = reader["ForeignKeyTable"]?.ToString(),
                DefaultValue = reader["DefaultValue"]?.ToString(),
                OrdinalPosition = Convert.ToInt32(reader["ORDINAL_POSITION"])
            });
        }

        return columns;
    }

    public async Task<IEnumerable<IndexInfoDto>> GetTableIndexesAsync(int userId, string tableName)
    {
        using var conn = await GetUserConnectionAsync(userId);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                i.name AS IndexName,
                i.type_desc AS IndexType,
                i.is_unique AS IsUnique,
                i.is_primary_key AS IsPrimaryKey,
                STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS Columns
            FROM sys.indexes i
            JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            WHERE i.object_id = OBJECT_ID(@TableName) AND i.name IS NOT NULL
            GROUP BY i.name, i.type_desc, i.is_unique, i.is_primary_key
            ORDER BY i.is_primary_key DESC, i.name;";
        cmd.Parameters.AddWithValue("@TableName", tableName);

        var indexes = new List<IndexInfoDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(new IndexInfoDto
            {
                IndexName = reader["IndexName"].ToString()!,
                IndexType = reader["IndexType"].ToString()!,
                IsUnique = Convert.ToBoolean(reader["IsUnique"]),
                IsPrimaryKey = Convert.ToBoolean(reader["IsPrimaryKey"]),
                Columns = reader["Columns"].ToString()!
            });
        }

        return indexes;
    }

    public async Task<TableDataDto> GetTableDataAsync(int userId, string tableName, int page, int pageSize)
    {
        using var conn = await GetUserConnectionAsync(userId);

        // Get total count
        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = $"SELECT COUNT(*) FROM [{tableName}]";
        var totalRows = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        // Get paginated data
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM [{tableName}] ORDER BY (SELECT NULL) OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);

        var columns = new List<string>();
        var rows = new List<Dictionary<string, object?>>();

        using var reader = await cmd.ExecuteReaderAsync();
        for (int c = 0; c < reader.FieldCount; c++)
            columns.Add(reader.GetName(c));

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int c = 0; c < reader.FieldCount; c++)
                row[columns[c]] = reader.IsDBNull(c) ? null : reader.GetValue(c);
            rows.Add(row);
        }

        return new TableDataDto
        {
            Columns = columns,
            Rows = rows,
            TotalRows = totalRows,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<DbObjectDto>> GetViewsAsync(int userId)
    {
        using var conn = await GetUserConnectionAsync(userId);
        return await GetObjectsAsync(conn, "SELECT name, schema_name(schema_id) AS [Schema], 'VIEW' AS Type, create_date, modify_date FROM sys.views WHERE is_ms_shipped = 0 ORDER BY name;");
    }

    public async Task<string?> GetViewDefinitionAsync(int userId, string viewName)
    {
        using var conn = await GetUserConnectionAsync(userId);
        return await GetObjectDefinitionAsync(conn, viewName);
    }

    public async Task<IEnumerable<DbObjectDto>> GetProceduresAsync(int userId)
    {
        using var conn = await GetUserConnectionAsync(userId);
        return await GetObjectsAsync(conn, "SELECT name, schema_name(schema_id) AS [Schema], 'PROCEDURE' AS Type, create_date, modify_date FROM sys.procedures WHERE is_ms_shipped = 0 ORDER BY name;");
    }

    public async Task<string?> GetProcedureDefinitionAsync(int userId, string procName)
    {
        using var conn = await GetUserConnectionAsync(userId);
        return await GetObjectDefinitionAsync(conn, procName);
    }

    public async Task<IEnumerable<DbObjectDto>> GetFunctionsAsync(int userId)
    {
        using var conn = await GetUserConnectionAsync(userId);
        return await GetObjectsAsync(conn, "SELECT name, schema_name(schema_id) AS [Schema], type_desc AS Type, create_date, modify_date FROM sys.objects WHERE type IN ('FN','IF','TF') AND is_ms_shipped = 0 ORDER BY name;");
    }

    public async Task<string?> GetFunctionDefinitionAsync(int userId, string funcName)
    {
        using var conn = await GetUserConnectionAsync(userId);
        return await GetObjectDefinitionAsync(conn, funcName);
    }

    public async Task<IEnumerable<DbObjectDto>> GetTriggersAsync(int userId)
    {
        using var conn = await GetUserConnectionAsync(userId);
        return await GetObjectsAsync(conn, "SELECT name, schema_name(schema_id) AS [Schema], 'TRIGGER' AS Type, create_date, modify_date FROM sys.triggers WHERE is_ms_shipped = 0 AND parent_class = 1 ORDER BY name;");
    }

    public async Task<string?> GetTriggerDefinitionAsync(int userId, string triggerName)
    {
        using var conn = await GetUserConnectionAsync(userId);
        return await GetObjectDefinitionAsync(conn, triggerName);
    }

    // ─── Private helpers ──────────────────────────────────────────

    private async Task<SqlConnection> GetUserConnectionAsync(int userId)
    {
        var session = await _sessionRepo.GetByUserIdAsync(userId);
        if (session == null)
            throw new InvalidOperationException("Session not found. Please contact the administrator.");

        if (!session.DatabaseCreated || string.IsNullOrEmpty(session.DbLoginPassword))
            throw new InvalidOperationException("Database not created yet. Please create your database first.");

        var hackConfig = await _configRepo.GetActiveConfigAsync();
        if (hackConfig == null)
            throw new InvalidOperationException("Hackathon server is not configured. Please contact the administrator.");

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

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

        string loginName = $"hack_{user.UserID}";
        string connStr = $"Server={hackConfig.ServerName};Database={session.DatabaseName};User Id={loginName};Password={loginPassword};TrustServerCertificate=True;Connection Timeout=15;";

        try
        {
            var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            return conn;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Cannot connect to your database: {ex.Message}");
        }
    }

    private static async Task<IEnumerable<DbObjectDto>> GetObjectsAsync(SqlConnection conn, string query)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = query;

        var objects = new List<DbObjectDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            objects.Add(new DbObjectDto
            {
                Name = reader["name"].ToString()!,
                Schema = reader["Schema"].ToString()!,
                Type = reader["Type"].ToString()!,
                CreateDate = reader["create_date"] != DBNull.Value ? Convert.ToDateTime(reader["create_date"]) : null,
                ModifyDate = reader["modify_date"] != DBNull.Value ? Convert.ToDateTime(reader["modify_date"]) : null
            });
        }

        return objects;
    }

    private static async Task<string?> GetObjectDefinitionAsync(SqlConnection conn, string objectName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT OBJECT_DEFINITION(OBJECT_ID(@Name)) AS Definition;";
        cmd.Parameters.AddWithValue("@Name", objectName);

        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }
}
