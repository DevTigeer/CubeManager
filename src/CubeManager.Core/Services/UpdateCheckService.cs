using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Security.Cryptography;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using Serilog;

namespace CubeManager.Core.Services;

public class UpdateCheckService : IUpdateCheckService
{
    private const string ManifestUrlKey = "update_manifest_url";
    private const string LastCheckKey = "update_last_check_at";
    private const string DefaultManifestUrl =
        "https://github.com/DevTigeer/CubeManager/releases/latest/download/update.json";

    private readonly IConfigRepository _configRepo;
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    public UpdateCheckService(IConfigRepository configRepo)
    {
        _configRepo = configRepo;
    }

    public async Task<AppUpdateCheckResult> CheckAsync(
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var manifestUrl = await _configRepo.GetAsync(ManifestUrlKey);
            if (string.IsNullOrWhiteSpace(manifestUrl))
                manifestUrl = DefaultManifestUrl;

            using var response = await HttpClient.GetAsync(manifestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var updateInfo = await response.Content.ReadFromJsonAsync<AppUpdateInfo>(
                cancellationToken: cancellationToken);

            if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.Version))
            {
                return new AppUpdateCheckResult
                {
                    CurrentVersion = currentVersion,
                    ErrorMessage = "업데이트 정보를 읽을 수 없습니다."
                };
            }

            await _configRepo.SetAsync(LastCheckKey, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            return new AppUpdateCheckResult
            {
                CurrentVersion = currentVersion,
                Latest = updateInfo,
                IsUpdateAvailable = IsNewerVersion(updateInfo.Version, currentVersion)
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "업데이트 확인 실패");
            return new AppUpdateCheckResult
            {
                CurrentVersion = currentVersion,
                ErrorMessage = "업데이트 확인에 실패했습니다."
            };
        }
    }

    public async Task<string> DownloadInstallerAsync(
        AppUpdateInfo updateInfo,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
            throw new InvalidOperationException("설치파일 다운로드 주소가 없습니다.");

        var downloadDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CubeManager",
            "Updates");
        Directory.CreateDirectory(downloadDir);

        var fileName = Path.GetFileName(new Uri(updateInfo.DownloadUrl).AbsolutePath);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"CubeManagerSetup-{updateInfo.Version}.exe";

        var targetPath = Path.Combine(downloadDir, fileName);

        using var response = await HttpClient.GetAsync(
            updateInfo.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        {
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = File.Create(targetPath);

            var buffer = new byte[81920];
            long downloadedBytes = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                downloadedBytes += read;

                if (totalBytes is > 0)
                    progress?.Report((int)(downloadedBytes * 100 / totalBytes.Value));
            }

            progress?.Report(100);
            await target.FlushAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(updateInfo.Sha256))
            await VerifySha256Async(targetPath, updateInfo.Sha256, cancellationToken);

        return targetPath;
    }

    private static async Task VerifySha256Async(
        string filePath,
        string expectedHash,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var normalizedExpected = expectedHash.Replace(" ", "", StringComparison.Ordinal)
            .ToLowerInvariant();

        if (!actualHash.Equals(normalizedExpected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("설치파일 검증에 실패했습니다.");
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        if (TryParseVersion(latest, out var latestVersion) &&
            TryParseVersion(current, out var currentVersion))
        {
            return latestVersion > currentVersion;
        }

        return string.Compare(latest, current, StringComparison.OrdinalIgnoreCase) > 0;
    }

    private static bool TryParseVersion(string value, [NotNullWhen(true)] out Version? version)
    {
        version = null;
        var normalized = value.Trim().TrimStart('v', 'V');
        return Version.TryParse(normalized, out version);
    }
}
