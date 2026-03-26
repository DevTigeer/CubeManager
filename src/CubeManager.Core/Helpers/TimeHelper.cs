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

    /// <summary>주어진 날짜가 해당 월의 몇 주차인지 (1-based, 월요일 시작, 수요일 기준 월 판정)</summary>
    public static int GetWeekOfMonth(DateTime date)
    {
        // 해당 날짜의 주 월요일 찾기
        var monday = date;
        while (monday.DayOfWeek != DayOfWeek.Monday)
            monday = monday.AddDays(-1);

        // 이 주의 수요일이 속한 월 기준
        var wednesday = monday.AddDays(2);
        var targetMonth = wednesday.Month;
        var targetYear = wednesday.Year;

        // 해당 월의 1주차 월요일 찾기 (첫 번째 주: 수요일이 해당 월에 속하는 첫 주)
        var firstOfMonth = new DateTime(targetYear, targetMonth, 1);
        var firstMonday = firstOfMonth;
        while (firstMonday.DayOfWeek != DayOfWeek.Monday)
            firstMonday = firstMonday.AddDays(-1);

        // 첫 주의 수요일이 이전 달이면 다음 주가 1주차
        if (firstMonday.AddDays(2).Month != targetMonth)
            firstMonday = firstMonday.AddDays(7);

        return ((monday - firstMonday).Days / 7) + 1;
    }

    /// <summary>해당 월의 N주차 날짜 범위 (월~일, 수요일 기준 월 판정, 월경계 넘김 허용)</summary>
    public static (DateTime start, DateTime end) GetWeekRange(int year, int month, int weekNum)
    {
        var firstOfMonth = new DateTime(year, month, 1);

        // 1일이 속한 주의 월요일 찾기
        var firstMonday = firstOfMonth;
        while (firstMonday.DayOfWeek != DayOfWeek.Monday)
            firstMonday = firstMonday.AddDays(-1);

        // 수요일 기준: 첫 주의 수요일이 해당 월이 아니면 다음 주가 1주차
        if (firstMonday.AddDays(2).Month != month)
            firstMonday = firstMonday.AddDays(7);

        var weekStart = firstMonday.AddDays((weekNum - 1) * 7);
        var weekEnd = weekStart.AddDays(6);

        return (weekStart, weekEnd);
    }

    /// <summary>
    /// 주간이 속하는 급여정산 월을 결정.
    /// 기준: 해당 주의 수요일이 속한 월 = 정산 월.
    /// ex) 2/23(월)~3/1(일) → 수요일=2/25 → 2월 정산
    /// ex) 3/30(월)~4/5(일) → 수요일=4/1 → 4월 정산
    /// </summary>
    public static (int year, int month) GetSalaryMonth(DateTime weekStart)
    {
        var wednesday = weekStart.AddDays(2); // 월+2 = 수요일
        return (wednesday.Year, wednesday.Month);
    }

    /// <summary>해당 월의 총 주차 수 (수요일 기준)</summary>
    public static int GetTotalWeeks(int year, int month)
    {
        var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        // 마지막 날의 주 월요일 찾기
        var lastMonday = lastDay;
        while (lastMonday.DayOfWeek != DayOfWeek.Monday)
            lastMonday = lastMonday.AddDays(-1);

        // 마지막 주의 수요일이 이번 달이면 포함, 다음 달이면 제외
        var lastWednesday = lastMonday.AddDays(2);
        if (lastWednesday.Month != month)
            lastMonday = lastMonday.AddDays(-7); // 한 주 전이 실제 마지막 주

        return GetWeekOfMonth(lastMonday);
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
