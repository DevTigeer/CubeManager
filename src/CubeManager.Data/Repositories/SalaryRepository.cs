using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class SalaryRepository : ISalaryRepository
{
    private readonly Database _db;

    public SalaryRepository(Database db) => _db = db;

    public async Task<IEnumerable<SalaryRecord>> GetByMonthAsync(string yearMonth)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<SalaryRecord>(
            "SELECT s.*, e.name AS employee_name, e.hourly_wage " +
            "FROM salary_records s JOIN employees e ON s.employee_id = e.id " +
            "WHERE s.year_month = @yearMonth ORDER BY e.name",
            new { yearMonth });
    }

    public async Task<SalaryRecord?> GetByEmployeeMonthAsync(int employeeId, string yearMonth)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<SalaryRecord>(
            "SELECT s.*, e.name AS employee_name, e.hourly_wage " +
            "FROM salary_records s JOIN employees e ON s.employee_id = e.id " +
            "WHERE s.employee_id = @employeeId AND s.year_month = @yearMonth",
            new { employeeId, yearMonth });
    }

    public async Task UpsertAsync(SalaryRecord r)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO salary_records (employee_id, year_month, week1_hours, week2_hours, week3_hours,
                week4_hours, week5_hours, total_hours, holiday_hours, holiday_bonus, base_salary,
                meal_allowance, taxi_allowance, gross_salary, tax_33, net_salary, is_manual_edit)
            VALUES (@EmployeeId, @YearMonth, @Week1Hours, @Week2Hours, @Week3Hours,
                @Week4Hours, @Week5Hours, @TotalHours, @HolidayHours, @HolidayBonus, @BaseSalary,
                @MealAllowance, @TaxiAllowance, @GrossSalary, @Tax33, @NetSalary, @IsManualEdit)
            ON CONFLICT(employee_id, year_month) DO UPDATE SET
                week1_hours=@Week1Hours, week2_hours=@Week2Hours, week3_hours=@Week3Hours,
                week4_hours=@Week4Hours, week5_hours=@Week5Hours, total_hours=@TotalHours,
                holiday_hours=@HolidayHours, holiday_bonus=@HolidayBonus, base_salary=@BaseSalary,
                meal_allowance=@MealAllowance, taxi_allowance=@TaxiAllowance,
                gross_salary=@GrossSalary, tax_33=@Tax33, net_salary=@NetSalary,
                is_manual_edit=@IsManualEdit, updated_at=datetime('now','localtime')
            """, r);
    }
}
