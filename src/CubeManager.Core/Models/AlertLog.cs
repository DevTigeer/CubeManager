namespace CubeManager.Core.Models;

/// <summary>알림 이력 모델</summary>
public class AlertLog
{
    public int Id { get; set; }
    public string AlertType { get; set; } = "";      // checklist_delay, late_arrival, handover_unread, no_show, late_accumulate
    public int? EmployeeId { get; set; }
    public string AlertDate { get; set; } = "";       // YYYY-MM-DD
    public string AlertTime { get; set; } = "";       // HH:MM:SS
    public string Severity { get; set; } = "warning"; // info, warning, critical
    public string Message { get; set; } = "";
    public bool IsResolved { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolvedAt { get; set; }
    public string CreatedAt { get; set; } = "";

    // JOIN용
    public string? EmployeeName { get; set; }
}

/// <summary>알림 유형 상수</summary>
public static class AlertTypes
{
    public const string ChecklistDelay = "checklist_delay";
    public const string LateArrival = "late_arrival";
    public const string HandoverUnread = "handover_unread";
    public const string NoShow = "no_show";
    public const string LateAccumulate = "late_accumulate";
}
