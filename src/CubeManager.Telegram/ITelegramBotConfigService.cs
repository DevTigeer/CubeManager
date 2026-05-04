namespace CubeManager.Telegram;

/// <summary>
/// 봇 설정 영속화. 토큰은 DPAPI 암호화하여 app_config에 저장.
/// </summary>
public interface ITelegramBotConfigService
{
    Task<TelegramBotOptions> LoadAsync();
    Task SaveAsync(string token, IReadOnlyList<long> allowedChatIds, bool enabled);
}
