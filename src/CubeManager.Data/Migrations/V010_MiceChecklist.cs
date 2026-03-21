using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V010_MiceChecklist : IMigration
{
    public int Version => 10;
    public string Description => "미끼관리 팝업 + 체크리스트 테이블 생성";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS mice_popups (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                content TEXT NOT NULL,
                interval_minutes INTEGER NOT NULL DEFAULT 60,
                is_active INTEGER NOT NULL DEFAULT 1,
                last_shown_at TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS checklist_templates (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                day_of_week INTEGER NOT NULL,
                task_text TEXT NOT NULL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                is_active INTEGER NOT NULL DEFAULT 1
            )
            """, transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS checklist_records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                template_id INTEGER NOT NULL REFERENCES checklist_templates(id),
                check_date TEXT NOT NULL,
                is_checked INTEGER NOT NULL DEFAULT 0,
                checked_by TEXT,
                checked_at TEXT,
                UNIQUE(template_id, check_date)
            )
            """, transaction: tx);

        conn.Execute("CREATE INDEX IF NOT EXISTS idx_checklist_records_date ON checklist_records(check_date)", transaction: tx);
    }
}
