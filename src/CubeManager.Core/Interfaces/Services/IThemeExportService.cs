namespace CubeManager.Core.Interfaces.Services;

public interface IThemeExportService
{
    /// <summary>전체 테마+힌트를 JSON 파일로 Export</summary>
    Task ExportAllToJsonAsync(string filePath);

    /// <summary>특정 테마+힌트를 JSON 파일로 Export</summary>
    Task ExportThemeToJsonAsync(int themeId, string filePath);

    /// <summary>Cube Escape 원본 템플릿 JSON을 Import</summary>
    Task<ThemeImportResult> ImportOriginalJsonAsync(string filePath);
}

public sealed class ThemeImportResult
{
    public int ThemeCount { get; set; }
    public int HintCount { get; set; }
}
