namespace CubeManager.Core.Models;

/// <summary>근무 파트 (오픈/마감/미들 등)</summary>
public class WorkPart
{
    public int Id { get; set; }
    public string PartName { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
