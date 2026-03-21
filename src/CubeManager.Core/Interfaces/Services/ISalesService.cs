using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface ISalesService
{
    Task<DailySales?> GetDailySalesAsync(string date);
    Task<IEnumerable<SaleItem>> GetSaleItemsAsync(string date);
    Task AddSaleItemAsync(string date, string description, int amount, string paymentType, string category);
    Task UpsertSaleItemAsync(string date, string description, int amount, string paymentType, string category);
    Task DeleteSaleItemAsync(string date, int itemId);
    Task<CashBalance?> GetCashBalanceAsync(string date);
    /// <summary>해당 날짜의 daily_sales 합계 + cash_balance를 재계산.</summary>
    Task RecalculateTotalsAsync(string date);
}
