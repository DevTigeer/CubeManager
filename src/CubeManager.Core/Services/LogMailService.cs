using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Runtime.Versioning;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using Serilog;

namespace CubeManager.Core.Services;

/// <summary>
/// Gmail SMTP(smtp.gmail.com:587, STARTTLS)로 로그 파일을 개발자에게 전송한다.
///
/// 첨부 범위: Serilog는 일 단위 롤링(RollingInterval.Day, 7일 보관)이라
/// 로그가 날짜별 파일(cube-YYYYMMDD.log)로 쪼개져 저장된다. 에러 보고 시점의
/// 오류가 반드시 "오늘" 파일에 있으리란 보장이 없으므로, 최신 1개만으로는 부족하다.
/// 따라서 logs 폴더 전체를 zip으로 묶어 첨부한다.
/// </summary>
[SupportedOSPlatform("windows")]
public class LogMailService : ILogMailService
{
    private const string SenderKey = "log_mail_sender";
    private const string AppPasswordKey = "log_mail_app_password";
    private const string RecipientEmail = "dev.tigeer@gmail.com";
    private const string SmtpHost = "smtp.gmail.com";
    private const int SmtpPort = 587;

    private readonly IConfigRepository _configRepo;

    public LogMailService(IConfigRepository configRepo)
    {
        _configRepo = configRepo;
    }

    public async Task<string> GetSenderEmailAsync()
    {
        var enc = await _configRepo.GetAsync(SenderKey);
        return string.IsNullOrEmpty(enc) ? string.Empty : CredentialHelper.Decrypt(enc);
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var sender = await GetSenderEmailAsync();
        var appPassword = await GetAppPasswordAsync();
        return !string.IsNullOrWhiteSpace(sender) && !string.IsNullOrWhiteSpace(appPassword);
    }

    public async Task SaveCredentialsAsync(string senderEmail, string appPassword)
    {
        await _configRepo.SetAsync(SenderKey, CredentialHelper.Encrypt(senderEmail.Trim()));
        // Gmail 앱 비밀번호는 "xxxx xxxx xxxx xxxx" 형태로 표기되므로 공백 제거 후 저장
        var normalized = appPassword.Replace(" ", "", StringComparison.Ordinal);
        await _configRepo.SetAsync(AppPasswordKey, CredentialHelper.Encrypt(normalized));
    }

    public async Task<LogMailResult> SendLogsAsync(CancellationToken cancellationToken = default)
    {
        var sender = await GetSenderEmailAsync();
        var appPassword = await GetAppPasswordAsync();

        if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(appPassword))
            return LogMailResult.Fail("발신 Gmail 계정/앱 비밀번호가 설정되지 않았습니다.");

        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CubeManager", "logs");

        if (!Directory.Exists(logDir))
            return LogMailResult.Fail("로그 폴더를 찾을 수 없습니다.");

        var logFiles = Directory.GetFiles(logDir, "*.log");
        if (logFiles.Length == 0)
            return LogMailResult.Fail("전송할 로그 파일이 없습니다.");

        string? zipPath = null;
        try
        {
            zipPath = await CreateLogsZipAsync(logFiles, cancellationToken);

            using var message = new MailMessage(sender, RecipientEmail)
            {
                Subject = $"[CubeManager] 에러 로그 전송 ({DateTime.Now:yyyy-MM-dd HH:mm})",
                Body = $"CubeManager 로그 파일 {logFiles.Length}개(zip 첨부)입니다.\n" +
                       $"발신 계정: {sender}\n" +
                       $"전송 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };
            message.Attachments.Add(new Attachment(zipPath));

            using var smtp = new SmtpClient(SmtpHost, SmtpPort)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(sender, appPassword)
            };

            await smtp.SendMailAsync(message, cancellationToken);

            Log.Information("에러 로그 이메일 전송 성공 (파일 {Count}개 → {Recipient})",
                logFiles.Length, RecipientEmail);
            return LogMailResult.Ok($"로그 {logFiles.Length}개 파일을 전송했습니다.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "에러 로그 이메일 전송 실패");
            return LogMailResult.Fail($"로그 전송에 실패했습니다: {ex.Message}");
        }
        finally
        {
            if (zipPath != null)
            {
                try { File.Delete(zipPath); }
                catch (Exception ex) { Log.Debug(ex, "임시 로그 zip 삭제 실패 (무시)"); }
            }
        }
    }

    private async Task<string> GetAppPasswordAsync()
    {
        var enc = await _configRepo.GetAsync(AppPasswordKey);
        return string.IsNullOrEmpty(enc) ? string.Empty : CredentialHelper.Decrypt(enc);
    }

    private static async Task<string> CreateLogsZipAsync(
        IReadOnlyList<string> logFiles, CancellationToken cancellationToken)
    {
        var tempZip = Path.Combine(Path.GetTempPath(),
            $"cube-logs-{DateTime.Now:yyyyMMddHHmmss}.zip");
        if (File.Exists(tempZip))
            File.Delete(tempZip);

        using var archive = ZipFile.Open(tempZip, ZipArchiveMode.Create);
        foreach (var file in logFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = archive.CreateEntry(Path.GetFileName(file), CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            // Serilog가 쓰기 중인(잠긴) 파일도 읽을 수 있도록 공유 모드로 오픈
            await using var source = new FileStream(
                file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await source.CopyToAsync(entryStream, cancellationToken);
        }

        return tempZip;
    }
}
