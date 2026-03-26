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
        var schedules = new List<Schedule>();

        // 주차별 실제 날짜 범위를 계산하여 월 경계를 넘는 날짜도 포함
        // ex) 4월 1주차 화요일 = 3/31 (수요일 기준으로 4월에 속하는 주)
        var totalWeeks = TimeHelper.GetTotalWeeks(year, month);
        var targetWeeks = weekNums is { Length: > 0 }
            ? weekNums
            : Enumerable.Range(1, totalWeeks).ToArray();

        var addedDates = new HashSet<string>(); // 중복 방지

        foreach (var weekNum in targetWeeks)
        {
            var (weekStart, weekEnd) = TimeHelper.GetWeekRange(year, month, weekNum);

            // 주의 월~일 전체를 순회 (월 경계 넘김 허용)
            for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
            {
                if (!days.Contains(date.DayOfWeek)) continue;

                var dateStr = date.ToString("yyyy-MM-dd");
                if (!addedDates.Add(dateStr)) continue; // 이미 추가된 날짜 스킵

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

    public async Task<bool> ChangeEmployeeAsync(int scheduleId, int newEmployeeId)
    {
        var existing = await _scheduleRepo.GetByIdAsync(scheduleId);
        if (existing == null) return false;

        existing.EmployeeId = newEmployeeId;
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
