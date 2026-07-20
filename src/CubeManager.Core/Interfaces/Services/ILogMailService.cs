namespace CubeManager.Core.Interfaces.Services;

/// <summary>
/// 로그 파일을 Gmail SMTP로 개발자에게 전송하는 서비스.
/// 발신 계정/앱 비밀번호는 앱 설정(app_config)에 DPAPI 암호화하여 저장한다.
/// </summary>
public interface ILogMailService
{
    /// <summary>발신 Gmail 계정과 앱 비밀번호가 모두 설정되어 있으면 true.</summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>저장된 발신 Gmail 계정 이메일을 반환한다 (미설정 시 빈 문자열).</summary>
    Task<string> GetSenderEmailAsync();

    /// <summary>발신 Gmail 계정과 앱 비밀번호를 저장한다.</summary>
    Task SaveCredentialsAsync(string senderEmail, string appPassword);

    /// <summary>%APPDATA%/CubeManager/logs 의 로그 파일을 개발자 이메일로 전송한다.</summary>
    Task<LogMailResult> SendLogsAsync(CancellationToken cancellationToken = default);
}

/// <summary>로그 전송 결과.</summary>
public record LogMailResult(bool Success, string Message)
{
    public static LogMailResult Ok(string message) => new(true, message);
    public static LogMailResult Fail(string message) => new(false, message);
}
