using System.Data;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class SalesRepository : ISalesRepository
{
    private readonly Database _db;

    // [임시] 현금잔액 추적 시작일 — 이 날짜 이전 cash_balance는 무시.
    // 기준일 opening은 DB에 저장된 값을 그대로 보존(carry-forward 재계산 대상에서 제외).
    private const string CashRefDate = "2026-05-02";

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

        // 기준일 이전: 추적 안 함 → opening=0, 당일 거래만 표시
        if (string.CompareOrdinal(date, CashRefDate) < 0)
        {
            var (preIn, preOut) = await ComputeCashFlowsAsync(conn, null, date);
            return new CashBalance
            {
                BalanceDate = date,
                OpeningBalance = 0,
                CashIn = preIn,
                CashOut = preOut,
                ClosingBalance = preIn - preOut
            };
        }

        var stored = await conn.QuerySingleOrDefaultAsync<CashBalance>(
            "SELECT id, balance_date, opening_balance, cash_in, cash_out, closing_balance, note " +
            "FROM cash_balance WHERE balance_date = @date", new { date });
        if (stored != null) return stored;

        var prevClosing = await GetOpeningBalanceAsync(conn, null, date);
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
        // 기준일 이전은 cash_balance 추적 대상에서 제외
        if (string.CompareOrdinal(date, CashRefDate) < 0) return;

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
        if (string.CompareOrdinal(date, CashRefDate) < 0) return;

        var (cashIn, cashOut) = await ComputeCashFlowsAsync(conn, tx, date);

        // 기준일은 opening 보존 (DB에 저장된 값 그대로). 그 이후는 carry-forward.
        if (date == CashRefDate)
        {
            // 행이 있으면 cash_in/out/closing만 갱신, 없으면 opening=0으로 새 행 (현장에서 직접 보정)
            await conn.ExecuteAsync("""
                INSERT INTO cash_balance (balance_date, opening_balance, cash_in, cash_out, closing_balance)
                VALUES (@date, 0, @cashIn, @cashOut, @cashIn - @cashOut)
                ON CONFLICT(balance_date) DO UPDATE SET
                    cash_in = @cashIn,
                    cash_out = @cashOut,
                    closing_balance = opening_balance + @cashIn - @cashOut
                """, new { date, cashIn, cashOut }, transaction: tx);
            return;
        }

        var opening = await GetOpeningBalanceAsync(conn, tx, date);
        var closing = opening + cashIn - cashOut;

        await conn.ExecuteAsync("""
            INSERT INTO cash_balance (balance_date, opening_balance, cash_in, cash_out, closing_balance)
            VALUES (@date, @opening, @cashIn, @cashOut, @closing)
            ON CONFLICT(balance_date) DO UPDATE SET
                opening_balance = @opening, cash_in = @cashIn,
                cash_out = @cashOut, closing_balance = @closing
            """, new { date, opening, cashIn, cashOut, closing }, transaction: tx);
    }

    // 기준일 이후의 carry-forward opening 계산 (기준일 자체는 보존이라 별도 처리)
    private static async Task<int> GetOpeningBalanceAsync(IDbConnection conn, IDbTransaction? tx, string date)
    {
        var cmp = string.CompareOrdinal(date, CashRefDate);
        if (cmp < 0) return 0;
        if (cmp == 0)
        {
            // 기준일 자신의 opening은 DB 저장값을 그대로 사용 (없으면 0)
            return await conn.ExecuteScalarAsync<int?>(
                "SELECT opening_balance FROM cash_balance WHERE balance_date = @date",
                new { date }, transaction: tx) ?? 0;
        }

        // 기준일~전일 범위에서 가장 최근 마감액
        var prev = await conn.ExecuteScalarAsync<int?>(
            "SELECT closing_balance FROM cash_balance " +
            "WHERE balance_date < @date AND balance_date >= @refDate " +
            "ORDER BY balance_date DESC LIMIT 1",
            new { date, refDate = CashRefDate }, transaction: tx);
        return prev ?? 0;
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
