using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V026_ReservationVerified : IMigration
{
    public int Version => 26;
    public string Description => "reservations에 정산체크(is_verified) 컬럼 추가";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        var columns = conn.Query<string>(
            "SELECT name FROM pragma_table_info('reservations')", transaction: tx);
        var colSet = new HashSet<string>(columns);

        if (!colSet.Contains("is_verified"))
            conn.Execute(
                "ALTER TABLE reservations ADD COLUMN is_verified INTEGER NOT NULL DEFAULT 0",
                transaction: tx);
    }
}
