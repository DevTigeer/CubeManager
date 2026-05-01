using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V021_ReservationNote : IMigration
{
    public int Version => 21;
    public string Description => "reservations에 비고(note) 컬럼 추가";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        var columns = conn.Query<string>(
            "SELECT name FROM pragma_table_info('reservations')", transaction: tx);
        var colSet = new HashSet<string>(columns);

        if (!colSet.Contains("note"))
            conn.Execute("ALTER TABLE reservations ADD COLUMN note TEXT", transaction: tx);
    }
}
