using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IMicePopupRepository
{
    Task<IEnumerable<MicePopup>> GetAllAsync();
    Task<IEnumerable<MicePopup>> GetActiveAsync();
    Task<int> InsertAsync(MicePopup popup);
    Task UpdateAsync(MicePopup popup);
    Task DeleteAsync(int id);
    Task UpdateLastShownAsync(int id, string dateTime);
}
