using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly Database _db;

    public ScheduleRepository(Database db) => _db = db;

    public async Task<IEnumerable<Schedule>> GetByDateRangeAsync(string startDate, string endDate)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Schedule>(
            "SELECT s.id, s.employee_id, s.work_date, s.start_time, s.end_time, " +
            "s.is_holiday, s.note, e.name AS employee_name " +
            "FROM schedules s JOIN employees e ON s.employee_id = e.id " +
            "WHERE s.work_date >= @startDate AND s.work_date <= @endDate " +
            "ORDER BY s.work_date, s.start_time",
            new { startDate, endDate });
    }

    public async Task<IEnumerable<Schedule>> GetByEmployeeAndMonthAsync(int employeeId, string yearMonth)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Schedule>(
            "SELECT id, employee_id, work_date, start_time, end_time, is_holiday, note " +
            "FROM schedules WHERE employee_id = @employeeId " +
            "AND work_date LIKE @pattern ORDER BY work_date",
            new { employeeId, pattern = $"{yearMonth}%" });
    }

    public async Task<IEnumerable<Schedule>> GetByDateAsync(string date)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Schedule>(
            "SELECT s.id, s.employee_id, s.work_date, s.start_time, s.end_time, " +
            "s.is_holiday, s.note, e.name AS employee_name " +
            "FROM schedules s JOIN employees e ON s.employee_id = e.id " +
            "WHERE s.work_date = @date ORDER BY s.start_time",
            new { date });
    }

    public async Task<Schedule?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Schedule>(
            "SELECT id, employee_id, work_date, start_time, end_time, is_holiday, note " +
            "FROM schedules WHERE id = @id", new { id });
    }

    public async Task<int> InsertAsync(Schedule schedule)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO schedules (employee_id, work_date, start_time, end_time, is_holiday, note) " +
            "VALUES (@EmployeeId, @WorkDate, @StartTime, @EndTime, @IsHoliday, @Note); " +
            "SELECT last_insert_rowid()", schedule);
    }

    public async Task BulkInsertAsync(IEnumerable<Schedule> schedules)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var s in schedules)
            {
                await conn.ExecuteAsync(
                    "INSERT OR REPLACE INTO schedules (employee_id, work_date, start_time, end_time, is_holiday, note) " +
                    "VALUES (@EmployeeId, @WorkDate, @StartTime, @EndTime, @IsHoliday, @Note)", s, tx);
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Schedule schedule)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE schedules SET employee_id = @EmployeeId, start_time = @StartTime, end_time = @EndTime, " +
            "is_holiday = @IsHoliday, note = @Note, updated_at = datetime('now','localtime') " +
            "WHERE id = @Id", schedule);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM schedules WHERE id = @id", new { id }) > 0;
    }

    public async Task<bool> DeleteByEmployeeDateAsync(int employeeId, string date)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(
            "DELETE FROM schedules WHERE employee_id = @employeeId AND work_date = @date",
            new { employeeId, date }) > 0;
    }
}
