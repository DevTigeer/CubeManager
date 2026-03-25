using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V015_WorkParts : IMigration
{
    public int Version => 15;
    public string Description => "근무 파트 테이블 (오픈/마감/미들 등)";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS work_parts (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                part_name   TEXT    NOT NULL,
                start_time  TEXT    NOT NULL,
                end_time    TEXT    NOT NULL,
                sort_order  INTEGER NOT NULL DEFAULT 0,
                is_active   INTEGER NOT NULL DEFAULT 1,
                created_at  TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
            )
        """, transaction: tx);

        conn.Execute("""
            INSERT INTO work_parts (part_name, start_time, end_time, sort_order) VALUES
                ('오픈', '10:00', '17:00', 1),
                ('마감', '17:00', '24:00', 2),
                ('미들1', '12:00', '19:00', 3),
                ('미들2', '14:00', '21:00', 4)
        """, transaction: tx);
    }
}
