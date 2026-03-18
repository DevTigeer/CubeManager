using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface IReservationScraperService
{
    /// <summary>지정 날짜의 예약 데이터를 웹에서 조회</summary>
    Task<IEnumerable<Reservation>> FetchReservationsAsync(DateTime date);

    /// <summary>저장된 자격증명으로 연결 테스트 (로그인 시도)</summary>
    Task<bool> TestConnectionAsync(string id, string password);
}
