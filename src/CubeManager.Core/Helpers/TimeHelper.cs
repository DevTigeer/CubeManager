namespace CubeManager.Core.Helpers;

public static class TimeHelper
{
    /// <summary>HH:MM → 분 단위 변환 (자정 보정: 00:00~09:59는 +24h)</summary>
    public static int ToMinutes(string time)
    {
        var parts = time.Split(':');
        var h = int.Parse(parts[0]);
        var m = int.Parse(parts[1]);
        if (h < 10) h += 24; // 운영시간 10:00 기준, 그 이전은 익일
        return h * 60 + m;
    }

    /// <summary>근무시간(시간 단위) 계산</summary>
    public static double CalcHours(string startTime, string endTime)
    {
        var diff = ToMinutes(endTime) - ToMinutes(startTime);
        return diff > 0 ? diff / 60.0 : 0;
    }

    /// <summary>주어진 날짜가 해당 월의 몇 주차인지 (1-based, 월요일 시작)</summary>
    public static int GetWeekOfMonth(DateTime date)
    {
        var first = new DateTime(date.Year, date.Month, 1);
        var firstMonday = first;
        while (firstMonday.DayOfWeek != DayOfWeek.Monday)
            firstMonday = firstMonday.AddDays(-1);

        var target = date;
        while (target.DayOfWeek != DayOfWeek.Monday)
            target = target.AddDays(-1);

        return ((target - firstMonday).Days / 7) + 1;
    }

    /// <summary>해당 월의 N주차 날짜 범위 (월 경계 클램핑)</summary>
    public static (DateTime start, DateTime end) GetWeekRange(int year, int month, int weekNum)
    {
        var firstDay = new DateTime(year, month, 1);
        var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var firstMonday = firstDay;
        while (firstMonday.DayOfWeek != DayOfWeek.Monday)
            firstMonday = firstMonday.AddDays(-1);

        var weekStart = firstMonday.AddDays((weekNum - 1) * 7);
        var weekEnd = weekStart.AddDays(6);

        if (weekStart < firstDay) weekStart = firstDay;
        if (weekEnd > lastDay) weekEnd = lastDay;

        return (weekStart, weekEnd);
    }

    /// <summary>해당 월의 총 주차 수</summary>
    public static int GetTotalWeeks(int year, int month)
    {
        var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        return GetWeekOfMonth(lastDay);
    }

    /// <summary>30분 간격 타임 슬롯 목록 (10:00 ~ 01:00)</summary>
    public static readonly string[] TimeSlots = GenerateTimeSlots();

    private static string[] GenerateTimeSlots()
    {
        var slots = new List<string>();
        for (var h = 10; h <= 24; h++)
            for (var m = 0; m < 60; m += 30)
            {
                var hour = h >= 24 ? h - 24 : h;
                slots.Add($"{hour:D2}:{m:D2}");
                if (h == 25 || (h == 24 && m == 30)) goto done; // 01:00까지
            }
        done:
        slots.Add("01:00");
        return slots.Distinct().ToArray();
    }
}
