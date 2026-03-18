using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V006_HandoverInventory : IMigration
{
    public int Version => 6;
    public string Description => "handovers, handover_comments, inventory 테이블";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS handovers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                author_name TEXT NOT NULL,
                content TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);

        conn.Execute("CREATE INDEX IF NOT EXISTS idx_handovers_created ON handovers(created_at DESC)", transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS handover_comments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                handover_id INTEGER NOT NULL REFERENCES handovers(id) ON DELETE CASCADE,
                parent_comment_id INTEGER REFERENCES handover_comments(id),
                author_name TEXT NOT NULL,
                content TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS inventory (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                item_name TEXT NOT NULL,
                required_qty INTEGER DEFAULT 0,
                current_qty INTEGER DEFAULT 0,
                category TEXT,
                note TEXT,
                updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);
    }
}
