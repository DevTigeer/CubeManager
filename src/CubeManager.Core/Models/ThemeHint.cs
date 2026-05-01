namespace CubeManager.Core.Models;

/// <summary>테마별 단서/풀이/정답. 힌트코드는 1~9999 숫자이며 표시/Export 시 4자리로 채운다.</summary>
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
