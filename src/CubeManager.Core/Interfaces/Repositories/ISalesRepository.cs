using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface ISalesRepository
{
    Task<DailySales?> GetDailySalesAsync(string date);
    Task<int> EnsureDailySalesAsync(string date);
    Task<IEnumerable<SaleItem>> GetSaleItemsAsync(int dailySalesId);
    Task<int> InsertSaleItemAsync(SaleItem item);
    Task UpsertSaleItemByDescAsync(int dailySalesId, string description, int amount, string paymentType, string category);
    Task<bool> DeleteSaleItemAsync(int id);
    Task<bool> UpdateSaleItemAsync(int id, string description, int amount, string paymentType);
    Task<int> DeleteSaleItemByDescAsync(int dailySalesId, string description, string paymentType, string category);
    Task UpdateDailySalesTotalsAsync(int dailySalesId);
    Task<CashBalance?> GetCashBalanceAsync(string date);
    /// <summary>행이 없으면 직전 carry-forward + 당일 sale_items로 합성된 잔액을 반환 (DB 쓰기 없음).</summary>
    Task<CashBalance> GetEffectiveCashBalanceAsync(string date);
    Task UpdateCashBalanceAsync(string date);
}
