using Dapper;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;

namespace CubeManager.Data.Repositories;

public class AlertLogRepository : IAlertLogRepository
{
    private readonly Database _db;
    public AlertLogRepository(Database db) => _db = db;

    public async Task InsertAsync(AlertLog log)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO alert_logs (alert_type, employee_id, alert_date, alert_time, severity, message)
            VALUES (@AlertType, @EmployeeId, @AlertDate, @AlertTime, @Severity, @Message)
            """, log);
    }

    public async Task<IEnumerable<AlertLog>> GetByDateRangeAsync(string startDate, string endDate, string? alertType = null)
    {
        using var conn = _db.CreateConnection();
        var sql = """
            SELECT a.*, e.name AS EmployeeName
            FROM alert_logs a
            LEFT JOIN employees e ON a.employee_id = e.id
            WHERE a.alert_date BETWEEN @startDate AND @endDate
            """;
        if (!string.IsNullOrEmpty(alertType))
            sql += " AND a.alert_type = @alertType";
        sql += " ORDER BY a.alert_date DESC, a.alert_time DESC";

        return await conn.QueryAsync<AlertLog>(sql, new { startDate, endDate, alertType });
    }

    public async Task<int> GetUnresolvedCountAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM alert_logs WHERE is_resolved = 0");
    }

    public async Task ResolveAsync(int id, string resolvedBy)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("""
            UPDATE alert_logs SET is_resolved = 1, resolved_by = @resolvedBy,
                resolved_at = datetime('now', 'localtime')
            WHERE id = @id
            """, new { id, resolvedBy });
    }

    public async Task<bool> ExistsTodayAsync(string alertType, int? employeeId)
    {
        using var conn = _db.CreateConnection();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        return await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM alert_logs
            WHERE alert_type = @alertType AND alert_date = @today
                AND (@employeeId IS NULL OR employee_id = @employeeId)
            """, new { alertType, today, employeeId }) > 0;
    }

    public async Task<int> GetMonthlyCountAsync(int employeeId, string alertType, string yearMonth)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM alert_logs
            WHERE employee_id = @employeeId AND alert_type = @alertType
                AND alert_date LIKE @pattern
            """, new { employeeId, alertType, pattern = yearMonth + "%" });
    }
}
