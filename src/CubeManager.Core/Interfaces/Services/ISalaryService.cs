using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface ISalaryService
{
    Task<IEnumerable<SalaryRecord>> GetMonthlySalaryTableAsync(string yearMonth);
    Task CalculateAllAsync(string yearMonth);
}
