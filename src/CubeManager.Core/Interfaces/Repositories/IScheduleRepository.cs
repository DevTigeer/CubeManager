using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IScheduleRepository
{
    /// <summary>특정 날짜 범위의 스케줄 조회 (직원 이름 포함)</summary>
    Task<IEnumerable<Schedule>> GetByDateRangeAsync(string startDate, string endDate);

    /// <summary>직원의 월간 스케줄 조회</summary>
    Task<IEnumerable<Schedule>> GetByEmployeeAndMonthAsync(int employeeId, string yearMonth);

    /// <summary>특정 날짜의 모든 스케줄</summary>
    Task<IEnumerable<Schedule>> GetByDateAsync(string date);

    Task<Schedule?> GetByIdAsync(int id);
    Task<int> InsertAsync(Schedule schedule);
    Task BulkInsertAsync(IEnumerable<Schedule> schedules);
    Task<bool> UpdateAsync(Schedule schedule);
    Task<bool> DeleteAsync(int id);

    /// <summary>특정 직원+날짜의 스케줄 삭제</summary>
    Task<bool> DeleteByEmployeeDateAsync(int employeeId, string date);
}
