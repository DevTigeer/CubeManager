using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V022_WebReservationId : IMigration
{
    public int Version => 22;
    public string Description => "reservations에 웹 예약번호(web_reservation_id) 추가";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        var columns = conn.Query<string>(
            "SELECT name FROM pragma_table_info('reservations')", transaction: tx);
        var colSet = new HashSet<string>(columns);

        if (!colSet.Contains("web_reservation_id"))
            conn.Execute("ALTER TABLE reservations ADD COLUMN web_reservation_id TEXT", transaction: tx);

        conn.Execute("""
            CREATE UNIQUE INDEX IF NOT EXISTS idx_reservations_web_reservation_id
            ON reservations(web_reservation_id)
            WHERE web_reservation_id IS NOT NULL AND web_reservation_id <> ''
            """, transaction: tx);
    }
}
