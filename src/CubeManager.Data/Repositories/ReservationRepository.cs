using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly Database _db;

    public ReservationRepository(Database db) => _db = db;

    public async Task<IEnumerable<Reservation>> GetByDateAsync(string date)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Reservation>(
            "SELECT id, reservation_date, time_slot, theme_name, customer_name, customer_phone, headcount, status, synced_at " +
            "FROM reservations WHERE reservation_date = @date ORDER BY time_slot",
            new { date });
    }

    public async Task<int> UpsertAsync(Reservation r)
    {
        using var conn = _db.CreateConnection();

        // 복합키로 기존 예약 찾기
        var existing = await conn.QuerySingleOrDefaultAsync<Reservation>(
            "SELECT id, status FROM reservations " +
            "WHERE reservation_date = @ReservationDate AND time_slot = @TimeSlot " +
            "AND theme_name = @ThemeName AND customer_name = @CustomerName",
            r);

        if (existing != null)
        {
            // 기존 예약: 인원/연락처만 업데이트, 상태는 보존 (사용자가 변경한 것 유지)
            await conn.ExecuteAsync(
                "UPDATE reservations SET headcount = @Headcount, customer_phone = @CustomerPhone, " +
                "synced_at = @SyncedAt WHERE id = @Id",
                new { r.Headcount, r.CustomerPhone, r.SyncedAt, existing.Id });
            return existing.Id;
        }

        // 새 예약 삽입
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO reservations (reservation_date, time_slot, theme_name, customer_name, customer_phone, headcount, status, synced_at) " +
            "VALUES (@ReservationDate, @TimeSlot, @ThemeName, @CustomerName, @CustomerPhone, @Headcount, @Status, @SyncedAt); " +
            "SELECT last_insert_rowid()", r);
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE reservations SET status = @status WHERE id = @id",
            new { id, status });
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Reservation>(
            "SELECT id, reservation_date, time_slot, theme_name, customer_name, customer_phone, headcount, status, synced_at " +
            "FROM reservations WHERE id = @id",
            new { id });
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM reservations WHERE id = @id", new { id });
    }
}
