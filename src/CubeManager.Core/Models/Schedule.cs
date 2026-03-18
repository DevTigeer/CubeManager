namespace CubeManager.Core.Models;

public class Schedule
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string WorkDate { get; set; } = string.Empty;  // YYYY-MM-DD
    public string StartTime { get; set; } = string.Empty;  // HH:MM
    public string EndTime { get; set; } = string.Empty;    // HH:MM
    public bool IsHoliday { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 조인용
    public string? EmployeeName { get; set; }
}
