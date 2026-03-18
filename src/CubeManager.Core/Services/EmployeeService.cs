using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;

namespace CubeManager.Core.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepo;

    public EmployeeService(IEmployeeRepository employeeRepo)
    {
        _employeeRepo = employeeRepo;
    }

    public Task<IEnumerable<Employee>> GetAllAsync() =>
        _employeeRepo.GetAllAsync();

    public Task<IEnumerable<Employee>> GetActiveAsync() =>
        _employeeRepo.GetActiveAsync();

    public Task<Employee?> GetByIdAsync(int id) =>
        _employeeRepo.GetByIdAsync(id);

    public async Task<int> AddEmployeeAsync(string name, int hourlyWage, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("이름은 필수입니다.", nameof(name));
        if (hourlyWage < 0)
            throw new ArgumentException("시급은 0 이상이어야 합니다.", nameof(hourlyWage));

        var employee = new Employee
        {
            Name = name.Trim(),
            HourlyWage = hourlyWage,
            IsActive = true,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
        };

        return await _employeeRepo.InsertAsync(employee);
    }

    public async Task<bool> UpdateEmployeeAsync(int id, string name, int hourlyWage, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("이름은 필수입니다.", nameof(name));
        if (hourlyWage < 0)
            throw new ArgumentException("시급은 0 이상이어야 합니다.", nameof(hourlyWage));

        var existing = await _employeeRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"직원을 찾을 수 없습니다: ID={id}");

        existing.Name = name.Trim();
        existing.HourlyWage = hourlyWage;
        existing.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        return await _employeeRepo.UpdateAsync(existing);
    }

    public async Task<bool> ToggleActiveAsync(int id, bool isActive)
    {
        var existing = await _employeeRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"직원을 찾을 수 없습니다: ID={id}");

        existing.IsActive = isActive;
        return await _employeeRepo.UpdateAsync(existing);
    }

    public Task<bool> DeactivateAsync(int id) =>
        _employeeRepo.DeactivateAsync(id);
}
