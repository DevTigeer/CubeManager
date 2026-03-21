using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IFreePassRepository
{
    /// <summary>전체 목록 (최신 발급순)</summary>
    Task<IEnumerable<FreePass>> GetAllAsync();

    /// <summary>월별 조회 (issued_date 기준)</summary>
    Task<IEnumerable<FreePass>> GetByMonthAsync(string yearMonth);

    /// <summary>다음 무료번호 조회 (A2000부터 순차)</summary>
    Task<string> GetNextPassNumberAsync();

    /// <summary>발급 (INSERT)</summary>
    Task<int> InsertAsync(FreePass pass);

    /// <summary>사용 처리 (used_date = today, is_used = 1)</summary>
    Task MarkUsedAsync(int id);

    /// <summary>삭제</summary>
    Task DeleteAsync(int id);
}
