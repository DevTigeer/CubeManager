using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class FreePassRepository : IFreePassRepository
{
    private readonly Database _db;

    public FreePassRepository(Database db) => _db = db;

    public async Task<IEnumerable<FreePass>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FreePass>(
            "SELECT id, pass_number, customer_name, headcount, phone, reason, note, " +
            "issued_date, used_date, is_used " +
            "FROM free_passes ORDER BY id DESC");
    }

    public async Task<IEnumerable<FreePass>> GetByMonthAsync(string yearMonth)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FreePass>(
            "SELECT id, pass_number, customer_name, headcount, phone, reason, note, " +
            "issued_date, used_date, is_used " +
            "FROM free_passes WHERE issued_date LIKE @pattern ORDER BY id DESC",
            new { pattern = $"{yearMonth}%" });
    }

    public async Task<string> GetNextPassNumberAsync()
    {
        using var conn = _db.CreateConnection();
        var maxNum = await conn.ExecuteScalarAsync<int?>(
            "SELECT MAX(CAST(SUBSTR(pass_number, 2) AS INTEGER)) FROM free_passes");
        var next = (maxNum ?? 1999) + 1;
        return $"A{next}";
    }

    public async Task<int> InsertAsync(FreePass pass)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO free_passes (pass_number, customer_name, headcount, phone, reason, note, issued_date, is_used) " +
            "VALUES (@PassNumber, @CustomerName, @Headcount, @Phone, @Reason, @Note, @IssuedDate, 0); " +
            "SELECT last_insert_rowid()", pass);
    }

    public async Task MarkUsedAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        await conn.ExecuteAsync(
            "UPDATE free_passes SET is_used = 1, used_date = @today WHERE id = @id",
            new { id, today });
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM free_passes WHERE id = @id", new { id });
    }
}
