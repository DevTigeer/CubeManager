using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IAlertLogRepository
{
    Task InsertAsync(AlertLog log);
    Task<IEnumerable<AlertLog>> GetByDateRangeAsync(string startDate, string endDate, string? alertType = null);
    Task<int> GetUnresolvedCountAsync();
    Task ResolveAsync(int id, string resolvedBy);
    Task<bool> ExistsTodayAsync(string alertType, int? employeeId);
    Task<int> GetMonthlyCountAsync(int employeeId, string alertType, string yearMonth);
}
