using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface IAttendanceService
{
    Task<Attendance> ClockInAsync(int employeeId);
    Task<Attendance> ClockOutAsync(int employeeId);
    Task<IEnumerable<Attendance>> GetTodayStatusAsync();
    Task<IEnumerable<Attendance>> GetMonthlyHistoryAsync(int employeeId, string yearMonth);
}
