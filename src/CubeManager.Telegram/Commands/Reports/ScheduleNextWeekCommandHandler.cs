using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;

namespace CubeManager.Telegram.Commands.Reports;

/// <summary>현재 주차의 다음 주(월~일) 스케줄을 스케줄탭 디자인 이미지로 전송.</summary>
public sealed class ScheduleNextWeekCommandHandler : ICommandHandler
{
    private readonly IScheduleService _schedule;
    private readonly IHolidayService _holiday;
    private readonly IScheduleTimeTableRenderer _renderer;

    public ScheduleNextWeekCommandHandler(IScheduleService schedule, IHolidayService holiday, IScheduleTimeTableRenderer renderer)
    {
        _schedule = schedule;
        _holiday = holiday;
        _renderer = renderer;
    }

    public string Command => "schedule_next";
    public IReadOnlyList<string> Aliases => new[] { "다음주스케줄", "다음주스케쥴" };
    public string Description => "다음주 스케줄 이미지 (현재 주차 기준 다음 주)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var (start, _) = DateArgParser.ParseRangeOrCurrentWeek(new[] { "다음주" });

        // 스케줄탭과 같은 주차 기준 (수요일이 속한 월)
        var monday = start.ToDateTime(TimeOnly.MinValue);
        var (year, month) = TimeHelper.GetSalaryMonth(monday);
        var weekNum = TimeHelper.GetWeekOfMonth(monday);

        var week = await ScheduleMonthCommandHandler.LoadWeekAsync(
            _schedule, _holiday, year, month, weekNum, $"다음주 · {month}월 {weekNum}주차");

        await ScheduleMonthCommandHandler.SendImagesAsync(ctx, _renderer, new[] { week });
    }
}
