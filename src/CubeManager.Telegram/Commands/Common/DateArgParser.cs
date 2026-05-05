using System.Globalization;

namespace CubeManager.Telegram.Commands.Common;

public static class DateArgParser
{
    /// <summary>
    /// 단일 날짜 인자 파싱. 인자 없으면 오늘.
    /// 지원: "오늘", "어제", "YYYY-MM-DD", "MM-DD" (올해 보완), "YYYYMMDD"
    /// </summary>
    public static DateOnly ParseSingleDateOrToday(string[] args, DateOnly? today = null)
    {
        var t = today ?? DateOnly.FromDateTime(DateTime.Today);
        if (args.Length == 0) return t;
        return ParseSingleDate(args[0], t) ?? t;
    }

    public static DateOnly? ParseSingleDate(string s, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var x = s.Trim();
        if (x is "오늘" or "today") return today;
        if (x is "어제" or "yesterday") return today.AddDays(-1);

        if (DateOnly.TryParseExact(x, new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;

        if (DateOnly.TryParseExact(x, new[] { "MM-dd", "M-d" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var md))
            return new DateOnly(today.Year, md.Month, md.Day);

        return null;
    }

    /// <summary>
    /// 월 인자 파싱. 인자 없으면 이번달 1일.
    /// 지원: "이번달", "지난달", "YYYY-MM", "YYYYMM"
    /// </summary>
    public static DateOnly ParseMonthOrCurrent(string[] args, DateOnly? today = null)
    {
        var t = today ?? DateOnly.FromDateTime(DateTime.Today);
        var firstOfThis = new DateOnly(t.Year, t.Month, 1);
        if (args.Length == 0) return firstOfThis;
        return ParseMonth(args[0], t) ?? firstOfThis;
    }

    public static DateOnly? ParseMonth(string s, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var x = s.Trim();
        var firstOfThis = new DateOnly(today.Year, today.Month, 1);
        if (x is "이번달") return firstOfThis;
        if (x is "지난달") return firstOfThis.AddMonths(-1);

        if (DateOnly.TryParseExact(x + "-01", new[] { "yyyy-MM-dd", "yyyy/MM/dd" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var ym))
            return new DateOnly(ym.Year, ym.Month, 1);

        if (x.Length == 6 && int.TryParse(x[..4], out var y) && int.TryParse(x[4..], out var m)
            && m >= 1 && m <= 12)
            return new DateOnly(y, m, 1);

        return null;
    }

    /// <summary>
    /// 기간 파싱. 인자 없으면 현재주(월~일).
    /// 지원: "이번주", "지난주", "YYYY-MM-DD~YYYY-MM-DD", 단일날짜(그날 하루)
    /// </summary>
    public static (DateOnly Start, DateOnly End) ParseRangeOrCurrentWeek(string[] args, DateOnly? today = null)
    {
        var t = today ?? DateOnly.FromDateTime(DateTime.Today);
        if (args.Length == 0) return CurrentWeek(t);

        var joined = string.Join(" ", args);
        if (joined is "이번주") return CurrentWeek(t);
        if (joined is "지난주")
        {
            var (s, e) = CurrentWeek(t);
            return (s.AddDays(-7), e.AddDays(-7));
        }

        // YYYY-MM-DD~YYYY-MM-DD (또는 공백 구분 두 인자)
        string? a = null, b = null;
        if (joined.Contains('~'))
        {
            var parts = joined.Split('~', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2) { a = parts[0]; b = parts[1]; }
        }
        else if (args.Length == 2)
        {
            a = args[0]; b = args[1];
        }

        if (a != null && b != null)
        {
            var s = ParseSingleDate(a, t);
            var e = ParseSingleDate(b, t);
            if (s != null && e != null && s <= e) return (s.Value, e.Value);
        }

        // 단일 날짜만 들어오면 그 하루
        var single = ParseSingleDate(args[0], t);
        if (single != null) return (single.Value, single.Value);

        return CurrentWeek(t);
    }

    private static (DateOnly Start, DateOnly End) CurrentWeek(DateOnly today)
    {
        var dow = (int)today.DayOfWeek; // Sun=0
        var fromMonday = dow == 0 ? 6 : dow - 1;
        var monday = today.AddDays(-fromMonday);
        return (monday, monday.AddDays(6));
    }

    public static string FormatDate(DateOnly d) => d.ToString("yyyy-MM-dd");
    public static string FormatMonth(DateOnly d) => d.ToString("yyyy-MM");
    public static string FormatDateKorean(DateOnly d)
    {
        var dow = d.DayOfWeek switch
        {
            DayOfWeek.Monday => "월",
            DayOfWeek.Tuesday => "화",
            DayOfWeek.Wednesday => "수",
            DayOfWeek.Thursday => "목",
            DayOfWeek.Friday => "금",
            DayOfWeek.Saturday => "토",
            _ => "일"
        };
        return $"{d:yyyy-MM-dd} ({dow})";
    }
}
