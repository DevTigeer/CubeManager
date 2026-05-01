using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Services;

public interface IUpdateCheckService
{
    Task<AppUpdateCheckResult> CheckAsync(string currentVersion, CancellationToken cancellationToken = default);
    Task<string> DownloadInstallerAsync(
        AppUpdateInfo updateInfo,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
