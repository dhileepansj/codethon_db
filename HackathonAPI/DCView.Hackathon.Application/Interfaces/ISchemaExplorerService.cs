using DCView.Hackathon.Application.DTOs.Schema;

namespace DCView.Hackathon.Application.Interfaces;

public interface ISchemaExplorerService
{
    Task<DatabaseOverviewDto> GetOverviewAsync(int userId);
    Task<IEnumerable<TableInfoDto>> GetTablesAsync(int userId);
    Task<IEnumerable<ColumnInfoDto>> GetTableColumnsAsync(int userId, string tableName);
    Task<IEnumerable<IndexInfoDto>> GetTableIndexesAsync(int userId, string tableName);
    Task<TableDataDto> GetTableDataAsync(int userId, string tableName, int page, int pageSize);
    Task<IEnumerable<DbObjectDto>> GetViewsAsync(int userId);
    Task<string?> GetViewDefinitionAsync(int userId, string viewName);
    Task<IEnumerable<DbObjectDto>> GetProceduresAsync(int userId);
    Task<string?> GetProcedureDefinitionAsync(int userId, string procName);
    Task<IEnumerable<DbObjectDto>> GetFunctionsAsync(int userId);
    Task<string?> GetFunctionDefinitionAsync(int userId, string funcName);
    Task<IEnumerable<DbObjectDto>> GetTriggersAsync(int userId);
    Task<string?> GetTriggerDefinitionAsync(int userId, string triggerName);
}
