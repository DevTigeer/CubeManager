using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class MicePopupRepository : IMicePopupRepository
{
    private readonly Database _db;
    public MicePopupRepository(Database db) => _db = db;

    public async Task<IEnumerable<MicePopup>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<MicePopup>(
            "SELECT id, title, content, interval_minutes, is_active, last_shown_at " +
            "FROM mice_popups ORDER BY id DESC");
    }

    public async Task<IEnumerable<MicePopup>> GetActiveAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<MicePopup>(
            "SELECT id, title, content, interval_minutes, is_active, last_shown_at " +
            "FROM mice_popups WHERE is_active = 1 ORDER BY id");
    }

    public async Task<int> InsertAsync(MicePopup popup)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO mice_popups (title, content, interval_minutes, is_active) " +
            "VALUES (@Title, @Content, @IntervalMinutes, @IsActive); SELECT last_insert_rowid()", popup);
    }

    public async Task UpdateAsync(MicePopup popup)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE mice_popups SET title=@Title, content=@Content, " +
            "interval_minutes=@IntervalMinutes, is_active=@IsActive WHERE id=@Id", popup);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM mice_popups WHERE id=@id", new { id });
    }

    public async Task UpdateLastShownAsync(int id, string dateTime)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE mice_popups SET last_shown_at=@dateTime WHERE id=@id",
            new { id, dateTime });
    }
}
