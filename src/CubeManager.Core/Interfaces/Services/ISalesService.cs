using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface ISalesService
{
    Task<DailySales?> GetDailySalesAsync(string date);
    Task<IEnumerable<SaleItem>> GetSaleItemsAsync(string date);
    Task AddSaleItemAsync(string date, string description, int amount, string paymentType, string category);
    Task UpsertSaleItemAsync(string date, string description, int amount, string paymentType, string category);
    Task DeleteSaleItemAsync(string date, int itemId);
    /// <summary>id로 sale_item을 직접 수정하고 totals/cash를 재계산.</summary>
    Task UpdateSaleItemAsync(string date, int itemId, string description, int amount, string paymentType);
    /// <summary>설명+결제수단+카테고리로 매칭되는 sale_item을 삭제하고 totals/cash를 재계산.</summary>
    Task<bool> RemoveSaleItemByDescAsync(string date, string description, string paymentType, string category);
    Task<CashBalance?> GetCashBalanceAsync(string date);
    /// <summary>cash_balance 행이 없으면 직전 carry-forward 기반 합성 잔액을 반환 (조회 전용).</summary>
    Task<CashBalance> GetEffectiveCashBalanceAsync(string date);
    /// <summary>해당 날짜의 daily_sales 합계 + cash_balance를 재계산.</summary>
    Task RecalculateTotalsAsync(string date);
}
