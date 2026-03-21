namespace CubeManager.Core.Models;

/// <summary>체크리스트 일별 완료 기록.</summary>
public class ChecklistRecord
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public string CheckDate { get; set; } = "";
    public bool IsChecked { get; set; }
    public string? CheckedBy { get; set; }
    public string? CheckedAt { get; set; }
    // JOIN용
    public string? TaskText { get; set; }
    public int SortOrder { get; set; }
}
