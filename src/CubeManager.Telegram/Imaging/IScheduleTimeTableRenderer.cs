using CubeManager.Core.Models;

namespace CubeManager.Telegram.Imaging;

/// <summary>
/// 앱 스케줄탭과 같은 디자인(타임테이블 + 직원별 주간 요약)의 주간 PNG.
/// 구현은 TimeTablePanel 을 재사용하는 UI 프로젝트에서 등록.
/// </summary>
public interface IScheduleTimeTableRenderer
{
    /// <summary>주차별 PNG. 한 번의 호출 안에서는 직원 색상이 모든 이미지에 동일하게 유지됨.</summary>
    IReadOnlyList<Stream> RenderWeeks(IReadOnlyList<ScheduleWeekImage> weeks);
}

/// <param name="Label">날짜 범위 아래 보조 문구 (예: "11월 1주차")</param>
/// <param name="HolidayDates">평일 공휴일 날짜 (YYYY-MM-DD)</param>
public sealed record ScheduleWeekImage(
    DateTime WeekStart,
    DateTime WeekEnd,
    string Label,
    IReadOnlyList<Schedule> Schedules,
    HashSet<string> HolidayDates);
