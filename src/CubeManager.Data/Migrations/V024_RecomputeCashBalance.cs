using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

// 과거 UpdateCashBalanceAsync 버그(전일 행 부재 시 prevClosing=0 처리)로
// 누적 잔액이 끊어져 저장된 cash_balance 행을 일괄 재계산.
public class V024_RecomputeCashBalance : IMigration
{
    public int Version => 24;
    public string Description => "cash_balance 행 일괄 재계산 (carry-forward 보정)";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        var dates = conn.Query<string>(
            "SELECT balance_date FROM cash_balance ORDER BY balance_date ASC",
            transaction: tx).ToList();

        var prevClosing = 0;
        foreach (var date in dates)
        {
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

            var closing = prevClosing + cashIn - cashOut;

            conn.Execute("""
                UPDATE cash_balance
                SET opening_balance = @prevClosing,
                    cash_in = @cashIn,
                    cash_out = @cashOut,
                    closing_balance = @closing
                WHERE balance_date = @date
                """, new { date, prevClosing, cashIn, cashOut, closing }, transaction: tx);

            prevClosing = closing;
        }
    }
}
