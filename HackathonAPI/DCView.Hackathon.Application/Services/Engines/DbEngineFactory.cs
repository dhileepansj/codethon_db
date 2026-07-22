using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Application.Services.Engines;

/// <summary>
/// Resolves the correct IDbEngine implementation based on the configured engine type.
/// </summary>
public class DbEngineFactory : IDbEngineFactory
{
    private readonly SqlServerEngine _sqlServer;
    private readonly OracleEngine _oracle;

    public DbEngineFactory(SqlServerEngine sqlServer, OracleEngine oracle)
    {
        _sqlServer = sqlServer;
        _oracle = oracle;
    }

    public IDbEngine GetEngine(DbEngineType engineType)
    {
        return engineType switch
        {
            DbEngineType.SqlServer => _sqlServer,
            DbEngineType.Oracle => _oracle,
            _ => throw new NotSupportedException($"Database engine '{engineType}' is not supported.")
        };
    }
}
