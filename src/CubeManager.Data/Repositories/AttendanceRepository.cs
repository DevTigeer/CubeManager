using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly Database _db;

    public AttendanceRepository(Database db) => _db = db;

    public async Task<Attendance?> GetByEmployeeDateAsync(int employeeId, string date)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Attendance>(
            "SELECT a.id, a.employee_id, a.work_date, a.clock_in, a.clock_out, " +
            "a.clock_in_status, a.clock_out_status, e.name AS employee_name, " +
            "s.start_time AS scheduled_start, s.end_time AS scheduled_end " +
            "FROM attendance a " +
            "JOIN employees e ON a.employee_id = e.id " +
            "LEFT JOIN schedules s ON a.employee_id = s.employee_id AND a.work_date = s.work_date " +
            "WHERE a.employee_id = @employeeId AND a.work_date = @date",
            new { employeeId, date });
    }

    public async Task<IEnumerable<Attendance>> GetByDateAsync(string date)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Attendance>(
            "SELECT a.id, a.employee_id, a.work_date, a.clock_in, a.clock_out, " +
            "a.clock_in_status, a.clock_out_status, e.name AS employee_name, " +
            "s.start_time AS scheduled_start, s.end_time AS scheduled_end " +
            "FROM attendance a " +
            "JOIN employees e ON a.employee_id = e.id " +
            "LEFT JOIN schedules s ON a.employee_id = s.employee_id AND a.work_date = s.work_date " +
            "WHERE a.work_date = @date ORDER BY a.clock_in",
            new { date });
    }

    public async Task<IEnumerable<Attendance>> GetByEmployeeMonthAsync(int employeeId, string yearMonth)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Attendance>(
            "SELECT a.id, a.employee_id, a.work_date, a.clock_in, a.clock_out, " +
            "a.clock_in_status, a.clock_out_status, e.name AS employee_name, " +
            "s.start_time AS scheduled_start, s.end_time AS scheduled_end " +
            "FROM attendance a " +
            "JOIN employees e ON a.employee_id = e.id " +
            "LEFT JOIN schedules s ON a.employee_id = s.employee_id AND a.work_date = s.work_date " +
            "WHERE a.employee_id = @employeeId AND a.work_date LIKE @pattern " +
            "ORDER BY a.work_date DESC",
            new { employeeId, pattern = $"{yearMonth}%" });
    }

    public async Task<int> InsertAsync(Attendance record)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO attendance (employee_id, work_date, clock_in, clock_in_status) " +
            "VALUES (@EmployeeId, @WorkDate, @ClockIn, @ClockInStatus); " +
            "SELECT last_insert_rowid()", record);
    }

    public async Task<bool> UpdateClockInAsync(int id, string clockIn, string status)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE attendance SET clock_in = @clockIn, clock_in_status = @status WHERE id = @id",
            new { id, clockIn, status }) > 0;
    }

    public async Task<bool> UpdateClockOutAsync(int id, string clockOut, string status)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE attendance SET clock_out = @clockOut, clock_out_status = @status WHERE id = @id",
            new { id, clockOut, status }) > 0;
    }
}
