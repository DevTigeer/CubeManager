using System.Data;
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

    public async Task UpsertSaleItemByDescAsync(int dailySalesId, string description, int amount, string paymentType, string category)
    {
        using var conn = _db.CreateConnection();

        // 같은 일자 + 같은 설명의 기존 항목 찾기
        var existing = await conn.QuerySingleOrDefaultAsync<int?>(
            "SELECT id FROM sale_items WHERE daily_sales_id = @dailySalesId AND description = @description AND payment_type = @paymentType AND category = @category",
            new { dailySalesId, description, paymentType, category });

        if (existing.HasValue)
        {
            // 기존 항목 금액 업데이트
            await conn.ExecuteAsync(
                "UPDATE sale_items SET amount = @amount WHERE id = @id",
                new { amount, id = existing.Value });
        }
        else
        {
            // 새 항목 추가
            await conn.ExecuteAsync(
                "INSERT INTO sale_items (daily_sales_id, description, amount, payment_type, category) " +
                "VALUES (@dailySalesId, @description, @amount, @paymentType, @category)",
                new { dailySalesId, description, amount, paymentType, category });
        }
    }

    public async Task<bool> DeleteSaleItemAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM sale_items WHERE id = @id", new { id }) > 0;
    }

    public async Task<bool> UpdateSaleItemAsync(int id, string description, int amount, string paymentType)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE sale_items SET description = @description, amount = @amount, payment_type = @paymentType WHERE id = @id",
            new { id, description, amount, paymentType }) > 0;
    }

    public async Task<int> DeleteSaleItemByDescAsync(int dailySalesId, string description, string paymentType, string category)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(
            "DELETE FROM sale_items WHERE daily_sales_id = @dailySalesId AND description = @description AND payment_type = @paymentType AND category = @category",
            new { dailySalesId, description, paymentType, category });
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

    // 행이 없어도 carry-forward로 합성된 잔액을 반환 (조회 전용, DB 쓰기 없음)
    public async Task<CashBalance> GetEffectiveCashBalanceAsync(string date)
    {
        using var conn = _db.CreateConnection();

        var stored = await conn.QuerySingleOrDefaultAsync<CashBalance>(
            "SELECT id, balance_date, opening_balance, cash_in, cash_out, closing_balance, note " +
            "FROM cash_balance WHERE balance_date = @date", new { date });
        if (stored != null) return stored;

        // 직전(과거) 행에서 carry-forward
        var prevClosing = await conn.ExecuteScalarAsync<int?>(
            "SELECT closing_balance FROM cash_balance WHERE balance_date < @date ORDER BY balance_date DESC LIMIT 1",
            new { date }) ?? 0;

        // 당일 sale_items로 현금 수입/지출 계산 (행이 없어도 sale_items만 존재할 가능성 대비)
        var (cashIn, cashOut) = await ComputeCashFlowsAsync(conn, null, date);
        return new CashBalance
        {
            BalanceDate = date,
            OpeningBalance = prevClosing,
            CashIn = cashIn,
            CashOut = cashOut,
            ClosingBalance = prevClosing + cashIn - cashOut
        };
    }

    public async Task UpdateCashBalanceAsync(string date)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        // 1) 대상일 재계산
        await RecomputeOneAsync(conn, tx, date);

        // 2) 대상일 이후 모든 행 연쇄 재계산 (과거 수정 시 미래 잔액 stale 방지)
        var futureDates = (await conn.QueryAsync<string>(
            "SELECT balance_date FROM cash_balance WHERE balance_date > @date ORDER BY balance_date ASC",
            new { date }, transaction: tx)).ToList();

        foreach (var d in futureDates)
            await RecomputeOneAsync(conn, tx, d);

        tx.Commit();
    }

    private static async Task RecomputeOneAsync(IDbConnection conn, IDbTransaction tx, string date)
    {
        // 전일이 아닌 "직전 가장 최근 행"에서 carry-forward (거래 없는 날 건너뛰기)
        var prevClosing = await conn.ExecuteScalarAsync<int?>(
            "SELECT closing_balance FROM cash_balance WHERE balance_date < @date ORDER BY balance_date DESC LIMIT 1",
            new { date }, transaction: tx) ?? 0;

        var (cashIn, cashOut) = await ComputeCashFlowsAsync(conn, tx, date);
        var closing = prevClosing + cashIn - cashOut;

        await conn.ExecuteAsync("""
            INSERT INTO cash_balance (balance_date, opening_balance, cash_in, cash_out, closing_balance)
            VALUES (@date, @prevClosing, @cashIn, @cashOut, @closing)
            ON CONFLICT(balance_date) DO UPDATE SET
                opening_balance = @prevClosing, cash_in = @cashIn,
                cash_out = @cashOut, closing_balance = @closing
            """, new { date, prevClosing, cashIn, cashOut, closing }, transaction: tx);
    }

    private static async Task<(int cashIn, int cashOut)> ComputeCashFlowsAsync(
        IDbConnection conn, IDbTransaction? tx, string date)
    {
        var dailyId = await conn.ExecuteScalarAsync<int?>(
            "SELECT id FROM daily_sales WHERE sale_date = @date",
            new { date }, transaction: tx);
        if (!dailyId.HasValue) return (0, 0);

        var cashIn = await conn.ExecuteScalarAsync<int>(
            "SELECT COALESCE(SUM(amount),0) FROM sale_items WHERE daily_sales_id = @id AND payment_type = 'cash' AND category = 'revenue'",
            new { id = dailyId.Value }, transaction: tx);
        var cashOut = await conn.ExecuteScalarAsync<int>(
            "SELECT COALESCE(SUM(amount),0) FROM sale_items WHERE daily_sales_id = @id AND payment_type = 'cash' AND category = 'expense'",
            new { id = dailyId.Value }, transaction: tx);
        return (cashIn, cashOut);
    }
}
