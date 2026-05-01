using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using Serilog;

namespace CubeManager.Core.Services;

public class ThemeExportService : IThemeExportService
{
    private readonly IThemeRepository _themeRepo;
    private static readonly Regex HintKeyPattern = new(@"^[^0-9]*[0-9]{1,4}$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 한글 유니코드 이스케이프 방지
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ThemeExportService(IThemeRepository themeRepo)
    {
        _themeRepo = themeRepo;
    }

    public async Task ExportAllToJsonAsync(string filePath)
    {
        var themes = await _themeRepo.GetAllThemesAsync();
        var exportData = new Dictionary<string, OriginalTheme>();

        foreach (var theme in themes)
        {
            if (!theme.IsActive) continue;

            var hints = await _themeRepo.GetHintsByThemeIdAsync(theme.Id);
            var themeKey = ResolveThemeKey(theme);
            exportData[themeKey] = ToOriginalTheme(theme, themeKey, hints);
        }

        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        Log.Information("테마 JSON Export 완료: {Path} ({Count}개 테마)", filePath, exportData.Count);
    }

    public async Task ExportThemeToJsonAsync(int themeId, string filePath)
    {
        var theme = await _themeRepo.GetThemeByIdAsync(themeId);
        if (theme == null) throw new ArgumentException($"테마 ID {themeId}를 찾을 수 없습니다.");

        var hints = await _themeRepo.GetHintsByThemeIdAsync(themeId);
        var themeKey = ResolveThemeKey(theme);
        var exportData = new Dictionary<string, OriginalTheme>
        {
            [themeKey] = ToOriginalTheme(theme, themeKey, hints)
        };

        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        Log.Information("테마 JSON Export 완료: {Path} ({Theme})", filePath, theme.ThemeName);
    }

    public async Task<ThemeImportResult> ImportOriginalJsonAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var importData = JsonSerializer.Deserialize<Dictionary<string, OriginalTheme>>(json, JsonOptions)
                         ?? throw new InvalidOperationException("JSON 형식을 읽을 수 없습니다.");
        ValidateImportData(importData);

        var existingThemes = (await _themeRepo.GetAllThemesAsync()).ToList();
        var importedThemes = 0;
        var importedHints = 0;
        var sortOrder = existingThemes.Count == 0 ? 0 : existingThemes.Max(t => t.SortOrder);

        foreach (var (rootKey, sourceTheme) in importData)
        {
            var themeKey = string.IsNullOrWhiteSpace(sourceTheme.Key) ? rootKey : sourceTheme.Key.Trim();
            var themeName = string.IsNullOrWhiteSpace(sourceTheme.Name) ? themeKey : sourceTheme.Name.Trim();
            var theme = existingThemes.FirstOrDefault(t =>
                string.Equals(t.ThemeKey, themeKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.ThemeName, themeName, StringComparison.OrdinalIgnoreCase));

            if (theme == null)
            {
                theme = new Theme
                {
                    ThemeKey = themeKey,
                    ThemeName = themeName,
                    Description = sourceTheme.Description,
                    BgColor = sourceTheme.Bg,
                    AccentColor = sourceTheme.Accent,
                    Icon = sourceTheme.Icon,
                    CodePrefix = sourceTheme.CodePrefix,
                    SortOrder = ++sortOrder,
                    IsActive = true
                };
                theme.Id = await _themeRepo.InsertThemeAsync(theme);
                existingThemes.Add(theme);
            }
            else
            {
                theme.ThemeKey = themeKey;
                theme.ThemeName = themeName;
                theme.Description = sourceTheme.Description;
                theme.BgColor = sourceTheme.Bg;
                theme.AccentColor = sourceTheme.Accent;
                theme.Icon = sourceTheme.Icon;
                theme.CodePrefix = sourceTheme.CodePrefix;
                theme.IsActive = true;
                await _themeRepo.UpdateThemeAsync(theme);
            }

            importedThemes++;

            var existingHints = (await _themeRepo.GetHintsByThemeIdAsync(theme.Id)).ToList();
            var hintOrder = 0;
            foreach (var (hintKey, sourceHint) in sourceTheme.HintMap ?? new Dictionary<string, OriginalHint>())
            {
                var hintCode = ParseHintCode(hintKey);
                var hint = existingHints.FirstOrDefault(h => h.HintCode == hintCode);
                if (hint == null)
                {
                    await _themeRepo.InsertHintAsync(new ThemeHint
                    {
                        ThemeId = theme.Id,
                        HintCode = hintCode,
                        Question = sourceHint.Idea ?? "",
                        Hint1 = sourceHint.Solution ?? "",
                        Hint2 = null,
                        Answer = sourceHint.Answer ?? "",
                        SortOrder = ++hintOrder
                    });
                }
                else
                {
                    hint.Question = sourceHint.Idea ?? "";
                    hint.Hint1 = sourceHint.Solution ?? "";
                    hint.Answer = sourceHint.Answer ?? "";
                    hint.SortOrder = ++hintOrder;
                    await _themeRepo.UpdateHintAsync(hint);
                }

                importedHints++;
            }
        }

        Log.Information("테마 JSON Import 완료: {Path} ({Themes}개 테마, {Hints}개 힌트)",
            filePath, importedThemes, importedHints);
        return new ThemeImportResult { ThemeCount = importedThemes, HintCount = importedHints };
    }

    private static OriginalTheme ToOriginalTheme(
        Theme theme,
        string themeKey,
        IEnumerable<ThemeHint> hints)
    {
        var prefix = string.IsNullOrWhiteSpace(theme.CodePrefix)
            ? (string.IsNullOrWhiteSpace(themeKey) ? "h" : themeKey[..Math.Min(themeKey.Length, 1)])
            : theme.CodePrefix;

        var hintMap = new Dictionary<string, OriginalHint>();
        foreach (var hint in hints.OrderBy(h => h.SortOrder).ThenBy(h => h.HintCode))
        {
            var hintKey = $"{prefix}{hint.HintCode:D4}";
            hintMap[hintKey] = new OriginalHint
            {
                Idea = hint.Question,
                Solution = hint.Hint1,
                Answer = hint.Answer
            };
        }

        return new OriginalTheme
        {
            Key = themeKey,
            Name = theme.ThemeName,
            Description = theme.Description,
            Bg = theme.BgColor,
            Accent = theme.AccentColor,
            Icon = theme.Icon,
            CodePrefix = prefix,
            HintMap = hintMap
        };
    }

    private static string ResolveThemeKey(Theme theme)
    {
        if (!string.IsNullOrWhiteSpace(theme.ThemeKey))
            return theme.ThemeKey;

        return theme.ThemeName.Trim().Replace(" ", "_", StringComparison.Ordinal).ToLowerInvariant();
    }

    private static void ValidateImportData(IDictionary<string, OriginalTheme> importData)
    {
        foreach (var (rootKey, sourceTheme) in importData)
        {
            if (sourceTheme == null)
                throw new InvalidOperationException($"테마 '{rootKey}' 데이터가 비어 있습니다.");

            var seenCodes = new HashSet<int>();
            foreach (var hintKey in sourceTheme.HintMap?.Keys ?? Enumerable.Empty<string>())
            {
                var code = ParseHintCode(hintKey);
                if (!seenCodes.Add(code))
                    throw new InvalidOperationException($"테마 '{rootKey}'에 중복 힌트 코드가 있습니다: {hintKey}");
            }
        }
    }

    private static int ParseHintCode(string hintKey)
    {
        if (string.IsNullOrWhiteSpace(hintKey) || !HintKeyPattern.IsMatch(hintKey))
            throw new InvalidOperationException($"힌트 코드 형식이 올바르지 않습니다: {hintKey}");

        var digits = new string(hintKey.Where(c => c is >= '0' and <= '9').ToArray());
        if (!int.TryParse(digits, out var code) || code < 1 || code > 9999)
            throw new InvalidOperationException($"힌트 코드 범위가 올바르지 않습니다: {hintKey}");

        return code;
    }

    // === Export DTO ===

    private sealed class OriginalTheme
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Bg { get; set; }
        public string? Accent { get; set; }
        public string? Icon { get; set; }
        public string? CodePrefix { get; set; }
        public IDictionary<string, OriginalHint>? HintMap { get; set; } = new Dictionary<string, OriginalHint>();
    }

    private sealed class OriginalHint
    {
        public string? Idea { get; set; }
        public string? Solution { get; set; }
        public string? Answer { get; set; }
    }
}
