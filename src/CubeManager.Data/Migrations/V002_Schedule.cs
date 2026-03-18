using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V002_Schedule : IMigration
{
    public int Version => 2;
    public string Description => "schedules, holidays 테이블 생성 + 2026 공휴일 시드";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS schedules (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                employee_id INTEGER NOT NULL REFERENCES employees(id),
                work_date TEXT NOT NULL,
                start_time TEXT NOT NULL,
                end_time TEXT NOT NULL,
                is_holiday INTEGER NOT NULL DEFAULT 0,
                note TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            )
            """, transaction: tx);

        conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_schedules_emp_date ON schedules(employee_id, work_date)", transaction: tx);
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_schedules_date ON schedules(work_date)", transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS holidays (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                holiday_date TEXT NOT NULL UNIQUE,
                holiday_name TEXT,
                is_weekend INTEGER NOT NULL DEFAULT 0,
                year INTEGER
            )
            """, transaction: tx);

        conn.Execute("CREATE INDEX IF NOT EXISTS idx_holidays_date ON holidays(holiday_date)", transaction: tx);

        // 2026년 공휴일 시드
        var holidays = new[]
        {
            ("2026-01-01", "신정", 0),
            ("2026-02-16", "설날 전날", 0),
            ("2026-02-17", "설날", 0),
            ("2026-02-18", "설날 다음날", 0),
            ("2026-03-01", "삼일절", 1),
            ("2026-03-02", "삼일절 대체휴일", 0),
            ("2026-05-05", "어린이날", 0),
            ("2026-05-24", "부처님오신날", 1),
            ("2026-05-25", "부처님오신날 대체", 0),
            ("2026-06-06", "현충일", 1),
            ("2026-08-15", "광복절", 1),
            ("2026-10-03", "개천절", 1),
            ("2026-10-04", "추석 전날", 1),
            ("2026-10-05", "추석", 0),
            ("2026-10-06", "추석 다음날", 0),
            ("2026-10-09", "한글날", 0),
            ("2026-12-25", "크리스마스", 0),
        };

        foreach (var (date, name, isWeekend) in holidays)
        {
            conn.Execute(
                "INSERT OR IGNORE INTO holidays (holiday_date, holiday_name, is_weekend, year) " +
                "VALUES (@date, @name, @isWeekend, 2026)",
                new { date, name, isWeekend }, tx);
        }
    }
}
