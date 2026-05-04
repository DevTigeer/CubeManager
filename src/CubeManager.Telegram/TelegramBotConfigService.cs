using System.Runtime.Versioning;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;

namespace CubeManager.Telegram;

[SupportedOSPlatform("windows")]
public sealed class TelegramBotConfigService : ITelegramBotConfigService
{
    public const string KeyToken = "telegram_bot_token";
    public const string KeyChatIds = "telegram_allowed_chat_ids";
    public const string KeyEnabled = "telegram_bot_enabled";

    private readonly IConfigRepository _config;

    public TelegramBotConfigService(IConfigRepository config) => _config = config;

    public async Task<TelegramBotOptions> LoadAsync()
    {
        var encryptedToken = await _config.GetAsync(KeyToken);
        var token = string.IsNullOrEmpty(encryptedToken)
            ? string.Empty
            : CredentialHelper.Decrypt(encryptedToken);

        var idsRaw = await _config.GetAsync(KeyChatIds);
        var ids = TelegramBotOptions.ParseChatIds(idsRaw);

        var enabled = await _config.GetIntAsync(KeyEnabled, 0) == 1;

        return new TelegramBotOptions
        {
            Token = token,
            AllowedChatIds = ids,
            Enabled = enabled
        };
    }

    public async Task SaveAsync(string token, IReadOnlyList<long> allowedChatIds, bool enabled)
    {
        var encrypted = string.IsNullOrEmpty(token)
            ? string.Empty
            : CredentialHelper.Encrypt(token);

        await _config.SetAsync(KeyToken, encrypted);
        await _config.SetAsync(KeyChatIds, TelegramBotOptions.FormatChatIds(allowedChatIds));
        await _config.SetAsync(KeyEnabled, enabled ? "1" : "0");
    }
}
