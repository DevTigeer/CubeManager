using System.Data;
using System.Globalization;
using Dapper;

namespace CubeManager.Data.Migrations;

/// <summary>
/// 2026 공휴일 시드(V002) 정정 + 2027 공휴일 추가.
/// - V002 의 추석(10/4~10/6)이 틀림 → 실제 9/24~9/26, 10/5 는 개천절 대체공휴일
/// - 누락: 6/3 전국동시지방선거, 8/17 광복절 대체공휴일
/// - 2026년 신규 공휴일: 노동절(5/1), 제헌절(7/17) — 공휴일법 개정, 대체공휴일 적용
/// 공휴일 API 키가 없으면 동기화가 돌지 않으므로 이 시드가 사실상 유일한 출처.
/// </summary>
public class V027_FixHolidays : IMigration
{
    public int Version => 27;
    public string Description => "2026 공휴일 정정 + 2027 공휴일 시드 + 스케줄 공휴일 플래그 재계산";

    private static readonly (string Date, string Name)[] Holidays =
    [
        // 2026
        ("2026-01-01", "신정"),
        ("2026-02-16", "설날 전날"),
        ("2026-02-17", "설날"),
        ("2026-02-18", "설날 다음날"),
        ("2026-03-01", "삼일절"),
        ("2026-03-02", "삼일절 대체공휴일"),
        ("2026-05-01", "노동절"),
        ("2026-05-05", "어린이날"),
        ("2026-05-24", "부처님오신날"),
        ("2026-05-25", "부처님오신날 대체공휴일"),
        ("2026-06-03", "전국동시지방선거"),
        ("2026-06-06", "현충일"),
        ("2026-07-17", "제헌절"),
        ("2026-08-15", "광복절"),
        ("2026-08-17", "광복절 대체공휴일"),
        ("2026-09-24", "추석 전날"),
        ("2026-09-25", "추석"),
        ("2026-09-26", "추석 다음날"),
        ("2026-10-03", "개천절"),
        ("2026-10-05", "개천절 대체공휴일"),
        ("2026-10-09", "한글날"),
        ("2026-12-25", "성탄절"),

        // 2027
        ("2027-01-01", "신정"),
        ("2027-02-06", "설날 전날"),
        ("2027-02-07", "설날"),
        ("2027-02-08", "설날 다음날"),
        ("2027-02-09", "설날 대체공휴일"),
        ("2027-03-01", "삼일절"),
        ("2027-05-01", "노동절"),
        ("2027-05-03", "노동절 대체공휴일"),
        ("2027-05-05", "어린이날"),
        ("2027-05-13", "부처님오신날"),
        ("2027-06-06", "현충일"),
        ("2027-07-17", "제헌절"),
        ("2027-07-19", "제헌절 대체공휴일"),
        ("2027-08-15", "광복절"),
        ("2027-08-16", "광복절 대체공휴일"),
        ("2027-09-14", "추석 전날"),
        ("2027-09-15", "추석"),
        ("2027-09-16", "추석 다음날"),
        ("2027-10-03", "개천절"),
        ("2027-10-04", "개천절 대체공휴일"),
        ("2027-10-09", "한글날"),
        ("2027-10-11", "한글날 대체공휴일"),
        ("2027-12-25", "성탄절"),
        ("2027-12-27", "성탄절 대체공휴일"),
    ];

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute(
            "DELETE FROM holidays WHERE holiday_date >= '2026-01-01' AND holiday_date < '2028-01-01'",
            transaction: tx);

        foreach (var (date, name) in Holidays)
        {
            var d = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1 : 0;
            conn.Execute(
                "INSERT INTO holidays (holiday_date, holiday_name, is_weekend, year) " +
                "VALUES (@date, @name, @isWeekend, @year)",
                new { date, name, isWeekend, year = d.Year }, tx);
        }

        // 저장된 스케줄의 공휴일 플래그를 정정된 공휴일 기준으로 재계산 (평일 공휴일만 1)
        conn.Execute("""
            UPDATE schedules
            SET is_holiday = CASE WHEN EXISTS (
                SELECT 1 FROM holidays h
                WHERE h.holiday_date = schedules.work_date AND h.is_weekend = 0
            ) THEN 1 ELSE 0 END
            WHERE work_date >= '2026-01-01'
            """, transaction: tx);
    }
}
