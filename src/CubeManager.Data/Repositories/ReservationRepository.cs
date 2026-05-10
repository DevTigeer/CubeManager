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
            "SELECT id, web_reservation_id, reservation_date, time_slot, theme_name, customer_name, customer_phone, headcount, status, note, is_verified, raw_html, synced_at " +
            "FROM reservations WHERE reservation_date = @date ORDER BY time_slot",
            new { date });
    }

    public async Task<int> UpsertAsync(Reservation r)
    {
        using var conn = _db.CreateConnection();

        Reservation? existing = null;

        if (!string.IsNullOrWhiteSpace(r.WebReservationId))
        {
            existing = await conn.QuerySingleOrDefaultAsync<Reservation>(
                "SELECT id, status, note FROM reservations WHERE web_reservation_id = @WebReservationId",
                r);
        }

        // 과거 데이터나 웹 예약번호가 없는 행은 기존 복합키로 fallback 매칭한다.
        existing ??= await conn.QuerySingleOrDefaultAsync<Reservation>(
            "SELECT id, status, note FROM reservations " +
            "WHERE reservation_date = @ReservationDate AND time_slot = @TimeSlot " +
            "AND theme_name = @ThemeName AND customer_name = @CustomerName",
            r);

        if (existing != null)
        {
            // 기존 예약: 웹 원본값은 최신화, 상태/비고는 보존한다.
            await conn.ExecuteAsync(
                "UPDATE reservations SET web_reservation_id = COALESCE(@WebReservationId, web_reservation_id), " +
                "reservation_date = @ReservationDate, time_slot = @TimeSlot, theme_name = @ThemeName, " +
                "customer_name = @CustomerName, customer_phone = @CustomerPhone, headcount = @Headcount, " +
                "raw_html = @RawHtml, synced_at = @SyncedAt WHERE id = @Id",
                new
                {
                    r.WebReservationId,
                    r.ReservationDate,
                    r.TimeSlot,
                    r.ThemeName,
                    r.CustomerName,
                    r.CustomerPhone,
                    r.Headcount,
                    r.RawHtml,
                    r.SyncedAt,
                    existing.Id
                });
            return existing.Id;
        }

        // 새 예약 삽입
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO reservations (web_reservation_id, reservation_date, time_slot, theme_name, customer_name, customer_phone, headcount, status, note, raw_html, synced_at) " +
            "VALUES (@WebReservationId, @ReservationDate, @TimeSlot, @ThemeName, @CustomerName, @CustomerPhone, @Headcount, @Status, @Note, @RawHtml, @SyncedAt); " +
            "SELECT last_insert_rowid()", r);
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE reservations SET status = @status WHERE id = @id",
            new { id, status });
    }

    public async Task UpdateNoteAsync(int id, string? note)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE reservations SET note = @note WHERE id = @id",
            new { id, note });
    }

    public async Task UpdateVerifiedAsync(int id, bool isVerified)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE reservations SET is_verified = @flag WHERE id = @id",
            new { id, flag = isVerified ? 1 : 0 });
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Reservation>(
            "SELECT id, web_reservation_id, reservation_date, time_slot, theme_name, customer_name, customer_phone, headcount, status, note, is_verified, raw_html, synced_at " +
            "FROM reservations WHERE id = @id",
            new { id });
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM reservations WHERE id = @id", new { id });
    }
}
