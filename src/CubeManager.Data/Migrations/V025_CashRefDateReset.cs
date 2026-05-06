using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

// [임시] 현금잔액 추적 시작일을 2026-05-02로 클램프.
// - 기준일 이전의 cash_balance 행은 삭제
// - 기준일 행의 opening_balance는 DB 저장값을 그대로 보존
// - 기준일 이후 행은 carry-forward로 일괄 재계산
public class V025_CashRefDateReset : IMigration
{
    private const string CashRefDate = "2026-05-02";

    public int Version => 25;
    public string Description => $"cash_balance 기준일({CashRefDate}) 클램프 + 재계산 (opening 보존)";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        // 1) 기준일 이전 행 삭제 (sale_items / daily_sales는 보존)
        conn.Execute(
            "DELETE FROM cash_balance WHERE balance_date < @refDate",
            new { refDate = CashRefDate }, transaction: tx);

        // 2) 기준일 이후 행 일괄 재계산 (기준일 opening은 DB 저장값 그대로 보존)
        var dates = conn.Query<string>(
            "SELECT balance_date FROM cash_balance WHERE balance_date >= @refDate ORDER BY balance_date ASC",
            new { refDate = CashRefDate }, transaction: tx).ToList();

        int? prevClosing = null;
        foreach (var date in dates)
        {
            // 당일 cash_in / cash_out 재집계
            var dailyId = conn.ExecuteScalar<int?>(
                "SELECT id FROM daily_sales WHERE sale_date = @date",
                new { date }, transaction: tx);

            int cashIn = 0, cashOut = 0;
            if (dailyId.HasValue)
            {
                cashIn = conn.ExecuteScalar<int>(
                    "SELECT COALESCE(SUM(amount),0) FROM sale_items WHERE daily_sales_id = @id AND payment_type = 'cash' AND category = 'revenue'",
                    new { id = dailyId.Value }, transaction: tx);
                cashOut = conn.ExecuteScalar<int>(
                    "SELECT COALESCE(SUM(amount),0) FROM sale_items WHERE daily_sales_id = @id AND payment_type = 'cash' AND category = 'expense'",
                    new { id = dailyId.Value }, transaction: tx);
            }

            int opening;
            if (date == CashRefDate)
            {
                // 기준일: 기존 opening_balance 보존
                opening = conn.ExecuteScalar<int?>(
                    "SELECT opening_balance FROM cash_balance WHERE balance_date = @date",
                    new { date }, transaction: tx) ?? 0;
            }
            else
            {
                // 기준일 이후: 직전 마감액 carry-forward (없으면 0 — 기준일 행이 없는 경우)
                opening = prevClosing ?? 0;
            }

            var closing = opening + cashIn - cashOut;

            conn.Execute("""
                UPDATE cash_balance
                SET opening_balance = @opening,
                    cash_in = @cashIn,
                    cash_out = @cashOut,
                    closing_balance = @closing
                WHERE balance_date = @date
                """, new { date, opening, cashIn, cashOut, closing }, transaction: tx);

            prevClosing = closing;
        }
    }
}
