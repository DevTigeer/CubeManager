namespace CubeManager.Telegram;

public interface ITelegramBotWorker
{
    bool IsRunning { get; }

    /// <summary>설정에서 옵션을 로드하여 폴링 시작. 미설정/비활성 시 INFO 로그 후 무동작.</summary>
    Task StartAsync();

    /// <summary>폴링 중지 (CancellationTokenSource cancel).</summary>
    Task StopAsync();

    /// <summary>설정 변경 후 재시작 (Stop → Start).</summary>
    Task RestartAsync();

    /// <summary>설정 저장 전 토큰/chat_id 검증용 일회성 발송.</summary>
    Task<TestSendResult> SendTestMessageAsync(string token, long chatId, CancellationToken ct = default);
}

public sealed record TestSendResult(bool Success, string? Error);
