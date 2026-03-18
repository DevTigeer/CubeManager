using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IHolidayRepository
{
    Task<IEnumerable<Holiday>> GetByYearAsync(int year);
    Task<bool> IsHolidayAsync(string date);

    /// <summary>평일(월~금) 공휴일인지 확인</summary>
    Task<bool> IsWeekdayHolidayAsync(string date);

    Task<IEnumerable<Holiday>> GetByMonthAsync(string yearMonth);
}
