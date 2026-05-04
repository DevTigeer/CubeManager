using System.Reflection;
using Telegram.Bot;

namespace CubeManager.Telegram.Commands;

public sealed class PingCommandHandler : ICommandHandler
{
    public string Command => "ping";
    public IReadOnlyList<string> Aliases => new[] { "핑" };
    public string Description => "봇 응답 확인";

    public async Task HandleAsync(CommandContext ctx)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "?";
        var msg = $"pong\nCubeManager v{version}\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        await ctx.Client.SendMessage(ctx.ChatId, msg, cancellationToken: ctx.CancellationToken);
    }
}
