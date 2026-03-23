using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V013_HandoverTitleCheck : IMigration
{
    public int Version => 13;
    public string Description => "handovers에 title, is_next_worker_checked 컬럼 추가";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        var columns = conn.Query<string>(
            "SELECT name FROM pragma_table_info('handovers')", transaction: tx);
        var colSet = new HashSet<string>(columns);

        if (!colSet.Contains("title"))
            conn.Execute("ALTER TABLE handovers ADD COLUMN title TEXT NOT NULL DEFAULT ''", transaction: tx);

        if (!colSet.Contains("is_next_worker_checked"))
            conn.Execute("ALTER TABLE handovers ADD COLUMN is_next_worker_checked INTEGER NOT NULL DEFAULT 0", transaction: tx);
    }
}
