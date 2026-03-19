using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class ThemeRepository : IThemeRepository
{
    private readonly Database _db;
    public ThemeRepository(Database db) => _db = db;

    // === 테마 CRUD ===

    public async Task<IEnumerable<Theme>> GetAllThemesAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Theme>(
            "SELECT id, theme_name, description, sort_order, is_active, created_at, updated_at " +
            "FROM themes ORDER BY sort_order, theme_name");
    }

    public async Task<Theme?> GetThemeByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Theme>(
            "SELECT id, theme_name, description, sort_order, is_active, created_at, updated_at " +
            "FROM themes WHERE id = @id", new { id });
    }

    public async Task<int> InsertThemeAsync(Theme theme)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO themes (theme_name, description, sort_order) " +
            "VALUES (@ThemeName, @Description, @SortOrder); SELECT last_insert_rowid()", theme);
    }

    public async Task<bool> UpdateThemeAsync(Theme theme)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE themes SET theme_name = @ThemeName, description = @Description, " +
            "sort_order = @SortOrder, is_active = @IsActive, " +
            "updated_at = datetime('now','localtime') WHERE id = @Id", theme) > 0;
    }

    public async Task<bool> DeleteThemeAsync(int id)
    {
        using var conn = _db.CreateConnection();
        // CASCADE로 theme_hints도 자동 삭제
        return await conn.ExecuteAsync("DELETE FROM themes WHERE id = @id", new { id }) > 0;
    }

    // === 힌트 CRUD ===

    public async Task<IEnumerable<ThemeHint>> GetHintsByThemeIdAsync(int themeId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ThemeHint>(
            "SELECT id, theme_id, hint_code, question, hint1, hint2, answer, sort_order, created_at, updated_at " +
            "FROM theme_hints WHERE theme_id = @themeId ORDER BY sort_order, hint_code",
            new { themeId });
    }

    public async Task<ThemeHint?> GetHintByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ThemeHint>(
            "SELECT id, theme_id, hint_code, question, hint1, hint2, answer, sort_order, created_at, updated_at " +
            "FROM theme_hints WHERE id = @id", new { id });
    }

    public async Task<int> InsertHintAsync(ThemeHint hint)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO theme_hints (theme_id, hint_code, question, hint1, hint2, answer, sort_order) " +
            "VALUES (@ThemeId, @HintCode, @Question, @Hint1, @Hint2, @Answer, @SortOrder); " +
            "SELECT last_insert_rowid()", hint);
    }

    public async Task<bool> UpdateHintAsync(ThemeHint hint)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE theme_hints SET hint_code = @HintCode, question = @Question, " +
            "hint1 = @Hint1, hint2 = @Hint2, answer = @Answer, sort_order = @SortOrder, " +
            "updated_at = datetime('now','localtime') WHERE id = @Id", hint) > 0;
    }

    public async Task<bool> DeleteHintAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM theme_hints WHERE id = @id", new { id }) > 0;
    }

    public async Task<bool> IsHintCodeExistsAsync(int themeId, int hintCode, int? excludeId = null)
    {
        using var conn = _db.CreateConnection();
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM theme_hints WHERE theme_id = @themeId AND hint_code = @hintCode AND id != @excludeId"
            : "SELECT COUNT(1) FROM theme_hints WHERE theme_id = @themeId AND hint_code = @hintCode";
        return await conn.ExecuteScalarAsync<int>(sql, new { themeId, hintCode, excludeId }) > 0;
    }
}
