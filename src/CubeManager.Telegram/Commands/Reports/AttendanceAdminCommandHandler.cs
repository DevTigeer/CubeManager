using System.Runtime.Versioning;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CubeManager.Telegram.Commands.Reports;

[SupportedOSPlatform("windows")]
public sealed class AttendanceAdminCommandHandler : ICommandHandler
{
    private readonly IAttendanceService _attendance;
    private readonly IBotImageRenderer _renderer;

    public AttendanceAdminCommandHandler(IAttendanceService attendance, IBotImageRenderer renderer)
    {
        _attendance = attendance;
        _renderer = renderer;
    }

    public string Command => "attendance_admin";
    public IReadOnlyList<string> Aliases => new[] { "출퇴근관리", "근무관리" };
    public string Description => "관리자 출퇴근 상세 (스케줄 vs 실제)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var (start, end) = DateArgParser.ParseRangeOrCurrentWeek(
            ctx.Args.Length == 0 ? new[] { "오늘" } : ctx.Args);

        var records = (await _attendance.GetByDateRangeAsync(
                DateArgParser.FormatDate(start), DateArgParser.FormatDate(end)))
            .OrderBy(a => a.WorkDate).ThenBy(a => a.ScheduledStart ?? a.ClockIn ?? "")
            .ToList();

        var headers = new[] { "날짜", "직원", "예정", "실제 출/퇴", "근무", "이상" };
        var rows = new List<IReadOnlyList<string>>();
        var lateCount = 0;
        var earlyCount = 0;

        foreach (var a in records)
        {
            var scheduled = string.IsNullOrEmpty(a.ScheduledStart) || string.IsNullOrEmpty(a.ScheduledEnd)
                ? "-"
                : $"{a.ScheduledStart}~{a.ScheduledEnd}";
            var actual = $"{FormatTime(a.ClockIn)} / {FormatTime(a.ClockOut)}";
            var worked = ComputeWorkHours(a.ClockIn, a.ClockOut);

            var anomalies = new List<string>();
            if (a.ClockInStatus == "late") { anomalies.Add("지각"); lateCount++; }
            if (a.ClockOutStatus == "early") { anomalies.Add("조퇴"); earlyCount++; }
            if (string.IsNullOrEmpty(a.ClockOut) && !string.IsNullOrEmpty(a.ClockIn)) anomalies.Add("미퇴근");
            if (string.IsNullOrEmpty(a.ClockIn)) anomalies.Add("결근");

            rows.Add(new[]
            {
                a.WorkDate,
                a.EmployeeName ?? $"#{a.EmployeeId}",
                scheduled,
                actual,
                worked,
                anomalies.Count == 0 ? "OK" : string.Join(",", anomalies)
            });
        }

        var subtitle = start == end
            ? DateArgParser.FormatDateKorean(start)
            : $"{DateArgParser.FormatDate(start)} ~ {DateArgParser.FormatDate(end)}";

        using var stream = _renderer.RenderTable(
            $"출퇴근 상세 (지각 {lateCount} / 조퇴 {earlyCount})",
            subtitle,
            headers,
            rows,
            $"총 {records.Count}건 · 조회 시각: {DateTime.Now:yyyy-MM-dd HH:mm}");

        var photo = InputFile.FromStream(stream, $"attendance-admin-{DateArgParser.FormatDate(start)}.png");
        await ctx.Client.SendPhoto(ctx.ChatId, photo, cancellationToken: ctx.CancellationToken);
    }

    private static string FormatTime(string? dt)
    {
        if (string.IsNullOrEmpty(dt)) return "-";
        if (DateTime.TryParse(dt, out var t)) return t.ToString("HH:mm");
        return dt.Length >= 16 ? dt.Substring(11, 5) : dt;
    }

    private static string ComputeWorkHours(string? inDt, string? outDt)
    {
        if (string.IsNullOrEmpty(inDt) || string.IsNullOrEmpty(outDt)) return "-";
        if (!DateTime.TryParse(inDt, out var ci) || !DateTime.TryParse(outDt, out var co)) return "-";
        if (co <= ci) return "-";
        var span = co - ci;
        return $"{span.TotalHours:0.0}h";
    }
}
