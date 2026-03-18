namespace CubeManager.Core.Models;

public class SalaryRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string YearMonth { get; set; } = string.Empty;
    public double Week1Hours { get; set; }
    public double Week2Hours { get; set; }
    public double Week3Hours { get; set; }
    public double Week4Hours { get; set; }
    public double Week5Hours { get; set; }
    public double TotalHours { get; set; }
    public double HolidayHours { get; set; }
    public int HolidayBonus { get; set; }
    public int BaseSalary { get; set; }
    public int MealAllowance { get; set; }
    public int TaxiAllowance { get; set; }
    public int GrossSalary { get; set; }
    public int Tax33 { get; set; }
    public int NetSalary { get; set; }
    public bool IsManualEdit { get; set; }

    // 조인용
    public string? EmployeeName { get; set; }
    public int HourlyWage { get; set; }
}
