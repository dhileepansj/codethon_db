namespace DCView.Hackathon.Application.DTOs.Schema;

public class DatabaseOverviewDto
{
    public string DatabaseName { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public int ViewCount { get; set; }
    public int ProcedureCount { get; set; }
    public int FunctionCount { get; set; }
    public int TriggerCount { get; set; }
    public decimal? SizeMB { get; set; }
}

public class TableInfoDto
{
    public string TableName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public int ColumnCount { get; set; }
    public long RowCount { get; set; }
    public DateTime? CreateDate { get; set; }
}

public class ColumnInfoDto
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsForeignKey { get; set; }
    public string? ForeignKeyTable { get; set; }
    public string? DefaultValue { get; set; }
    public int OrdinalPosition { get; set; }
}

public class IndexInfoDto
{
    public string IndexName { get; set; } = string.Empty;
    public string IndexType { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string Columns { get; set; } = string.Empty;
}

public class DbObjectDto
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public string Type { get; set; } = string.Empty;
    public DateTime? CreateDate { get; set; }
    public DateTime? ModifyDate { get; set; }
}

public class TableDataDto
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
