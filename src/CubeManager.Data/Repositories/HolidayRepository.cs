using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class HolidayRepository : IHolidayRepository
{
    private readonly Database _db;

    public HolidayRepository(Database db) => _db = db;

    public async Task<IEnumerable<Holiday>> GetByYearAsync(int year)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Holiday>(
            "SELECT id, holiday_date, holiday_name, is_weekend, year " +
            "FROM holidays WHERE year = @year ORDER BY holiday_date",
            new { year });
    }

    public async Task<bool> IsHolidayAsync(string date)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM holidays WHERE holiday_date = @date",
            new { date }) > 0;
    }

    public async Task<bool> IsWeekdayHolidayAsync(string date)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM holidays WHERE holiday_date = @date AND is_weekend = 0",
            new { date }) > 0;
    }

    public async Task<IEnumerable<Holiday>> GetByMonthAsync(string yearMonth)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Holiday>(
            "SELECT id, holiday_date, holiday_name, is_weekend, year " +
            "FROM holidays WHERE holiday_date LIKE @pattern ORDER BY holiday_date",
            new { pattern = $"{yearMonth}%" });
    }

    public async Task UpsertHolidaysAsync(IEnumerable<Holiday> holidays)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        foreach (var h in holidays)
        {
            await conn.ExecuteAsync(
                "INSERT OR IGNORE INTO holidays (holiday_date, holiday_name, is_weekend, year) " +
                "VALUES (@HolidayDate, @HolidayName, @IsWeekend, @Year)",
                h, tx);
        }

        tx.Commit();
    }

    public async Task<int> GetCountByYearAsync(int year)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM holidays WHERE year = @year",
            new { year });
    }
}
