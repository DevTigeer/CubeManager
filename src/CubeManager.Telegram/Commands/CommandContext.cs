using Telegram.Bot;

namespace CubeManager.Telegram.Commands;

public sealed class CommandContext
{
    public required ITelegramBotClient Client { get; init; }
    public required long ChatId { get; init; }
    public required string CommandText { get; init; }
    public required string[] Args { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    /// <summary>점주 DM chat id. 0이면 미설정. IOwnerOnlyCommand 가드에 사용.</summary>
    public long OwnerChatId { get; init; }
}
