using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface ISalesRepository
{
    Task<DailySales?> GetDailySalesAsync(string date);
    Task<int> EnsureDailySalesAsync(string date);
    Task<IEnumerable<SaleItem>> GetSaleItemsAsync(int dailySalesId);
    Task<int> InsertSaleItemAsync(SaleItem item);
    Task<bool> DeleteSaleItemAsync(int id);
    Task UpdateDailySalesTotalsAsync(int dailySalesId);
    Task<CashBalance?> GetCashBalanceAsync(string date);
    Task UpdateCashBalanceAsync(string date);
}
