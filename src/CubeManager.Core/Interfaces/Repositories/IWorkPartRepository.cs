using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IWorkPartRepository
{
    Task<IEnumerable<WorkPart>> GetActiveAsync();
    Task<IEnumerable<WorkPart>> GetAllAsync();
    Task<int> InsertAsync(WorkPart part);
    Task UpdateAsync(WorkPart part);
    Task DeleteAsync(int id);
}
