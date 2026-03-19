namespace CubeManager.Core.Models;

/// <summary>테마별 문제/힌트/정답. 힌트코드는 1000~9999 난수.</summary>
public class ThemeHint
{
    public int Id { get; set; }
    public int ThemeId { get; set; }
    public int HintCode { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Hint1 { get; set; } = string.Empty;
    public string? Hint2 { get; set; }
    public string Answer { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
