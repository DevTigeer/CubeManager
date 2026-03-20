using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface IHolidayService
{
    /// <summary>
    /// 공공데이터포털 API에서 해당 연도의 공휴일을 가져와 DB에 저장.
    /// 이미 해당 연도 데이터가 충분하면 스킵.
    /// </summary>
    Task<int> SyncHolidaysAsync(int year);

    /// <summary>해당 연도 공휴일 목록 조회 (DB 기준)</summary>
    Task<IEnumerable<Holiday>> GetHolidaysAsync(int year);

    /// <summary>평일 공휴일인지 확인</summary>
    Task<bool> IsWeekdayHolidayAsync(string date);
}
