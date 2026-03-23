using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V012_SaleNoteVerify : IMigration
{
    public int Version => 12;
    public string Description => "sale_items에 비고(note), 정산확인(is_verified) 컬럼 추가";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        // 컬럼 존재 여부 확인 후 추가 (ALTER TABLE IF NOT EXISTS 미지원)
        var columns = conn.Query<string>(
            "SELECT name FROM pragma_table_info('sale_items')", transaction: tx);
        var colSet = new HashSet<string>(columns);

        if (!colSet.Contains("note"))
            conn.Execute("ALTER TABLE sale_items ADD COLUMN note TEXT", transaction: tx);

        if (!colSet.Contains("is_verified"))
            conn.Execute("ALTER TABLE sale_items ADD COLUMN is_verified INTEGER NOT NULL DEFAULT 0", transaction: tx);
    }
}
