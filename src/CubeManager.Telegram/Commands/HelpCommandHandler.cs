using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace CubeManager.Telegram.Commands;

public sealed class HelpCommandHandler : ICommandHandler
{
    private readonly IServiceProvider _sp;

    public HelpCommandHandler(IServiceProvider sp) => _sp = sp;

    public string Command => "help";
    public IReadOnlyList<string> Aliases => new[] { "start", "도움말", "명령어" };
    public string Description => "사용 가능한 명령 목록";

    public async Task HandleAsync(CommandContext ctx)
    {
        var all = _sp.GetServices<ICommandHandler>();
        var sb = new StringBuilder();
        sb.AppendLine("📋 CubeManager 봇 명령");
        sb.AppendLine();
        foreach (var h in all.OrderBy(h => DisplayName(h), StringComparer.Ordinal))
        {
            sb.Append('/').Append(DisplayName(h)).Append(" — ").AppendLine(h.Description);
        }
        await ctx.Client.SendMessage(ctx.ChatId, sb.ToString(), cancellationToken: ctx.CancellationToken);
    }

    // 한글 alias 가 있으면 그걸 표시(예: "매출"), 없으면 영어 Command(예: "ping").
    private static string DisplayName(ICommandHandler h)
        => h.Aliases.FirstOrDefault(ContainsHangul) ?? h.Command;

    private static bool ContainsHangul(string s)
    {
        foreach (var c in s)
            if (c >= 0xAC00 && c <= 0xD7A3) return true;
        return false;
    }
}
