using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V008_ReservationThemeName : IMigration
{
    public int Version => 8;
    public string Description => "reservations 테이블 room_name → theme_name 컬럼 추가, 복합 유니크 인덱스";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        // room_name 컬럼을 theme_name으로 대체 (SQLite는 RENAME COLUMN 지원)
        conn.Execute(
            "ALTER TABLE reservations RENAME COLUMN room_name TO theme_name",
            transaction: tx);

        // 날짜+시간+테마+예약자 복합 유니크 인덱스 (Upsert 키)
        conn.Execute("""
            CREATE UNIQUE INDEX IF NOT EXISTS idx_reservations_upsert
            ON reservations(reservation_date, time_slot, theme_name, customer_name)
            """, transaction: tx);
    }
}
