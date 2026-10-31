using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CubeManager.Telegram.Commands.Reports;

/// <summary>
/// 월 전체 스케줄 — 1주차부터 마지막 주차까지 스케줄탭 디자인 이미지를 순서대로 전송.
/// 주차 기준은 앱 스케줄탭과 동일 (월~일, 수요일이 속한 월).
/// </summary>
public sealed class ScheduleMonthCommandHandler : ICommandHandler
{
    private readonly IScheduleService _schedule;
    private readonly IHolidayService _holiday;
    private readonly IScheduleTimeTableRenderer _renderer;

    public ScheduleMonthCommandHandler(IScheduleService schedule, IHolidayService holiday, IScheduleTimeTableRenderer renderer)
    {
        _schedule = schedule;
        _holiday = holiday;
        _renderer = renderer;
    }

    public string Command => "schedule_month";
    public IReadOnlyList<string> Aliases => new[] { "월스케줄", "월스케쥴", "이번달스케줄", "이번달스케쥴" };
    public string Description => "월 전체 스케줄 주차별 이미지 (인자 없음=이번달, 다음달, /schedule_month YYYY-MM)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var firstDay = DateArgParser.ParseMonthOrCurrent(ctx.Args);
        var totalWeeks = TimeHelper.GetTotalWeeks(firstDay.Year, firstDay.Month);

        var weeks = new List<ScheduleWeekImage>(totalWeeks);
        for (var weekNum = 1; weekNum <= totalWeeks; weekNum++)
        {
            weeks.Add(await LoadWeekAsync(_schedule, _holiday, firstDay.Year, firstDay.Month, weekNum,
                $"{firstDay.Month}월 {weekNum}주차 ({weekNum}/{totalWeeks})"));
        }

        await SendImagesAsync(ctx, _renderer, weeks);
    }

    /// <summary>스케줄탭과 같은 기준으로 한 주차 데이터 조회. /다음주스케줄 에서도 재사용.</summary>
    internal static async Task<ScheduleWeekImage> LoadWeekAsync(
        IScheduleService schedule, IHolidayService holiday, int year, int month, int weekNum, string label)
    {
        var (start, end) = TimeHelper.GetWeekRange(year, month, weekNum);
        var schedules = (await schedule.GetWeekScheduleAsync(year, month, weekNum)).ToList();

        // 스케줄탭과 동일: 평일 공휴일만 빨간 날 (연말 주차는 두 해 모두 조회)
        var holidayDates = new HashSet<string>();
        foreach (var y in new[] { start.Year, end.Year }.Distinct())
        {
            var holidays = await holiday.GetHolidaysAsync(y);
            holidayDates.UnionWith(holidays.Where(h => !h.IsWeekend).Select(h => h.HolidayDate));
        }

        return new ScheduleWeekImage(start, end, label, schedules, holidayDates);
    }

    internal static async Task SendImagesAsync(
        CommandContext ctx, IScheduleTimeTableRenderer renderer, IReadOnlyList<ScheduleWeekImage> weeks)
    {
        var images = renderer.RenderWeeks(weeks);
        try
        {
            for (var i = 0; i < images.Count; i++)
            {
                var photo = InputFile.FromStream(images[i], $"schedule-{weeks[i].WeekStart:yyyy-MM-dd}.png");
                await ctx.Client.SendPhoto(ctx.ChatId, photo, cancellationToken: ctx.CancellationToken);
            }
        }
        finally
        {
            foreach (var image in images) image.Dispose();
        }
    }
}
