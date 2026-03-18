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
        DayOfWeek[] days, int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var schedules = new List<Schedule>();

        for (var d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(year, month, d);
            if (!days.Contains(date.DayOfWeek)) continue;

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

    public Task<bool> UpdateScheduleAsync(int id, string startTime, string endTime)
    {
        return _scheduleRepo.UpdateAsync(new Schedule
        {
            Id = id,
            StartTime = startTime,
            EndTime = endTime
        });
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
