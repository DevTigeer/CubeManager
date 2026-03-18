using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IInventoryRepository
{
    Task<IEnumerable<InventoryItem>> GetAllAsync(string? category = null);
    Task<int> InsertAsync(InventoryItem item);
    Task<bool> UpdateQuantityAsync(int id, int currentQty);
    Task<bool> UpdateAsync(InventoryItem item);
    Task<bool> DeleteAsync(int id);
}
