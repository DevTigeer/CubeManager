using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;

namespace CubeManager.Core.Services;

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IHolidayRepository _holidayRepo;

    public ScheduleService(IScheduleRepository scheduleRepo, IHolidayRepository holidayRepo)
    {
        _scheduleRepo = scheduleRepo;
        _holidayRepo = holidayRepo;
    }

    public async Task<IEnumerable<Schedule>> GetWeekScheduleAsync(int year, int month, int weekNum)
    {
        var (start, end) = TimeHelper.GetWeekRange(year, month, weekNum);
        return await _scheduleRepo.GetByDateRangeAsync(
            start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));
    }

    public async Task AddScheduleAsync(int employeeId, string startTime, string endTime,
        DayOfWeek[] days, int year, int month, int[]? weekNums = null)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var schedules = new List<Schedule>();

        for (var d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(year, month, d);
            if (!days.Contains(date.DayOfWeek)) continue;

            // 주차 필터: weekNums가 지정되면 해당 주차만
            if (weekNums is { Length: > 0 })
            {
                var weekOfMonth = TimeHelper.GetWeekOfMonth(date);
                if (!weekNums.Contains(weekOfMonth)) continue;
            }

            var dateStr = date.ToString("yyyy-MM-dd");
            var isHoliday = await _holidayRepo.IsWeekdayHolidayAsync(dateStr);

            schedules.Add(new Schedule
            {
                EmployeeId = employeeId,
                WorkDate = dateStr,
                StartTime = startTime,
                EndTime = endTime,
                IsHoliday = isHoliday
            });
        }

        if (schedules.Count > 0)
            await _scheduleRepo.BulkInsertAsync(schedules);
    }

    public async Task<bool> UpdateScheduleAsync(int id, string startTime, string endTime)
    {
        // 기존 데이터 보존: 먼저 조회 후 시간만 변경
        var existing = await _scheduleRepo.GetByIdAsync(id);
        if (existing == null) return false;

        existing.StartTime = startTime;
        existing.EndTime = endTime;
        return await _scheduleRepo.UpdateAsync(existing);
    }

    public Task<bool> DeleteScheduleAsync(int id) =>
        _scheduleRepo.DeleteAsync(id);

    public async Task<double> GetWeeklyHoursAsync(int employeeId, int year, int month, int weekNum)
    {
        var (start, end) = TimeHelper.GetWeekRange(year, month, weekNum);
        var schedules = await _scheduleRepo.GetByDateRangeAsync(
            start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));

        return schedules
            .Where(s => s.EmployeeId == employeeId)
            .Sum(s => TimeHelper.CalcHours(s.StartTime, s.EndTime));
    }

    public Task<IEnumerable<Schedule>> GetByDateAsync(string date) =>
        _scheduleRepo.GetByDateAsync(date);
}
