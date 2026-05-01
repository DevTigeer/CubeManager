namespace CubeManager.Core.Models;

public class AppUpdateCheckResult
{
    public string CurrentVersion { get; set; } = "";
    public AppUpdateInfo? Latest { get; set; }
    public bool IsUpdateAvailable { get; set; }
    public string? ErrorMessage { get; set; }
}
