using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using Serilog;

namespace CubeManager.Core.Services;

public class ThemeExportService : IThemeExportService
{
    private readonly IThemeRepository _themeRepo;

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
        var exportData = new ExportRoot
        {
            ExportedAt = DateTime.Now,
            Themes = []
        };

        foreach (var theme in themes)
        {
            if (!theme.IsActive) continue;

            var hints = await _themeRepo.GetHintsByThemeIdAsync(theme.Id);
            exportData.Themes.Add(new ExportTheme
            {
                ThemeName = theme.ThemeName,
                Description = theme.Description,
                Hints = hints.Select(h => new ExportHint
                {
                    HintCode = h.HintCode,
                    Question = h.Question,
                    Hint1 = h.Hint1,
                    Hint2 = h.Hint2,
                    Answer = h.Answer
                }).ToList()
            });
        }

        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        Log.Information("테마 JSON Export 완료: {Path} ({Count}개 테마)", filePath, exportData.Themes.Count);
    }

    public async Task ExportThemeToJsonAsync(int themeId, string filePath)
    {
        var theme = await _themeRepo.GetThemeByIdAsync(themeId);
        if (theme == null) throw new ArgumentException($"테마 ID {themeId}를 찾을 수 없습니다.");

        var hints = await _themeRepo.GetHintsByThemeIdAsync(themeId);
        var exportData = new ExportRoot
        {
            ExportedAt = DateTime.Now,
            Themes =
            [
                new ExportTheme
                {
                    ThemeName = theme.ThemeName,
                    Description = theme.Description,
                    Hints = hints.Select(h => new ExportHint
                    {
                        HintCode = h.HintCode,
                        Question = h.Question,
                        Hint1 = h.Hint1,
                        Hint2 = h.Hint2,
                        Answer = h.Answer
                    }).ToList()
                }
            ]
        };

        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        Log.Information("테마 JSON Export 완료: {Path} ({Theme})", filePath, theme.ThemeName);
    }

    // === Export DTO ===

    private sealed class ExportRoot
    {
        public DateTime ExportedAt { get; set; }
        public List<ExportTheme> Themes { get; set; } = [];
    }

    private sealed class ExportTheme
    {
        public string ThemeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<ExportHint> Hints { get; set; } = [];
    }

    private sealed class ExportHint
    {
        public int HintCode { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Hint1 { get; set; } = string.Empty;
        public string? Hint2 { get; set; }
        public string Answer { get; set; } = string.Empty;
    }
}
