using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class SalesRepository : ISalesRepository
{
    private readonly Database _db;

    public SalesRepository(Database db) => _db = db;

    public async Task<DailySales?> GetDailySalesAsync(string date)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<DailySales>(
            "SELECT id, sale_date, card_amount, cash_amount, transfer_amount, total_revenue, note " +
            "FROM daily_sales WHERE sale_date = @date", new { date });
    }

    public async Task<int> EnsureDailySalesAsync(string date)
    {
        using var conn = _db.CreateConnection();
        var existing = await conn.QuerySingleOrDefaultAsync<int?>(
            "SELECT id FROM daily_sales WHERE sale_date = @date", new { date });

        if (existing.HasValue) return existing.Value;

        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO daily_sales (sale_date) VALUES (@date); SELECT last_insert_rowid()",
            new { date });
    }

    public async Task<IEnumerable<SaleItem>> GetSaleItemsAsync(int dailySalesId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<SaleItem>(
            "SELECT id, daily_sales_id, reservation_id, description, amount, payment_type, category, created_at " +
            "FROM sale_items WHERE daily_sales_id = @dailySalesId ORDER BY created_at",
            new { dailySalesId });
    }

    public async Task<int> InsertSaleItemAsync(SaleItem item)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO sale_items (daily_sales_id, reservation_id, description, amount, payment_type, category) " +
            "VALUES (@DailySalesId, @ReservationId, @Description, @Amount, @PaymentType, @Category); " +
            "SELECT last_insert_rowid()", item);
    }

    public async Task<bool> DeleteSaleItemAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM sale_items WHERE id = @id", new { id }) > 0;
    }

    public async Task UpdateDailySalesTotalsAsync(int dailySalesId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("""
            UPDATE daily_sales SET
                card_amount = (SELECT COALESCE(SUM(amount),0) FROM sale_items WHERE daily_sales_id = @id AND payment_type = 'card' AND category = 'revenue'),
                cash_amount = (SELECT COALESCE(SUM(amount),0) FROM sale_items WHERE daily_sales_id = @id AND payment_type = 'cash' AND category = 'revenue'),
                transfer_amount = (SELECT COALESCE(SUM(amount),0) FROM sale_items WHERE daily_sales_id = @id AND payment_type = 'transfer' AND category = 'revenue'),
                total_revenue = (SELECT COALESCE(SUM(CASE WHEN category='revenue' THEN amount ELSE -amount END),0) FROM sale_items WHERE daily_sales_id = @id),
                updated_at = datetime('now','localtime')
            WHERE id = @id
            """, new { id = dailySalesId });
    }

    public async Task<CashBalance?> GetCashBalanceAsync(string date)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<CashBalance>(
            "SELECT id, balance_date, opening_balance, cash_in, cash_out, closing_balance, note " +
            "FROM cash_balance WHERE balance_date = @date", new { date });
    }

    public async Task UpdateCashBalanceAsync(string date)
    {
        using var conn = _db.CreateConnection();

        // 전일 마감 잔액 조회
        var prevDate = DateTime.Parse(date).AddDays(-1).ToString("yyyy-MM-dd");
        var prevClosing = await conn.ExecuteScalarAsync<int?>(
            "SELECT closing_balance FROM cash_balance WHERE balance_date = @prevDate",
            new { prevDate }) ?? 0;

        // 당일 현금 수입/지출
        var dailySales = await conn.QuerySingleOrDefaultAsync<int?>(
            "SELECT id FROM daily_sales WHERE sale_date = @date", new { date });

        int cashIn = 0, cashOut = 0;
        if (dailySales.HasValue)
        {
            cashIn = await conn.ExecuteScalarAsync<int>(
                "SELECT COALESCE(SUM(amount),0) FROM sale_items WHERE daily_sales_id = @id AND payment_type = 'cash' AND category = 'revenue'",
                new { id = dailySales.Value });
            cashOut = await conn.ExecuteScalarAsync<int>(
                "SELECT COALESCE(SUM(amount),0) FROM sale_items WHERE daily_sales_id = @id AND category = 'expense' AND payment_type = 'cash'",
                new { id = dailySales.Value });
        }

        var closing = prevClosing + cashIn - cashOut;

        await conn.ExecuteAsync("""
            INSERT INTO cash_balance (balance_date, opening_balance, cash_in, cash_out, closing_balance)
            VALUES (@date, @prevClosing, @cashIn, @cashOut, @closing)
            ON CONFLICT(balance_date) DO UPDATE SET
                opening_balance = @prevClosing, cash_in = @cashIn,
                cash_out = @cashOut, closing_balance = @closing
            """, new { date, prevClosing, cashIn, cashOut, closing });
    }
}
