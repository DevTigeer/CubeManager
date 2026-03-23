using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V014_AlertSystem : IMigration
{
    public int Version => 14;
    public string Description => "알림 시스템 테이블 (alert_logs)";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS alert_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                alert_type TEXT NOT NULL,
                employee_id INTEGER,
                alert_date TEXT NOT NULL,
                alert_time TEXT NOT NULL,
                severity TEXT NOT NULL DEFAULT 'warning',
                message TEXT NOT NULL,
                is_resolved INTEGER NOT NULL DEFAULT 0,
                resolved_by TEXT,
                resolved_at TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                FOREIGN KEY (employee_id) REFERENCES employees(id)
            )
            """, transaction: tx);

        conn.Execute("CREATE INDEX IF NOT EXISTS idx_alert_date ON alert_logs(alert_date)", transaction: tx);
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_alert_employee ON alert_logs(employee_id)", transaction: tx);
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_alert_type ON alert_logs(alert_type)", transaction: tx);
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_alert_resolved ON alert_logs(is_resolved)", transaction: tx);
    }
}
