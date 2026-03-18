using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface ISalaryRepository
{
    Task<IEnumerable<SalaryRecord>> GetByMonthAsync(string yearMonth);
    Task<SalaryRecord?> GetByEmployeeMonthAsync(int employeeId, string yearMonth);
    Task UpsertAsync(SalaryRecord record);
}
