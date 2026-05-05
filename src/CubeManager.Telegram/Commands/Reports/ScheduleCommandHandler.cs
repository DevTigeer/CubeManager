using System.Runtime.Versioning;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CubeManager.Telegram.Commands.Reports;

[SupportedOSPlatform("windows")]
public sealed class ScheduleCommandHandler : ICommandHandler
{
    private readonly IScheduleService _schedule;
    private readonly IBotImageRenderer _renderer;

    public ScheduleCommandHandler(IScheduleService schedule, IBotImageRenderer renderer)
    {
        _schedule = schedule;
        _renderer = renderer;
    }

    public string Command => "schedule";
    public IReadOnlyList<string> Aliases => new[] { "스케줄", "스케쥴" };
    public string Description => "스케줄표 (인자 없음=현재주, /schedule YYYY-MM-DD~YYYY-MM-DD)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var (start, end) = DateArgParser.ParseRangeOrCurrentWeek(ctx.Args);

        var allSchedules = new List<Schedule>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            var daily = await _schedule.GetByDateAsync(DateArgParser.FormatDate(d));
            allSchedules.AddRange(daily);
        }

        // 직원별 그룹화 → 행
        var byEmployee = allSchedules
            .GroupBy(s => new { s.EmployeeId, s.EmployeeName })
            .OrderBy(g => g.Key.EmployeeName)
            .ToList();

        var dayCount = (end.DayNumber - start.DayNumber) + 1;
        var headers = new List<string> { "직원" };
        for (var i = 0; i < dayCount; i++)
        {
            var d = start.AddDays(i);
            var dow = d.DayOfWeek switch
            {
                DayOfWeek.Monday => "월", DayOfWeek.Tuesday => "화", DayOfWeek.Wednesday => "수",
                DayOfWeek.Thursday => "목", DayOfWeek.Friday => "금", DayOfWeek.Saturday => "토",
                _ => "일"
            };
            headers.Add($"{d.Month}/{d.Day}({dow})");
        }

        var rows = new List<IReadOnlyList<string>>();
        foreach (var grp in byEmployee)
        {
            var row = new List<string> { grp.Key.EmployeeName ?? $"#{grp.Key.EmployeeId}" };
            for (var i = 0; i < dayCount; i++)
            {
                var d = DateArgParser.FormatDate(start.AddDays(i));
                var match = grp.FirstOrDefault(s => s.WorkDate == d);
                row.Add(match == null ? "-" : $"{match.StartTime}~{match.EndTime}");
            }
            rows.Add(row);
        }

        var subtitle = start == end
            ? DateArgParser.FormatDateKorean(start)
            : $"{DateArgParser.FormatDate(start)} ~ {DateArgParser.FormatDate(end)}";

        using var stream = _renderer.RenderTable(
            "주간 스케줄",
            subtitle,
            headers,
            rows,
            $"총 {byEmployee.Count}명 · 조회 시각: {DateTime.Now:yyyy-MM-dd HH:mm}");

        var photo = InputFile.FromStream(stream, $"schedule-{DateArgParser.FormatDate(start)}.png");
        await ctx.Client.SendPhoto(ctx.ChatId, photo, cancellationToken: ctx.CancellationToken);
    }
}
