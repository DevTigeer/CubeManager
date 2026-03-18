using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V003_Attendance : IMigration
{
    public int Version => 3;
    public string Description => "attendance 테이블 생성";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS attendance (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                employee_id INTEGER NOT NULL REFERENCES employees(id),
                work_date TEXT NOT NULL,
                clock_in TEXT,
                clock_out TEXT,
                clock_in_status TEXT,
                clock_out_status TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            )
            """, transaction: tx);

        conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_attendance_emp_date ON attendance(employee_id, work_date)", transaction: tx);
    }
}
