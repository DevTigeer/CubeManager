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
    public const string KeyOwnerChatId = "telegram_owner_chat_id";

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

        var ownerRaw = await _config.GetAsync(KeyOwnerChatId);
        var ownerId = long.TryParse(ownerRaw, out var o) ? o : 0L;

        return new TelegramBotOptions
        {
            Token = token,
            AllowedChatIds = ids,
            Enabled = enabled,
            OwnerChatId = ownerId
        };
    }

    public async Task SaveAsync(string token, IReadOnlyList<long> allowedChatIds, bool enabled, long ownerChatId = 0)
    {
        var encrypted = string.IsNullOrEmpty(token)
            ? string.Empty
            : CredentialHelper.Encrypt(token);

        await _config.SetAsync(KeyToken, encrypted);
        await _config.SetAsync(KeyChatIds, TelegramBotOptions.FormatChatIds(allowedChatIds));
        await _config.SetAsync(KeyEnabled, enabled ? "1" : "0");
        await _config.SetAsync(KeyOwnerChatId, ownerChatId == 0 ? "" : ownerChatId.ToString());
    }
}
