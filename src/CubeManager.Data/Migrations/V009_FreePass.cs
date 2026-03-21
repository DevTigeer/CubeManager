using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V009_FreePass : IMigration
{
    public int Version => 9;
    public string Description => "무료이용권 테이블 생성";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS free_passes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                pass_number TEXT NOT NULL UNIQUE,
                customer_name TEXT NOT NULL,
                headcount INTEGER NOT NULL DEFAULT 1,
                phone TEXT,
                reason TEXT NOT NULL DEFAULT 'record',
                note TEXT,
                issued_date TEXT NOT NULL,
                used_date TEXT,
                is_used INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);

        conn.Execute("CREATE INDEX IF NOT EXISTS idx_free_passes_number ON free_passes(pass_number)", transaction: tx);
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_free_passes_used ON free_passes(is_used)", transaction: tx);
    }
}
