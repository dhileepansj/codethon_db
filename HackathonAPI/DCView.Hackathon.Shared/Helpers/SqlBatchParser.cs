using System.Text.RegularExpressions;

namespace DCView.Hackathon.Shared.Helpers;

/// <summary>
/// Splits SQL scripts on standalone "GO" lines (SSMS convention).
/// </summary>
public static class SqlBatchParser
{
    private static readonly Regex GoBatchRegex = new(
        @"^\s*GO\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    // ─── Blocked SQL Patterns ─────────────────────────────────────
    // These patterns are dangerous in a hackathon context — they allow
    // participants to discover other databases, drop databases, or
    // alter server-level settings.

    private static readonly (Regex Pattern, string Description)[] BlockedPatterns =
    {
        // Database discovery / escape
        (new Regex(@"\bDB_NAME\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "DB_NAME() is not allowed"),

        (new Regex(@"\bDB_ID\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "DB_ID() is not allowed"),

        (new Regex(@"\bSELECT\b.*\bFROM\b.*\bsys\.databases\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "Querying sys.databases is not allowed"),

        (new Regex(@"\bSELECT\b.*\bFROM\b.*\bsys\.server_principals\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "Querying sys.server_principals is not allowed"),

        (new Regex(@"\bSELECT\b.*\bFROM\b.*\bsys\.syslogins\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "Querying sys.syslogins is not allowed"),

        (new Regex(@"\bSELECT\b.*\bFROM\b.*\bsys\.configurations\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "Querying sys.configurations is not allowed"),

        (new Regex(@"\bSELECT\b.*\bFROM\b.*\bmaster\.", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "Querying the master database is not allowed"),

        // Database-level destructive commands
        (new Regex(@"\bDROP\s+DATABASE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "DROP DATABASE is not allowed"),

        (new Regex(@"\bALTER\s+DATABASE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "ALTER DATABASE is not allowed"),

        (new Regex(@"\bCREATE\s+DATABASE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "CREATE DATABASE is not allowed"),

        // Server-level operations
        (new Regex(@"\bCREATE\s+LOGIN\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "CREATE LOGIN is not allowed"),

        (new Regex(@"\bALTER\s+LOGIN\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "ALTER LOGIN is not allowed"),

        (new Regex(@"\bDROP\s+LOGIN\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "DROP LOGIN is not allowed"),

        (new Regex(@"\bCREATE\s+USER\b.*\bFOR\s+LOGIN\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
            "CREATE USER FOR LOGIN is not allowed"),

        // USE statement (switching databases)
        (new Regex(@"\bUSE\s+\[?(?!tempdb\b)\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "USE [database] is not allowed — you can only work in your assigned database"),

        // Dangerous system procedures
        (new Regex(@"\bEXEC(?:UTE)?\s+(?:sp_configure|xp_cmdshell|xp_regread|xp_regwrite|sp_addlogin|sp_droplogin|sp_grantdbaccess|sp_addsrvrolemember|sp_addrolemember\s+.*\bsysadmin\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "This system procedure is not allowed"),

        (new Regex(@"\bxp_cmdshell\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "xp_cmdshell is not allowed"),

        (new Regex(@"\bsp_configure\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "sp_configure is not allowed"),

        // OPENROWSET / OPENDATASOURCE (remote access)
        (new Regex(@"\bOPENROWSET\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "OPENROWSET is not allowed"),

        (new Regex(@"\bOPENDATASOURCE\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "OPENDATASOURCE is not allowed"),

        // BACKUP / RESTORE
        (new Regex(@"\bBACKUP\s+(DATABASE|LOG)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "BACKUP is not allowed"),

        (new Regex(@"\bRESTORE\s+(DATABASE|LOG)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "RESTORE is not allowed"),

        // Shutdown
        (new Regex(@"\bSHUTDOWN\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "SHUTDOWN is not allowed"),

        // KILL (terminate other sessions)
        (new Regex(@"\bKILL\s+\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "KILL is not allowed"),

        // Linked servers
        (new Regex(@"\bsp_addlinkedserver\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "Linked server operations are not allowed"),

        (new Regex(@"\bOPENQUERY\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "OPENQUERY is not allowed"),
    };

    /// <summary>
    /// Validates a SQL batch against blocked patterns.
    /// Returns null if safe, or an error message describing the violation.
    /// </summary>
    public static string? ValidateBatchSafety(string batch)
    {
        if (string.IsNullOrWhiteSpace(batch))
            return null;

        // Strip single-line comments for pattern matching (avoid false positives in comments)
        // But keep the original for actual execution
        var normalized = StripSqlComments(batch);

        foreach (var (pattern, description) in BlockedPatterns)
        {
            if (pattern.IsMatch(normalized))
                return description;
        }

        return null;
    }

    /// <summary>
    /// Strips SQL comments to avoid false positives in guardrail checks.
    /// Removes -- line comments and /* */ block comments.
    /// </summary>
    private static string StripSqlComments(string sql)
    {
        // Remove block comments
        var result = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        // Remove line comments
        result = Regex.Replace(result, @"--[^\r\n]*", " ");
        return result;
    }

    /// <summary>
    /// Splits SQL text on standalone "GO" lines.
    /// Returns non-empty batches only.
    /// </summary>
    public static List<string> SplitBatches(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return new List<string>();

        var batches = GoBatchRegex.Split(sql)
            .Select(b => b.Trim())
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .ToList();

        return batches;
    }

    /// <summary>
    /// Detects whether a batch is likely a SELECT statement (returns rows).
    /// </summary>
    public static bool IsSelectBatch(string batch)
    {
        var trimmed = batch.TrimStart();

        // Strip leading comments (-- and /* */)
        while (trimmed.StartsWith("--"))
        {
            int newLine = trimmed.IndexOf('\n');
            if (newLine < 0) return false;
            trimmed = trimmed[(newLine + 1)..].TrimStart();
        }

        while (trimmed.StartsWith("/*"))
        {
            int end = trimmed.IndexOf("*/");
            if (end < 0) return false;
            trimmed = trimmed[(end + 2)..].TrimStart();
        }

        var upper = trimmed.ToUpperInvariant();
        return upper.StartsWith("SELECT")
            || upper.StartsWith("WITH")
            || upper.StartsWith("EXEC")
            || upper.StartsWith("EXECUTE")
            || upper.StartsWith("SP_")
            || upper.StartsWith("XP_");
    }

    /// <summary>
    /// Detects the query type keyword for logging.
    /// </summary>
    public static string DetectQueryType(string batch)
    {
        var trimmed = batch.TrimStart();

        // Strip leading comments
        while (trimmed.StartsWith("--"))
        {
            int newLine = trimmed.IndexOf('\n');
            if (newLine < 0) return "UNKNOWN";
            trimmed = trimmed[(newLine + 1)..].TrimStart();
        }

        while (trimmed.StartsWith("/*"))
        {
            int end = trimmed.IndexOf("*/");
            if (end < 0) return "UNKNOWN";
            trimmed = trimmed[(end + 2)..].TrimStart();
        }

        var upper = trimmed.ToUpperInvariant();

        if (upper.StartsWith("SELECT") || upper.StartsWith("WITH")) return "SELECT";
        if (upper.StartsWith("INSERT")) return "INSERT";
        if (upper.StartsWith("UPDATE")) return "UPDATE";
        if (upper.StartsWith("DELETE")) return "DELETE";
        if (upper.StartsWith("CREATE")) return "CREATE";
        if (upper.StartsWith("ALTER")) return "ALTER";
        if (upper.StartsWith("DROP")) return "DROP";
        if (upper.StartsWith("EXEC") || upper.StartsWith("EXECUTE")) return "EXEC";
        if (upper.StartsWith("TRUNCATE")) return "TRUNCATE";

        return "OTHER";
    }
}
