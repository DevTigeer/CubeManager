using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface IScheduleService
{
    /// <summary>주간 스케줄 조회</summary>
    Task<IEnumerable<Schedule>> GetWeekScheduleAsync(int year, int month, int weekNum);

    /// <summary>직원 스케줄 일괄 추가 (요일별, 해당 월, 선택 주차)</summary>
    Task AddScheduleAsync(int employeeId, string startTime, string endTime,
        DayOfWeek[] days, int year, int month, int[]? weekNums = null);

    /// <summary>단일 스케줄 수정</summary>
    Task<bool> UpdateScheduleAsync(int id, string startTime, string endTime);

    /// <summary>스케줄 직원 변경</summary>
    Task<bool> ChangeEmployeeAsync(int scheduleId, int newEmployeeId);

    /// <summary>스케줄 삭제</summary>
    Task<bool> DeleteScheduleAsync(int id);

    /// <summary>직원별 주간 근무시간 합산</summary>
    Task<double> GetWeeklyHoursAsync(int employeeId, int year, int month, int weekNum);

    /// <summary>특정 날짜의 스케줄 조회</summary>
    Task<IEnumerable<Schedule>> GetByDateAsync(string date);
}
