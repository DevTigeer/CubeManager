using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V004_ReservationSales : IMigration
{
    public int Version => 4;
    public string Description => "reservations, daily_sales, sale_items, cash_balance 테이블";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS reservations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                reservation_date TEXT NOT NULL,
                time_slot TEXT,
                room_name TEXT,
                customer_name TEXT,
                customer_phone TEXT,
                headcount INTEGER DEFAULT 0,
                status TEXT DEFAULT 'confirmed',
                raw_html TEXT,
                synced_at TEXT
            )
            """, transaction: tx);
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_reservations_date ON reservations(reservation_date)", transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS daily_sales (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                sale_date TEXT NOT NULL UNIQUE,
                card_amount INTEGER DEFAULT 0,
                cash_amount INTEGER DEFAULT 0,
                transfer_amount INTEGER DEFAULT 0,
                total_revenue INTEGER DEFAULT 0,
                note TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS sale_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                daily_sales_id INTEGER NOT NULL REFERENCES daily_sales(id),
                reservation_id INTEGER REFERENCES reservations(id),
                description TEXT,
                amount INTEGER NOT NULL,
                payment_type TEXT NOT NULL,
                category TEXT DEFAULT 'revenue',
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);
        conn.Execute("CREATE INDEX IF NOT EXISTS idx_sale_items_daily ON sale_items(daily_sales_id)", transaction: tx);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS cash_balance (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                balance_date TEXT NOT NULL UNIQUE,
                opening_balance INTEGER DEFAULT 0,
                cash_in INTEGER DEFAULT 0,
                cash_out INTEGER DEFAULT 0,
                closing_balance INTEGER DEFAULT 0,
                note TEXT
            )
            """, transaction: tx);
    }
}
