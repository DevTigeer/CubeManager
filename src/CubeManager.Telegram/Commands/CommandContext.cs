using Telegram.Bot;

namespace CubeManager.Telegram.Commands;

public sealed class CommandContext
{
    public required ITelegramBotClient Client { get; init; }
    public required long ChatId { get; init; }
    public required string CommandText { get; init; }
    public required string[] Args { get; init; }
    public required CancellationToken CancellationToken { get; init; }
}
