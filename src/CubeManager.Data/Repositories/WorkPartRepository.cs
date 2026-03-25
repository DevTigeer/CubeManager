using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class WorkPartRepository : IWorkPartRepository
{
    private readonly Database _db;
    public WorkPartRepository(Database db) => _db = db;

    public async Task<IEnumerable<WorkPart>> GetActiveAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<WorkPart>(
            "SELECT id AS Id, part_name AS PartName, start_time AS StartTime, end_time AS EndTime, " +
            "sort_order AS SortOrder, is_active AS IsActive " +
            "FROM work_parts WHERE is_active = 1 ORDER BY sort_order");
    }

    public async Task<IEnumerable<WorkPart>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<WorkPart>(
            "SELECT id AS Id, part_name AS PartName, start_time AS StartTime, end_time AS EndTime, " +
            "sort_order AS SortOrder, is_active AS IsActive " +
            "FROM work_parts ORDER BY sort_order");
    }

    public async Task<int> InsertAsync(WorkPart part)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO work_parts (part_name, start_time, end_time, sort_order) " +
            "VALUES (@PartName, @StartTime, @EndTime, @SortOrder); SELECT last_insert_rowid()", part);
    }

    public async Task UpdateAsync(WorkPart part)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE work_parts SET part_name = @PartName, start_time = @StartTime, " +
            "end_time = @EndTime, sort_order = @SortOrder, is_active = @IsActive WHERE id = @Id", part);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM work_parts WHERE id = @id", new { id });
    }
}
