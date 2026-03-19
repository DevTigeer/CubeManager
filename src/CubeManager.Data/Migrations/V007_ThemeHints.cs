using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V007_ThemeHints : IMigration
{
    public int Version => 7;
    public string Description => "themes, theme_hints 테이블 (테마 힌트 관리)";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS themes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                theme_name TEXT NOT NULL,
                description TEXT,
                sort_order INTEGER NOT NULL DEFAULT 0,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS theme_hints (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                theme_id INTEGER NOT NULL REFERENCES themes(id) ON DELETE CASCADE,
                hint_code INTEGER NOT NULL,
                question TEXT NOT NULL,
                hint1 TEXT NOT NULL,
                hint2 TEXT,
                answer TEXT NOT NULL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);

        conn.Execute(
            "CREATE UNIQUE INDEX IF NOT EXISTS idx_theme_hints_code ON theme_hints(theme_id, hint_code)",
            transaction: tx);

        conn.Execute(
            "CREATE INDEX IF NOT EXISTS idx_theme_hints_theme ON theme_hints(theme_id)",
            transaction: tx);
    }
}
