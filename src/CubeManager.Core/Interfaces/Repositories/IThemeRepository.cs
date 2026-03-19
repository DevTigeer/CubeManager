using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IThemeRepository
{
    // 테마 CRUD
    Task<IEnumerable<Theme>> GetAllThemesAsync();
    Task<Theme?> GetThemeByIdAsync(int id);
    Task<int> InsertThemeAsync(Theme theme);
    Task<bool> UpdateThemeAsync(Theme theme);
    Task<bool> DeleteThemeAsync(int id);

    // 힌트 CRUD
    Task<IEnumerable<ThemeHint>> GetHintsByThemeIdAsync(int themeId);
    Task<ThemeHint?> GetHintByIdAsync(int id);
    Task<int> InsertHintAsync(ThemeHint hint);
    Task<bool> UpdateHintAsync(ThemeHint hint);
    Task<bool> DeleteHintAsync(int id);

    // 힌트코드 중복 체크
    Task<bool> IsHintCodeExistsAsync(int themeId, int hintCode, int? excludeId = null);
}
