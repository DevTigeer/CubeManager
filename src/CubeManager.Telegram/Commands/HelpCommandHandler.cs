using System.Text;
using Telegram.Bot;

namespace CubeManager.Telegram.Commands;

public sealed class HelpCommandHandler : ICommandHandler
{
    private readonly IEnumerable<ICommandHandler> _all;

    public HelpCommandHandler(IEnumerable<ICommandHandler> all) => _all = all;

    public string Command => "help";
    public IReadOnlyList<string> Aliases => new[] { "start", "도움말" };
    public string Description => "사용 가능한 명령 목록";

    public async Task HandleAsync(CommandContext ctx)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📋 CubeManager 봇 명령");
        sb.AppendLine();
        foreach (var h in _all.OrderBy(h => h.Command, StringComparer.Ordinal))
        {
            sb.Append('/').Append(h.Command).Append(" — ").AppendLine(h.Description);
        }
        await ctx.Client.SendMessage(ctx.ChatId, sb.ToString(), cancellationToken: ctx.CancellationToken);
    }
}
