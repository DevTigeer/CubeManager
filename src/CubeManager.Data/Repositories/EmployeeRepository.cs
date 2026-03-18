using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly Database _db;

    public EmployeeRepository(Database db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Employee>(
            "SELECT id, name, hourly_wage, is_active, phone, created_at, updated_at " +
            "FROM employees ORDER BY is_active DESC, name");
    }

    public async Task<IEnumerable<Employee>> GetActiveAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Employee>(
            "SELECT id, name, hourly_wage, is_active, phone, created_at, updated_at " +
            "FROM employees WHERE is_active = 1 ORDER BY name");
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Employee>(
            "SELECT id, name, hourly_wage, is_active, phone, created_at, updated_at " +
            "FROM employees WHERE id = @id",
            new { id });
    }

    public async Task<int> InsertAsync(Employee employee)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO employees (name, hourly_wage, is_active, phone, created_at, updated_at) " +
            "VALUES (@Name, @HourlyWage, @IsActive, @Phone, " +
            "datetime('now','localtime'), datetime('now','localtime')); " +
            "SELECT last_insert_rowid()",
            employee);
    }

    public async Task<bool> UpdateAsync(Employee employee)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE employees SET name = @Name, hourly_wage = @HourlyWage, " +
            "is_active = @IsActive, phone = @Phone, " +
            "updated_at = datetime('now','localtime') " +
            "WHERE id = @Id",
            employee);
        return rows > 0;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE employees SET is_active = 0, updated_at = datetime('now','localtime') " +
            "WHERE id = @id",
            new { id });
        return rows > 0;
    }
}
