using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface IAlertService
{
    /// <summary>체크리스트 미완료 검사 (출근 후 1시간)</summary>
    Task CheckChecklistDelayAsync();

    /// <summary>인수인계 미확인 검사 (출근 후 30분)</summary>
    Task CheckHandoverUnreadAsync();

    /// <summary>무단결근 검사 (12시 체크)</summary>
    Task CheckNoShowAsync();

    /// <summary>지각 누적 경고 (월 3회 이상)</summary>
    Task CheckLateAccumulateAsync();

    /// <summary>미해결 알림 건수</summary>
    Task<int> GetUnresolvedCountAsync();

    /// <summary>알림 이력 조회</summary>
    Task<IEnumerable<AlertLog>> GetAlertHistoryAsync(string startDate, string endDate, string? alertType = null);

    /// <summary>알림 해결 처리</summary>
    Task ResolveAlertAsync(int alertId, string resolvedBy);
}
