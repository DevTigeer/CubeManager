namespace CubeManager.Core.Models;

public class AppUpdateInfo
{
    public string Version { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public bool Mandatory { get; set; }
    public string Notes { get; set; } = "";
    public DateTimeOffset? ReleasedAt { get; set; }
}
