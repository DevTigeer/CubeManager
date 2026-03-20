using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IReservationRepository
{
    /// <summary>날짜별 예약 목록 조회 (DB 저장된 것)</summary>
    Task<IEnumerable<Reservation>> GetByDateAsync(string date);

    /// <summary>예약 Upsert — 날짜+시간+테마+예약자 기준으로 존재하면 업데이트, 없으면 삽입</summary>
    Task<int> UpsertAsync(Reservation reservation);

    /// <summary>예약 상태 변경 (confirmed → removed → confirmed)</summary>
    Task UpdateStatusAsync(int id, string status);

    /// <summary>ID로 조회</summary>
    Task<Reservation?> GetByIdAsync(int id);

    /// <summary>예약 완전 삭제 (DB에서 제거)</summary>
    Task DeleteAsync(int id);
}
