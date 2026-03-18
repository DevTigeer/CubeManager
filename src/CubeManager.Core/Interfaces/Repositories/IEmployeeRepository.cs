using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<IEnumerable<Employee>> GetActiveAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<int> InsertAsync(Employee employee);
    Task<bool> UpdateAsync(Employee employee);
    Task<bool> DeactivateAsync(int id);
}
