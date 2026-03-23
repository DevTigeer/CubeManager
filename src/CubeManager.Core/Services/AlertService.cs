using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using Serilog;

namespace CubeManager.Core.Services;

public class AlertService : IAlertService
{
    private readonly IAlertLogRepository _alertRepo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IChecklistRepository _checklistRepo;
    private readonly IHandoverRepository _handoverRepo;
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public AlertService(
        IAlertLogRepository alertRepo,
        IAttendanceRepository attendanceRepo,
        IChecklistRepository checklistRepo,
        IHandoverRepository handoverRepo,
        IScheduleRepository scheduleRepo,
        IEmployeeRepository employeeRepo)
    {
        _alertRepo = alertRepo;
        _attendanceRepo = attendanceRepo;
        _checklistRepo = checklistRepo;
        _handoverRepo = handoverRepo;
        _scheduleRepo = scheduleRepo;
        _employeeRepo = employeeRepo;
    }

    /// <summary>체크리스트 미완료 검사: 출근 후 60분 경과 + 완료율 0%</summary>
    public async Task CheckChecklistDelayAsync()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var now = DateTime.Now;

        var attendances = await _attendanceRepo.GetByDateAsync(today);
        foreach (var att in attendances)
        {
            if (string.IsNullOrEmpty(att.ClockIn)) continue;

            var clockIn = DateTime.Parse(att.ClockIn);
            var elapsed = (now - clockIn).TotalMinutes;
            if (elapsed < 60) continue; // 아직 1시간 안 됨

            // 이미 오늘 이 직원에 대해 알림 생성했는지 확인
            if (await _alertRepo.ExistsTodayAsync(AlertTypes.ChecklistDelay, att.EmployeeId))
                continue;

            // 체크리스트 완료율 확인
            var records = await _checklistRepo.GetRecordsForDateAsync(today);
            var total = records.Count();
            if (total == 0) continue;
            var done = records.Count(r => r.IsChecked);
            var rate = (double)done / total * 100;

            if (rate < 50)
            {
                var empName = att.EmployeeName ?? $"ID:{att.EmployeeId}";
                var severity = rate == 0 ? "critical" : "warning";
                var msg = $"{empName}: 출근 후 {(int)elapsed}분 경과, 체크리스트 {done}/{total} ({rate:F0}%) 완료";

                await _alertRepo.InsertAsync(new AlertLog
                {
                    AlertType = AlertTypes.ChecklistDelay,
                    EmployeeId = att.EmployeeId,
                    AlertDate = today,
                    AlertTime = now.ToString("HH:mm:ss"),
                    Severity = severity,
                    Message = msg
                });
                Log.Warning("[ALERT] {Message}", msg);
            }
        }
    }

    /// <summary>인수인계 미확인 검사: 출근 후 30분 경과 + 미확인 건 존재</summary>
    public async Task CheckHandoverUnreadAsync()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var now = DateTime.Now;

        var attendances = await _attendanceRepo.GetByDateAsync(today);
        foreach (var att in attendances)
        {
            if (string.IsNullOrEmpty(att.ClockIn)) continue;

            var clockIn = DateTime.Parse(att.ClockIn);
            var elapsed = (now - clockIn).TotalMinutes;
            if (elapsed < 30) continue;

            if (await _alertRepo.ExistsTodayAsync(AlertTypes.HandoverUnread, att.EmployeeId))
                continue;

            // 최근 미확인 인수인계 확인 (어제~오늘)
            var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            var (handovers, _) = await _handoverRepo.GetPagedAsync(1, 5);
            var unread = handovers.Where(h => !h.IsNextWorkerChecked
                && string.Compare(h.CreatedAt.ToString("yyyy-MM-dd"), yesterday) >= 0).ToList();

            if (unread.Any())
            {
                var empName = att.EmployeeName ?? $"ID:{att.EmployeeId}";
                var msg = $"{empName}: 미확인 인수인계 {unread.Count}건 (출근 후 {(int)elapsed}분 경과)";

                await _alertRepo.InsertAsync(new AlertLog
                {
                    AlertType = AlertTypes.HandoverUnread,
                    EmployeeId = att.EmployeeId,
                    AlertDate = today,
                    AlertTime = now.ToString("HH:mm:ss"),
                    Severity = "warning",
                    Message = msg
                });
                Log.Warning("[ALERT] {Message}", msg);
            }
        }
    }

    /// <summary>무단결근 감지: 12시 기준, 스케줄 있는데 출근 기록 없는 직원</summary>
    public async Task CheckNoShowAsync()
    {
        var now = DateTime.Now;
        if (now.Hour < 12) return; // 12시 이전에는 체크 안 함

        var today = DateTime.Today.ToString("yyyy-MM-dd");

        // 오늘 스케줄된 직원 목록
        var schedules = await _scheduleRepo.GetByDateAsync(today);
        var scheduledIds = schedules.Select(s => s.EmployeeId).Distinct().ToList();

        // 오늘 출근 기록
        var attendances = await _attendanceRepo.GetByDateAsync(today);
        var clockedInIds = attendances.Where(a => !string.IsNullOrEmpty(a.ClockIn))
            .Select(a => a.EmployeeId).ToHashSet();

        foreach (var empId in scheduledIds)
        {
            if (clockedInIds.Contains(empId)) continue; // 출근함

            if (await _alertRepo.ExistsTodayAsync(AlertTypes.NoShow, empId))
                continue;

            var employees = await _employeeRepo.GetActiveAsync();
            var emp = employees.FirstOrDefault(e => e.Id == empId);
            var empName = emp?.Name ?? $"ID:{empId}";

            var sched = schedules.First(s => s.EmployeeId == empId);
            var msg = $"{empName}: 스케줄 {sched.StartTime}~{sched.EndTime} 있으나 출근 기록 없음 (무단결근 의심)";

            await _alertRepo.InsertAsync(new AlertLog
            {
                AlertType = AlertTypes.NoShow,
                EmployeeId = empId,
                AlertDate = today,
                AlertTime = now.ToString("HH:mm:ss"),
                Severity = "critical",
                Message = msg
            });
            Log.Warning("[ALERT] {Message}", msg);
        }
    }

    /// <summary>지각 누적 경고: 이번 달 지각 3회 이상</summary>
    public async Task CheckLateAccumulateAsync()
    {
        var now = DateTime.Now;
        var yearMonth = now.ToString("yyyy-MM");
        var today = now.ToString("yyyy-MM-dd");

        var employees = await _employeeRepo.GetActiveAsync();
        foreach (var emp in employees)
        {
            // 이번 달 지각 횟수 (attendance 테이블에서)
            var attendances = await _attendanceRepo.GetByDateAsync(today); // today용이지만 월간 조회 필요
            // alert_logs에서 이미 이번 달 late_accumulate 알림이 있는지
            if (await _alertRepo.ExistsTodayAsync(AlertTypes.LateAccumulate, emp.Id))
                continue;

            var lateCount = await _alertRepo.GetMonthlyCountAsync(emp.Id, AlertTypes.LateArrival, yearMonth);
            if (lateCount >= 3)
            {
                // 이번 달 누적 경고 이미 발생했는지 확인
                var existingWarn = await _alertRepo.GetMonthlyCountAsync(emp.Id, AlertTypes.LateAccumulate, yearMonth);
                if (existingWarn > 0) continue;

                var msg = $"{emp.Name}: 이번 달 지각 {lateCount}회 누적 (경고 기준 3회 초과)";
                await _alertRepo.InsertAsync(new AlertLog
                {
                    AlertType = AlertTypes.LateAccumulate,
                    EmployeeId = emp.Id,
                    AlertDate = today,
                    AlertTime = now.ToString("HH:mm:ss"),
                    Severity = "critical",
                    Message = msg
                });
                Log.Warning("[ALERT] {Message}", msg);
            }
        }
    }

    public Task<int> GetUnresolvedCountAsync() => _alertRepo.GetUnresolvedCountAsync();

    public Task<IEnumerable<AlertLog>> GetAlertHistoryAsync(string startDate, string endDate, string? alertType = null)
        => _alertRepo.GetByDateRangeAsync(startDate, endDate, alertType);

    public Task ResolveAlertAsync(int alertId, string resolvedBy) => _alertRepo.ResolveAsync(alertId, resolvedBy);
}
