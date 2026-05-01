using CubeManager.Core.Interfaces.Services;
using CubeManager.Dialogs;
using Serilog;

namespace CubeManager.Helpers;

public static class UpdateCoordinator
{
    public static async Task CheckAndPromptAsync(
        IWin32Window owner,
        IUpdateCheckService updateService,
        bool showUpToDateToast,
        CancellationToken cancellationToken = default)
    {
        var result = await updateService.CheckAsync(AppVersionHelper.CurrentVersion, cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            if (showUpToDateToast)
                ToastNotification.Show(result.ErrorMessage, ToastType.Warning);
            return;
        }

        if (!result.IsUpdateAvailable || result.Latest == null)
        {
            if (showUpToDateToast)
                ToastNotification.Show("현재 최신 버전입니다.", ToastType.Success);
            return;
        }

        Log.Information("업데이트 발견: {CurrentVersion} -> {LatestVersion}",
            result.CurrentVersion, result.Latest.Version);

        using var dialog = new UpdateAvailableDialog(updateService, result);
        dialog.ShowDialog(owner);
    }
}
