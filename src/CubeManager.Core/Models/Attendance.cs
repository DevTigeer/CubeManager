namespace CubeManager.Core.Models;

public class Attendance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string WorkDate { get; set; } = string.Empty;
    public string? ClockIn { get; set; }     // YYYY-MM-DD HH:MM:SS
    public string? ClockOut { get; set; }
    public string? ClockInStatus { get; set; }  // on_time / late
    public string? ClockOutStatus { get; set; } // on_time / early
    public DateTime CreatedAt { get; set; }

    // 조인용
    public string? EmployeeName { get; set; }
    public string? ScheduledStart { get; set; }
    public string? ScheduledEnd { get; set; }
}
