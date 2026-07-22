using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Application.Interfaces;

/// <summary>
/// Factory that resolves the correct IDbEngine based on the configured engine type.
/// </summary>
public interface IDbEngineFactory
{
    IDbEngine GetEngine(DbEngineType engineType);
}
