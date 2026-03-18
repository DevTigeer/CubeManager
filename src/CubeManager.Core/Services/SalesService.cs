using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;

namespace CubeManager.Core.Services;

public class SalesService : ISalesService
{
    private readonly ISalesRepository _salesRepo;

    public SalesService(ISalesRepository salesRepo) => _salesRepo = salesRepo;

    public Task<DailySales?> GetDailySalesAsync(string date) =>
        _salesRepo.GetDailySalesAsync(date);

    public async Task<IEnumerable<SaleItem>> GetSaleItemsAsync(string date)
    {
        var daily = await _salesRepo.GetDailySalesAsync(date);
        return daily == null
            ? Enumerable.Empty<SaleItem>()
            : await _salesRepo.GetSaleItemsAsync(daily.Id);
    }

    public async Task AddSaleItemAsync(string date, string description, int amount,
        string paymentType, string category)
    {
        if (amount <= 0)
            throw new ArgumentException("금액은 양수여야 합니다.");

        var dailyId = await _salesRepo.EnsureDailySalesAsync(date);
        await _salesRepo.InsertSaleItemAsync(new SaleItem
        {
            DailySalesId = dailyId,
            Description = description,
            Amount = amount,
            PaymentType = paymentType,
            Category = category
        });
        await _salesRepo.UpdateDailySalesTotalsAsync(dailyId);
        await _salesRepo.UpdateCashBalanceAsync(date);
    }

    public async Task DeleteSaleItemAsync(string date, int itemId)
    {
        await _salesRepo.DeleteSaleItemAsync(itemId);
        var daily = await _salesRepo.GetDailySalesAsync(date);
        if (daily != null)
        {
            await _salesRepo.UpdateDailySalesTotalsAsync(daily.Id);
            await _salesRepo.UpdateCashBalanceAsync(date);
        }
    }

    public Task<CashBalance?> GetCashBalanceAsync(string date) =>
        _salesRepo.GetCashBalanceAsync(date);
}
