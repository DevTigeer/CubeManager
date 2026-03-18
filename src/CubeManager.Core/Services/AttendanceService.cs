using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;

namespace CubeManager.Core.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IScheduleRepository _scheduleRepo;

    public AttendanceService(IAttendanceRepository attendanceRepo, IScheduleRepository scheduleRepo)
    {
        _attendanceRepo = attendanceRepo;
        _scheduleRepo = scheduleRepo;
    }

    public async Task<Attendance> ClockInAsync(int employeeId)
    {
        var now = DateTime.Now;
        var today = now.ToString("yyyy-MM-dd");
        var nowTime = now.ToString("yyyy-MM-dd HH:mm:ss");

        // 이미 출근 기록이 있는지 확인
        var existing = await _attendanceRepo.GetByEmployeeDateAsync(employeeId, today);
        if (existing?.ClockIn != null)
            throw new InvalidOperationException("이미 출근 기록이 있습니다.");

        // 스케줄에서 예정 출근 시간 조회
        var schedules = await _scheduleRepo.GetByDateAsync(today);
        var schedule = schedules.FirstOrDefault(s => s.EmployeeId == employeeId);

        var status = "on_time";
        if (schedule != null)
        {
            var scheduledMin = TimeHelper.ToMinutes(schedule.StartTime);
            var actualMin = now.Hour * 60 + now.Minute;
            if (now.Hour < 10) actualMin += 24 * 60; // 자정 보정
            status = actualMin <= scheduledMin ? "on_time" : "late";
        }

        if (existing != null)
        {
            await _attendanceRepo.UpdateClockInAsync(existing.Id, nowTime, status);
            existing.ClockIn = nowTime;
            existing.ClockInStatus = status;
            return existing;
        }

        var record = new Attendance
        {
            EmployeeId = employeeId,
            WorkDate = today,
            ClockIn = nowTime,
            ClockInStatus = status
        };
        record.Id = await _attendanceRepo.InsertAsync(record);
        return record;
    }

    public async Task<Attendance> ClockOutAsync(int employeeId)
    {
        var now = DateTime.Now;
        // 자정 이후 퇴근은 전날 기준
        var workDate = now.Hour < 10
            ? now.AddDays(-1).ToString("yyyy-MM-dd")
            : now.ToString("yyyy-MM-dd");
        var nowTime = now.ToString("yyyy-MM-dd HH:mm:ss");

        var existing = await _attendanceRepo.GetByEmployeeDateAsync(employeeId, workDate);
        if (existing == null)
            throw new InvalidOperationException("출근 기록이 없습니다. 먼저 출근하세요.");
        if (existing.ClockOut != null)
            throw new InvalidOperationException("이미 퇴근 기록이 있습니다.");

        // 스케줄에서 예정 퇴근 시간 조회
        var schedules = await _scheduleRepo.GetByDateAsync(workDate);
        var schedule = schedules.FirstOrDefault(s => s.EmployeeId == employeeId);

        var status = "on_time";
        if (schedule != null)
        {
            var scheduledMin = TimeHelper.ToMinutes(schedule.EndTime);
            var actualMin = now.Hour * 60 + now.Minute;
            if (now.Hour < 10) actualMin += 24 * 60;
            status = actualMin >= scheduledMin ? "on_time" : "early";
        }

        await _attendanceRepo.UpdateClockOutAsync(existing.Id, nowTime, status);
        existing.ClockOut = nowTime;
        existing.ClockOutStatus = status;
        return existing;
    }

    public async Task<IEnumerable<Attendance>> GetTodayStatusAsync()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        return await _attendanceRepo.GetByDateAsync(today);
    }

    public Task<IEnumerable<Attendance>> GetMonthlyHistoryAsync(int employeeId, string yearMonth) =>
        _attendanceRepo.GetByEmployeeMonthAsync(employeeId, yearMonth);
}
