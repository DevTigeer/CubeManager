using System.Runtime.Versioning;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CubeManager.Telegram.Commands.Reports;

[SupportedOSPlatform("windows")]
public sealed class AttendanceCommandHandler : ICommandHandler
{
    private readonly IAttendanceService _attendance;
    private readonly IBotImageRenderer _renderer;

    public AttendanceCommandHandler(IAttendanceService attendance, IBotImageRenderer renderer)
    {
        _attendance = attendance;
        _renderer = renderer;
    }

    public string Command => "attendance";
    public IReadOnlyList<string> Aliases => new[] { "출퇴근", "근무" };
    public string Description => "출퇴근 현황 (인자 없음=오늘, /attendance YYYY-MM-DD)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var date = DateArgParser.ParseSingleDateOrToday(ctx.Args);
        var dateStr = DateArgParser.FormatDate(date);

        var records = (await _attendance.GetByDateRangeAsync(dateStr, dateStr))
            .OrderBy(a => a.ScheduledStart ?? a.ClockIn ?? "")
            .ToList();

        var headers = new[] { "직원", "출근", "퇴근", "근무" };
        var rows = new List<IReadOnlyList<string>>();

        foreach (var a in records)
        {
            var clockIn = FormatTime(a.ClockIn);
            var clockOut = FormatTime(a.ClockOut);
            var worked = ComputeWorkHours(a.ClockIn, a.ClockOut);
            rows.Add(new[]
            {
                a.EmployeeName ?? $"#{a.EmployeeId}",
                FormatStatus(clockIn, a.ClockInStatus, "지각"),
                FormatStatus(clockOut, a.ClockOutStatus, "조퇴"),
                worked
            });
        }

        using var stream = _renderer.RenderTable(
            "출퇴근 현황",
            DateArgParser.FormatDateKorean(date),
            headers,
            rows,
            $"총 {records.Count}명 · 조회 시각: {DateTime.Now:yyyy-MM-dd HH:mm}");

        var photo = InputFile.FromStream(stream, $"attendance-{dateStr}.png");
        await ctx.Client.SendPhoto(ctx.ChatId, photo, cancellationToken: ctx.CancellationToken);
    }

    private static string FormatTime(string? dt)
    {
        if (string.IsNullOrEmpty(dt)) return "-";
        if (DateTime.TryParse(dt, out var t)) return t.ToString("HH:mm");
        return dt.Length >= 16 ? dt.Substring(11, 5) : dt;
    }

    private static string FormatStatus(string time, string? status, string warningLabel)
    {
        if (time == "-") return "-";
        if (status == "late" || status == "early") return $"{time} ({warningLabel})";
        return time;
    }

    private static string ComputeWorkHours(string? inDt, string? outDt)
    {
        if (string.IsNullOrEmpty(inDt) || string.IsNullOrEmpty(outDt)) return "-";
        if (!DateTime.TryParse(inDt, out var ci) || !DateTime.TryParse(outDt, out var co)) return "-";
        if (co <= ci) return "-";
        var span = co - ci;
        return $"{(int)span.TotalHours}시간 {span.Minutes}분";
    }
}
