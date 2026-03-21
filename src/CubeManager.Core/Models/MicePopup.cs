namespace CubeManager.Core.Models;

/// <summary>미끼관리 팝업 설정. 일정 간격마다 팝업을 띄워 업무 확인.</summary>
public class MicePopup
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int IntervalMinutes { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public string? LastShownAt { get; set; }
}
