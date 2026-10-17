using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface IEmployeeService
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<IEnumerable<Employee>> GetActiveAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<int> AddEmployeeAsync(string name, int hourlyWage, string? phone);
    Task<bool> UpdateEmployeeAsync(int id, string name, int hourlyWage, string? phone);
    Task<bool> ToggleActiveAsync(int id, bool isActive);
    Task<bool> DeactivateAsync(int id);
    /// <summary>스케줄/출퇴근/급여/알림 기록이 없는 직원만 삭제. 기록이 있으면 InvalidOperationException.</summary>
    Task<bool> DeleteEmployeeAsync(int id);
}
