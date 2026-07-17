using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class ScaffoldScriptRepository : IScaffoldScriptRepository
{
    private readonly HackathonDbContext _db;

    public ScaffoldScriptRepository(HackathonDbContext db) => _db = db;

    public async Task<IEnumerable<ScaffoldScript>> GetAllActiveAsync()
        => await _db.ScaffoldScripts
            .Where(s => s.IsActive)
            .OrderBy(s => s.ExecutionOrder)
            .ToListAsync();

    public async Task<IEnumerable<ScaffoldScript>> GetAllAsync()
        => await _db.ScaffoldScripts
            .OrderBy(s => s.ExecutionOrder)
            .ToListAsync();

    public async Task<ScaffoldScript?> GetByIdAsync(int id)
        => await _db.ScaffoldScripts.FindAsync(id);

    public async Task<ScaffoldScript> CreateAsync(ScaffoldScript script)
    {
        _db.ScaffoldScripts.Add(script);
        await _db.SaveChangesAsync();
        return script;
    }

    public async Task UpdateAsync(ScaffoldScript script)
    {
        _db.ScaffoldScripts.Update(script);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var script = await _db.ScaffoldScripts.FindAsync(id);
        if (script != null)
        {
            _db.ScaffoldScripts.Remove(script);
            await _db.SaveChangesAsync();
        }
    }
}
