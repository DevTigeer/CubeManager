using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly Database _db;
    public InventoryRepository(Database db) => _db = db;

    public async Task<IEnumerable<InventoryItem>> GetAllAsync(string? category = null)
    {
        using var conn = _db.CreateConnection();
        var where = string.IsNullOrEmpty(category) ? "" : "WHERE category = @category";
        return await conn.QueryAsync<InventoryItem>(
            $"SELECT id, item_name, required_qty, current_qty, category, note, updated_at " +
            $"FROM inventory {where} ORDER BY item_name",
            new { category });
    }

    public async Task<int> InsertAsync(InventoryItem item)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO inventory (item_name, required_qty, current_qty, category, note) " +
            "VALUES (@ItemName, @RequiredQty, @CurrentQty, @Category, @Note); SELECT last_insert_rowid()", item);
    }

    public async Task<bool> UpdateQuantityAsync(int id, int currentQty)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE inventory SET current_qty = @currentQty, updated_at = datetime('now','localtime') WHERE id = @id",
            new { id, currentQty }) > 0;
    }

    public async Task<bool> UpdateAsync(InventoryItem item)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE inventory SET item_name = @ItemName, required_qty = @RequiredQty, " +
            "current_qty = @CurrentQty, category = @Category, note = @Note, " +
            "updated_at = datetime('now','localtime') WHERE id = @Id", item) > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM inventory WHERE id = @id", new { id }) > 0;
    }
}
