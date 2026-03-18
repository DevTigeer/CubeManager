using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IAttendanceRepository
{
    Task<Attendance?> GetByEmployeeDateAsync(int employeeId, string date);
    Task<IEnumerable<Attendance>> GetByDateAsync(string date);
    Task<IEnumerable<Attendance>> GetByEmployeeMonthAsync(int employeeId, string yearMonth);
    Task<int> InsertAsync(Attendance record);
    Task<bool> UpdateClockInAsync(int id, string clockIn, string status);
    Task<bool> UpdateClockOutAsync(int id, string clockOut, string status);
}
