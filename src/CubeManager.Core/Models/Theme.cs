namespace CubeManager.Core.Models;

/// <summary>매장 방탈출 테마 (예: 집착, 타이타닉 등)</summary>
public class Theme
{
    public int Id { get; set; }
    public string ThemeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
