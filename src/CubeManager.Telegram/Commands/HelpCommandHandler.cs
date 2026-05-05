using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace CubeManager.Telegram.Commands;

public sealed class HelpCommandHandler : ICommandHandler
{
    private readonly IServiceProvider _sp;

    public HelpCommandHandler(IServiceProvider sp) => _sp = sp;

    public string Command => "help";
    public IReadOnlyList<string> Aliases => new[] { "start", "도움말" };
    public string Description => "사용 가능한 명령 목록";

    public async Task HandleAsync(CommandContext ctx)
    {
        var all = _sp.GetServices<ICommandHandler>();
        var sb = new StringBuilder();
        sb.AppendLine("📋 CubeManager 봇 명령");
        sb.AppendLine();
        foreach (var h in all.OrderBy(h => h.Command, StringComparer.Ordinal))
        {
            sb.Append('/').Append(h.Command).Append(" — ").AppendLine(h.Description);
        }
        await ctx.Client.SendMessage(ctx.ChatId, sb.ToString(), cancellationToken: ctx.CancellationToken);
    }
}
