namespace CubeManager.Core.Models;

/// <summary>체크리스트 템플릿. 요일+역할별 할일 항목 (관리자가 등록).</summary>
public class ChecklistTemplate
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }       // 0=일,1=월,...6=토 (비트마스크 아님, 다중 요일은 행 복수 삽입)
    public string Role { get; set; } = "all"; // "open","close","middle1","middle2","all"
    public string TaskText { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
