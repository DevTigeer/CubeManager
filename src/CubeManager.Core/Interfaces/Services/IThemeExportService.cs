namespace CubeManager.Core.Interfaces.Services;

public interface IThemeExportService
{
    /// <summary>전체 테마+힌트를 JSON 파일로 Export</summary>
    Task ExportAllToJsonAsync(string filePath);

    /// <summary>특정 테마+힌트를 JSON 파일로 Export</summary>
    Task ExportThemeToJsonAsync(int themeId, string filePath);
}
