using CubeManager.Core.Interfaces.Repositories;
using Dapper;

namespace CubeManager.Data.Repositories;

public class ConfigRepository : IConfigRepository
{
    private readonly Database _db;

    public ConfigRepository(Database db)
    {
        _db = db;
    }

    public async Task<string?> GetAsync(string key)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<string?>(
            "SELECT value FROM app_config WHERE key = @key",
            new { key });
    }

    public async Task<int> GetIntAsync(string key, int defaultValue)
    {
        var value = await GetAsync(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task SetAsync(string key, string value)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO app_config (key, value, updated_at)
            VALUES (@key, @value, datetime('now', 'localtime'))
            ON CONFLICT(key) DO UPDATE SET
                value = @value,
                updated_at = datetime('now', 'localtime')
            """,
            new { key, value });
    }
}
