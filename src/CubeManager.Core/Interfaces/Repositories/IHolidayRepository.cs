using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IHolidayRepository
{
    Task<IEnumerable<Holiday>> GetByYearAsync(int year);
    Task<bool> IsHolidayAsync(string date);

    /// <summary>평일(월~금) 공휴일인지 확인</summary>
    Task<bool> IsWeekdayHolidayAsync(string date);

    Task<IEnumerable<Holiday>> GetByMonthAsync(string yearMonth);

    /// <summary>공휴일 Upsert (holiday_date 기준 중복 무시)</summary>
    Task UpsertHolidaysAsync(IEnumerable<Holiday> holidays);

    /// <summary>특정 연도의 공휴일 수</summary>
    Task<int> GetCountByYearAsync(int year);
}
