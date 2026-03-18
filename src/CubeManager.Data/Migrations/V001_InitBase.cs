using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V001_InitBase : IMigration
{
    public int Version => 1;
    public string Description => "employees, app_config 테이블 생성";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS employees (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                hourly_wage INTEGER NOT NULL DEFAULT 0,
                is_active INTEGER NOT NULL DEFAULT 1,
                phone TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            )
            """, transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS app_config (
                key TEXT PRIMARY KEY,
                value TEXT,
                updated_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            )
            """, transaction: tx);

        // 기본 설정값 시드
        var seeds = new[]
        {
            ("default_meal_allowance", "5000"),
            ("taxi_allowance", "10000"),
            ("taxi_cutoff_time", "23:30"),
            ("holiday_bonus_per_hour", "3000"),
            ("meal_min_hours", "6"),
            ("web_base_url", "http://www.cubeescape.co.kr"),
            ("web_login_id", ""),
            ("web_login_pw", ""),
            ("auto_refresh_enabled", "0"),
            ("auto_refresh_interval_min", "30"),
        };

        foreach (var (key, value) in seeds)
        {
            conn.Execute(
                "INSERT OR IGNORE INTO app_config (key, value) VALUES (@key, @value)",
                new { key, value }, tx);
        }
    }
}
