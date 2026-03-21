namespace CubeManager.Core.Models;

/// <summary>체크리스트 템플릿. 요일별 할일 항목 (관리자가 등록).</summary>
public class ChecklistTemplate
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }       // 0=일,1=월,...6=토
    public string TaskText { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
