using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;
using DCView.Hackathon.Application.DTOs.Hackathon;
using DCView.Hackathon.Application.DTOs.Schema;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Application.Services.Engines;

/// <summary>
/// Oracle implementation of the database engine abstraction.
/// Creates a schema (user) per participant within a shared Oracle instance.
/// </summary>
public class OracleEngine : IDbEngine
{
    // Oracle batch separator: standalone "/" on a line (like SQL*Plus)
    private static readonly Regex BatchSeparator = new(
        @"^\s*/\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // ─── Blocked Oracle Patterns ─────────────────────────────────
    private static readonly (Regex Pattern, string Description)[] BlockedPatterns =
    {
        (new Regex(@"\bDROP\s+USER\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "DROP USER is not allowed"),

        (new Regex(@"\bALTER\s+USER\b(?!.*\bIDENTIFIED\s+BY\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "ALTER USER is not allowed (except changing own password)"),

        (new Regex(@"\bCREATE\s+USER\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "CREATE USER is not allowed"),

        (new Regex(@"\bALTER\s+SYSTEM\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "ALTER SYSTEM is not allowed"),

        (new Regex(@"\bALTER\s+DATABASE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "ALTER DATABASE is not allowed"),

        (new Regex(@"\bDROP\s+DATABASE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "DROP DATABASE is not allowed"),

        (new Regex(@"\bDROP\s+TABLESPACE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "DROP TABLESPACE is not allowed"),

        (new Regex(@"\bCREATE\s+DATABASE\s+LINK\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "CREATE DATABASE LINK is not allowed"),

        (new Regex(@"\bGRANT\b.*\bTO\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "GRANT is not allowed"),

        (new Regex(@"\bREVOKE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "REVOKE is not allowed"),

        (new Regex(@"\bEXECUTE\s+IMMEDIATE\b.*\bALTER\s+SYSTEM\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "Dynamic ALTER SYSTEM is not allowed"),

        (new Regex(@"\bDBMS_SCHEDULER\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "DBMS_SCHEDULER is not allowed"),

        (new Regex(@"\bUTL_FILE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "UTL_FILE is not allowed"),

        (new Regex(@"\bUTL_HTTP\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "UTL_HTTP is not allowed"),

        (new Regex(@"\bUTL_TCP\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "UTL_TCP is not allowed"),

        (new Regex(@"\bDBMS_JAVA\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "DBMS_JAVA is not allowed"),

        (new Regex(@"\bSELECT\b.*\bFROM\b.*\bDBA_", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "Querying DBA_ views is not allowed"),

        (new Regex(@"\bSELECT\b.*\bFROM\b.*\bV\$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "Querying V$ dynamic performance views is not allowed"),

        (new Regex(@"\bSHUTDOWN\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "SHUTDOWN is not allowed"),

        (new Regex(@"\bSTARTUP\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "STARTUP is not allowed"),

        (new Regex(@"\bALTER\s+SESSION\s+SET\s+CURRENT_SCHEMA\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "Switching schema is not allowed"),
    };

    public async Task<DbCreationResult> CreateParticipantDatabaseAsync(
        HackathonConfig config,
        string userId,
        string adminPassword,
        string encryptionKey)
    {
        // In Oracle, we create a user/schema per participant (not a database)
        string schemaName = $"{config.DbPrefix}{userId}".ToUpper();
        string loginPassword = GenerateRandomPassword();

        string adminConnStr = BuildAdminConnectionString(config, adminPassword);

        using var conn = new OracleConnection(adminConnStr);
        await conn.OpenAsync();

        // Create user (schema)
        using var createUserCmd = conn.CreateCommand();
        createUserCmd.CommandText = $"CREATE USER \"{schemaName}\" IDENTIFIED BY \"{loginPassword}\" DEFAULT TABLESPACE USERS TEMPORARY TABLESPACE TEMP QUOTA UNLIMITED ON USERS";
        createUserCmd.CommandTimeout = 60;
        await createUserCmd.ExecuteNonQueryAsync();

        // Grant privileges
        using var grantCmd = conn.CreateCommand();
        grantCmd.CommandText = $@"
            GRANT CONNECT, RESOURCE TO ""{schemaName}"" .
            GRANT CREATE VIEW TO ""{schemaName}"" .
            GRANT CREATE PROCEDURE TO ""{schemaName}"" .
            GRANT CREATE TRIGGER TO ""{schemaName}"" .
            GRANT CREATE SEQUENCE TO ""{schemaName}""";

        // Execute grants individually (Oracle doesn't support multi-statement in one command)
        var grants = new[]
        {
            $"GRANT CONNECT, RESOURCE TO \"{schemaName}\"",
            $"GRANT CREATE VIEW TO \"{schemaName}\"",
            $"GRANT CREATE PROCEDURE TO \"{schemaName}\"",
            $"GRANT CREATE TRIGGER TO \"{schemaName}\"",
            $"GRANT CREATE SEQUENCE TO \"{schemaName}\""
        };

        foreach (var grant in grants)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = grant;
            await cmd.ExecuteNonQueryAsync();
        }

        return new DbCreationResult
        {
            DatabaseName = schemaName,
            LoginName = schemaName,
            LoginPassword = loginPassword
        };
    }

    public async Task ExecuteScaffoldScriptsAsync(
        HackathonConfig config,
        string databaseName,
        string adminPassword,
        IEnumerable<ScaffoldScript> scripts)
    {
        // Connect as admin but set current_schema to participant's schema
        string adminConnStr = BuildAdminConnectionString(config, adminPassword);

        using var conn = new OracleConnection(adminConnStr);
        await conn.OpenAsync();

        // Switch to participant's schema
        using var schemaCmd = conn.CreateCommand();
        schemaCmd.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = \"{databaseName}\"";
        await schemaCmd.ExecuteNonQueryAsync();

        foreach (var script in scripts)
        {
            try
            {
                var batches = SplitBatches(script.SqlContent);
                foreach (var batch in batches)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = batch;
                    cmd.CommandTimeout = 60;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Best-effort: continue with next script if one fails
            }
        }
    }

    public async Task<ExecuteResultDto> ExecuteAsync(
        HackathonConfig config,
        HackathonSession session,
        string userId,
        string loginPassword,
        ExecuteRequestDto request)
    {
        string connStr = BuildParticipantConnectionString(config, session, loginPassword);

        var batches = SplitBatches(request.Sql);
        var result = new ExecuteResultDto
        {
            TotalBatches = batches.Count,
            ExecutedBatches = 0
        };

        using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];

            // Oracle guardrail check
            var violation = ValidateBatchSafety(batch);
            if (violation != null)
            {
                result.Results.Add(new BatchResultDto
                {
                    BatchIndex = i,
                    Type = "ERROR",
                    Error = $"Blocked: {violation}"
                });
                result.ExecutedBatches++;
                break;
            }

            var queryType = DetectQueryType(batch);
            var sw = Stopwatch.StartNew();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = batch;
                cmd.CommandTimeout = config.MaxQueryTimeoutSeconds;

                if (IsSelectBatch(batch))
                {
                    var batchResult = await ExecuteSelectBatchAsync(cmd, request.Page, request.PageSize);
                    sw.Stop();
                    batchResult.BatchIndex = i;
                    batchResult.DurationMs = (int)sw.ElapsedMilliseconds;
                    result.Results.Add(batchResult);
                }
                else
                {
                    int affected = await cmd.ExecuteNonQueryAsync();
                    sw.Stop();
                    result.Results.Add(new BatchResultDto
                    {
                        BatchIndex = i,
                        Type = queryType is "INSERT" or "UPDATE" or "DELETE" or "MERGE" ? "DML" : "DDL",
                        Message = affected >= 0 ? $"{affected} row(s) affected" : "Command completed successfully",
                        RowsAffected = affected >= 0 ? affected : null,
                        DurationMs = (int)sw.ElapsedMilliseconds
                    });
                }

                result.ExecutedBatches++;
            }
            catch (OracleException ex) when (ex.Number == 1013) // ORA-01013: user requested cancel
            {
                sw.Stop();
                result.Results.Add(new BatchResultDto
                {
                    BatchIndex = i,
                    Type = "ERROR",
                    Error = "Query timed out (time limit exceeded)",
                    DurationMs = (int)sw.ElapsedMilliseconds
                });
                result.ExecutedBatches++;
                break;
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.Results.Add(new BatchResultDto
                {
                    BatchIndex = i,
                    Type = "ERROR",
                    Error = ex.Message,
                    DurationMs = (int)sw.ElapsedMilliseconds
                });
                result.ExecutedBatches++;
                break;
            }
        }

        return result;
    }

    public string BuildParticipantConnectionString(HackathonConfig config, HackathonSession session, string loginPassword)
    {
        int port = config.Port ?? 1521;
        string serviceName = config.OracleServiceName ?? "XEPDB1";
        // session.DatabaseName is the schema/user name in Oracle
        return $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={config.ServerName})(PORT={port}))(CONNECT_DATA=(SERVICE_NAME={serviceName})));User Id=\"{session.DatabaseName}\";Password=\"{loginPassword}\";Connection Timeout={config.MaxQueryTimeoutSeconds};";
    }

    public string? ValidateBatchSafety(string batch)
    {
        if (string.IsNullOrWhiteSpace(batch))
            return null;

        var normalized = StripOracleComments(batch);

        foreach (var (pattern, description) in BlockedPatterns)
        {
            if (pattern.IsMatch(normalized))
                return description;
        }

        return null;
    }

    public List<string> SplitBatches(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return new List<string>();

        // Split on standalone "/" lines (Oracle SQL*Plus convention)
        // Also treat ";" at end of PL/SQL blocks followed by "/" as single batches
        var batches = BatchSeparator.Split(sql)
            .Select(b => b.Trim().TrimEnd(';').Trim())
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .ToList();

        // If no "/" separators found, treat the whole thing as a single batch
        // (splitting on ";" for simple statements)
        if (batches.Count <= 1 && !string.IsNullOrWhiteSpace(sql))
        {
            // For simple SQL (no PL/SQL blocks), split on semicolons
            var trimmedSql = sql.Trim();
            if (!ContainsPlSqlBlock(trimmedSql))
            {
                batches = trimmedSql.Split(';')
                    .Select(b => b.Trim())
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .ToList();
            }
            else
            {
                batches = new List<string> { trimmedSql.TrimEnd(';').Trim() };
            }
        }

        return batches;
    }

    // ─── Schema Explorer ─────────────────────────────────────────

    public async Task<DatabaseOverviewDto> GetOverviewAsync(string connectionString)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();

        var overview = new DatabaseOverviewDto { DatabaseName = conn.Database ?? "Oracle" };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                (SELECT COUNT(*) FROM USER_TABLES) AS TableCount,
                (SELECT COUNT(*) FROM USER_VIEWS) AS ViewCount,
                (SELECT COUNT(*) FROM USER_PROCEDURES WHERE OBJECT_TYPE = 'PROCEDURE') AS ProcCount,
                (SELECT COUNT(*) FROM USER_PROCEDURES WHERE OBJECT_TYPE = 'FUNCTION') AS FuncCount,
                (SELECT COUNT(*) FROM USER_TRIGGERS) AS TriggerCount
            FROM DUAL";

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            overview.TableCount = Convert.ToInt32(reader["TableCount"]);
            overview.ViewCount = Convert.ToInt32(reader["ViewCount"]);
            overview.ProcedureCount = Convert.ToInt32(reader["ProcCount"]);
            overview.FunctionCount = Convert.ToInt32(reader["FuncCount"]);
            overview.TriggerCount = Convert.ToInt32(reader["TriggerCount"]);
        }
        reader.Close();

        // Get schema size
        using var sizeCmd = conn.CreateCommand();
        sizeCmd.CommandText = "SELECT NVL(SUM(BYTES)/1024/1024, 0) AS SizeMB FROM USER_SEGMENTS";
        var sizeResult = await sizeCmd.ExecuteScalarAsync();
        if (sizeResult != null && sizeResult != DBNull.Value)
            overview.SizeMB = Convert.ToDecimal(sizeResult);

        return overview;
    }

    public async Task<IEnumerable<TableInfoDto>> GetTablesAsync(string connectionString)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                t.TABLE_NAME AS TableName,
                (SELECT COUNT(*) FROM USER_TAB_COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME) AS ColumnCount,
                NVL(t.NUM_ROWS, 0) AS RowCount,
                o.CREATED AS CreateDate
            FROM USER_TABLES t
            LEFT JOIN USER_OBJECTS o ON o.OBJECT_NAME = t.TABLE_NAME AND o.OBJECT_TYPE = 'TABLE'
            ORDER BY t.TABLE_NAME";

        var tables = new List<TableInfoDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(new TableInfoDto
            {
                Schema = GetOracleUserFromConnection(conn),
                TableName = reader["TableName"].ToString()!,
                ColumnCount = Convert.ToInt32(reader["ColumnCount"]),
                RowCount = Convert.ToInt64(reader["RowCount"]),
                CreateDate = reader["CreateDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreateDate"]) : null
            });
        }

        return tables;
    }

    public async Task<IEnumerable<ColumnInfoDto>> GetTableColumnsAsync(string connectionString, string tableName)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE ||
                    CASE
                        WHEN c.DATA_TYPE IN ('VARCHAR2','NVARCHAR2','CHAR','NCHAR','RAW') THEN '(' || c.DATA_LENGTH || ')'
                        WHEN c.DATA_TYPE = 'NUMBER' AND c.DATA_PRECISION IS NOT NULL THEN '(' || c.DATA_PRECISION || ',' || NVL(c.DATA_SCALE,0) || ')'
                        ELSE ''
                    END AS DataType,
                c.DATA_LENGTH AS MaxLength,
                CASE WHEN c.NULLABLE = 'Y' THEN 1 ELSE 0 END AS IsNullable,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
                0 AS IsIdentity,
                CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsForeignKey,
                fk.R_TABLE_NAME AS ForeignKeyTable,
                c.DATA_DEFAULT AS DefaultValue,
                c.COLUMN_ID AS OrdinalPosition
            FROM USER_TAB_COLUMNS c
            LEFT JOIN (
                SELECT cc.COLUMN_NAME
                FROM USER_CONSTRAINTS uc
                JOIN USER_CONS_COLUMNS cc ON uc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
                WHERE uc.TABLE_NAME = :tableName AND uc.CONSTRAINT_TYPE = 'P'
            ) pk ON pk.COLUMN_NAME = c.COLUMN_NAME
            LEFT JOIN (
                SELECT cc.COLUMN_NAME, rcc.TABLE_NAME AS R_TABLE_NAME
                FROM USER_CONSTRAINTS uc
                JOIN USER_CONS_COLUMNS cc ON uc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
                JOIN USER_CONSTRAINTS ruc ON uc.R_CONSTRAINT_NAME = ruc.CONSTRAINT_NAME
                JOIN USER_CONS_COLUMNS rcc ON ruc.CONSTRAINT_NAME = rcc.CONSTRAINT_NAME
                WHERE uc.TABLE_NAME = :tableName AND uc.CONSTRAINT_TYPE = 'R'
            ) fk ON fk.COLUMN_NAME = c.COLUMN_NAME
            WHERE c.TABLE_NAME = :tableName
            ORDER BY c.COLUMN_ID";
        cmd.Parameters.Add(new OracleParameter("tableName", tableName.ToUpper()));

        var columns = new List<ColumnInfoDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnInfoDto
            {
                ColumnName = reader["COLUMN_NAME"].ToString()!,
                DataType = reader["DataType"].ToString()!,
                MaxLength = reader["MaxLength"] != DBNull.Value ? Convert.ToInt32(reader["MaxLength"]) : null,
                IsNullable = Convert.ToInt32(reader["IsNullable"]) == 1,
                IsPrimaryKey = Convert.ToInt32(reader["IsPrimaryKey"]) == 1,
                IsIdentity = false,
                IsForeignKey = Convert.ToInt32(reader["IsForeignKey"]) == 1,
                ForeignKeyTable = reader["ForeignKeyTable"]?.ToString(),
                DefaultValue = reader["DefaultValue"]?.ToString()?.Trim(),
                OrdinalPosition = Convert.ToInt32(reader["OrdinalPosition"])
            });
        }

        return columns;
    }

    public async Task<IEnumerable<IndexInfoDto>> GetTableIndexesAsync(string connectionString, string tableName)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                i.INDEX_NAME AS IndexName,
                i.INDEX_TYPE AS IndexType,
                CASE WHEN i.UNIQUENESS = 'UNIQUE' THEN 1 ELSE 0 END AS IsUnique,
                CASE WHEN uc.CONSTRAINT_TYPE = 'P' THEN 1 ELSE 0 END AS IsPrimaryKey,
                LISTAGG(ic.COLUMN_NAME, ', ') WITHIN GROUP (ORDER BY ic.COLUMN_POSITION) AS Columns
            FROM USER_INDEXES i
            JOIN USER_IND_COLUMNS ic ON i.INDEX_NAME = ic.INDEX_NAME
            LEFT JOIN USER_CONSTRAINTS uc ON uc.INDEX_NAME = i.INDEX_NAME AND uc.CONSTRAINT_TYPE = 'P'
            WHERE i.TABLE_NAME = :tableName
            GROUP BY i.INDEX_NAME, i.INDEX_TYPE, i.UNIQUENESS, uc.CONSTRAINT_TYPE
            ORDER BY CASE WHEN uc.CONSTRAINT_TYPE = 'P' THEN 0 ELSE 1 END, i.INDEX_NAME";
        cmd.Parameters.Add(new OracleParameter("tableName", tableName.ToUpper()));

        var indexes = new List<IndexInfoDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(new IndexInfoDto
            {
                IndexName = reader["IndexName"].ToString()!,
                IndexType = reader["IndexType"].ToString()!,
                IsUnique = Convert.ToInt32(reader["IsUnique"]) == 1,
                IsPrimaryKey = Convert.ToInt32(reader["IsPrimaryKey"]) == 1,
                Columns = reader["Columns"].ToString()!
            });
        }

        return indexes;
    }

    public async Task<TableDataDto> GetTableDataAsync(string connectionString, string tableName, int page, int pageSize)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();

        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName.ToUpper()}\"";
        var totalRows = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM \"{tableName.ToUpper()}\" OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY";
        cmd.Parameters.Add(new OracleParameter("offset", (page - 1) * pageSize));
        cmd.Parameters.Add(new OracleParameter("pageSize", pageSize));

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

    public async Task<IEnumerable<DbObjectDto>> GetViewsAsync(string connectionString)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();
        return await GetObjectsAsync(conn, @"
            SELECT OBJECT_NAME AS Name, 'VIEW' AS Type, CREATED AS CreateDate, LAST_DDL_TIME AS ModifyDate
            FROM USER_OBJECTS WHERE OBJECT_TYPE = 'VIEW' ORDER BY OBJECT_NAME");
    }

    public async Task<string?> GetViewDefinitionAsync(string connectionString, string viewName)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TEXT FROM USER_VIEWS WHERE VIEW_NAME = :name";
        cmd.Parameters.Add(new OracleParameter("name", viewName.ToUpper()));
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }

    public async Task<IEnumerable<DbObjectDto>> GetProceduresAsync(string connectionString)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();
        return await GetObjectsAsync(conn, @"
            SELECT OBJECT_NAME AS Name, 'PROCEDURE' AS Type, CREATED AS CreateDate, LAST_DDL_TIME AS ModifyDate
            FROM USER_OBJECTS WHERE OBJECT_TYPE = 'PROCEDURE' ORDER BY OBJECT_NAME");
    }

    public async Task<string?> GetProcedureDefinitionAsync(string connectionString, string procName)
    {
        return await GetSourceDefinitionAsync(connectionString, procName, "PROCEDURE");
    }

    public async Task<IEnumerable<DbObjectDto>> GetFunctionsAsync(string connectionString)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();
        return await GetObjectsAsync(conn, @"
            SELECT OBJECT_NAME AS Name, 'FUNCTION' AS Type, CREATED AS CreateDate, LAST_DDL_TIME AS ModifyDate
            FROM USER_OBJECTS WHERE OBJECT_TYPE = 'FUNCTION' ORDER BY OBJECT_NAME");
    }

    public async Task<string?> GetFunctionDefinitionAsync(string connectionString, string funcName)
    {
        return await GetSourceDefinitionAsync(connectionString, funcName, "FUNCTION");
    }

    public async Task<IEnumerable<DbObjectDto>> GetTriggersAsync(string connectionString)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();
        return await GetObjectsAsync(conn, @"
            SELECT OBJECT_NAME AS Name, 'TRIGGER' AS Type, CREATED AS CreateDate, LAST_DDL_TIME AS ModifyDate
            FROM USER_OBJECTS WHERE OBJECT_TYPE = 'TRIGGER' ORDER BY OBJECT_NAME");
    }

    public async Task<string?> GetTriggerDefinitionAsync(string connectionString, string triggerName)
    {
        return await GetSourceDefinitionAsync(connectionString, triggerName, "TRIGGER");
    }

    // ─── Private helpers ──────────────────────────────────────────

    private string BuildAdminConnectionString(HackathonConfig config, string adminPassword)
    {
        int port = config.Port ?? 1521;
        string serviceName = config.OracleServiceName ?? "XEPDB1";
        return $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={config.ServerName})(PORT={port}))(CONNECT_DATA=(SERVICE_NAME={serviceName})));User Id=\"{config.AdminUserId}\";Password=\"{adminPassword}\";Connection Timeout=30;";
    }

    private static async Task<BatchResultDto> ExecuteSelectBatchAsync(OracleCommand cmd, int page, int pageSize)
    {
        using var reader = await cmd.ExecuteReaderAsync();

        var columns = new List<string>();
        for (int c = 0; c < reader.FieldCount; c++)
            columns.Add(reader.GetName(c));

        var allRows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int c = 0; c < reader.FieldCount; c++)
                row[columns[c]] = reader.IsDBNull(c) ? null : reader.GetValue(c);
            allRows.Add(row);
        }

        int totalRows = allRows.Count;
        var pagedRows = allRows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new BatchResultDto
        {
            Type = "SELECT",
            Columns = columns,
            Rows = pagedRows,
            TotalRows = totalRows,
            Page = page,
            PageSize = pageSize,
            Message = $"{totalRows} row(s) returned"
        };
    }

    private static async Task<IEnumerable<DbObjectDto>> GetObjectsAsync(OracleConnection conn, string query)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = query;

        var objects = new List<DbObjectDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            objects.Add(new DbObjectDto
            {
                Name = reader["Name"].ToString()!,
                Schema = GetOracleUserFromConnection(conn),
                Type = reader["Type"].ToString()!,
                CreateDate = reader["CreateDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreateDate"]) : null,
                ModifyDate = reader["ModifyDate"] != DBNull.Value ? Convert.ToDateTime(reader["ModifyDate"]) : null
            });
        }

        return objects;
    }

    private static async Task<string?> GetSourceDefinitionAsync(string connectionString, string objectName, string objectType)
    {
        using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT TEXT FROM USER_SOURCE
            WHERE NAME = :name AND TYPE = :type
            ORDER BY LINE";
        cmd.Parameters.Add(new OracleParameter("name", objectName.ToUpper()));
        cmd.Parameters.Add(new OracleParameter("type", objectType.ToUpper()));

        var lines = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lines.Add(reader["TEXT"]?.ToString() ?? "");
        }

        return lines.Count > 0 ? string.Join("", lines) : null;
    }

    private static bool IsSelectBatch(string batch)
    {
        var trimmed = StripOracleComments(batch).TrimStart();
        var upper = trimmed.ToUpperInvariant();
        return upper.StartsWith("SELECT") || upper.StartsWith("WITH");
    }

    private static string DetectQueryType(string batch)
    {
        var trimmed = StripOracleComments(batch).TrimStart();
        var upper = trimmed.ToUpperInvariant();

        if (upper.StartsWith("SELECT") || upper.StartsWith("WITH")) return "SELECT";
        if (upper.StartsWith("INSERT")) return "INSERT";
        if (upper.StartsWith("UPDATE")) return "UPDATE";
        if (upper.StartsWith("DELETE")) return "DELETE";
        if (upper.StartsWith("MERGE")) return "MERGE";
        if (upper.StartsWith("CREATE")) return "CREATE";
        if (upper.StartsWith("ALTER")) return "ALTER";
        if (upper.StartsWith("DROP")) return "DROP";
        if (upper.StartsWith("TRUNCATE")) return "TRUNCATE";
        if (upper.StartsWith("BEGIN") || upper.StartsWith("DECLARE")) return "PLSQL";

        return "OTHER";
    }

    private static bool ContainsPlSqlBlock(string sql)
    {
        var upper = sql.ToUpperInvariant();
        return upper.Contains("BEGIN") || upper.Contains("DECLARE")
            || Regex.IsMatch(upper, @"\bCREATE\s+(OR\s+REPLACE\s+)?(PROCEDURE|FUNCTION|TRIGGER|PACKAGE)\b");
    }

    private static string StripOracleComments(string sql)
    {
        var result = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        result = Regex.Replace(result, @"--[^\r\n]*", " ");
        return result;
    }

    private static string GetOracleUserFromConnection(OracleConnection conn)
    {
        // Extract user from DataSource info or use a query
        try
        {
            // OracleConnection doesn't expose UserID property directly,
            // but the connection info is available through the connection string parsing
            var builder = new OracleConnectionStringBuilder(conn.ConnectionString);
            return builder.UserID?.ToUpper() ?? "USER";
        }
        catch
        {
            return "USER";
        }
    }

    private static string GenerateRandomPassword()
    {
        // Oracle passwords must start with a letter
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#$_";
        var random = new Random();
        return letters[random.Next(letters.Length)]
            + new string(Enumerable.Range(0, 19).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
