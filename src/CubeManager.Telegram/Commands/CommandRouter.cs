using Serilog;
using Telegram.Bot;

namespace CubeManager.Telegram.Commands;

public sealed class CommandRouter
{
    private readonly Dictionary<string, ICommandHandler> _byName;

    public CommandRouter(IEnumerable<ICommandHandler> handlers)
    {
        _byName = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in handlers)
        {
            _byName[h.Command] = h;
            foreach (var alias in h.Aliases)
                _byName[alias] = h;
        }
    }

    public IEnumerable<ICommandHandler> AllHandlers => _byName.Values.Distinct();

    public async Task RouteAsync(ITelegramBotClient client, long chatId, string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // "/ping@MyBot arg1 arg2" → name="ping", args=["arg1","arg2"]
        var trimmed = text.TrimStart();
        if (!trimmed.StartsWith('/')) return;

        var parts = trimmed.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var name = parts[0];
        var atIdx = name.IndexOf('@');
        if (atIdx > 0) name = name.Substring(0, atIdx);

        var args = parts.Skip(1).ToArray();

        if (!_byName.TryGetValue(name, out var handler))
        {
            await client.SendMessage(chatId, $"지원하지 않는 명령입니다: /{name}\n/help 로 목록을 확인하세요.", cancellationToken: ct);
            return;
        }

        var ctx = new CommandContext
        {
            Client = client,
            ChatId = chatId,
            CommandText = text,
            Args = args,
            CancellationToken = ct
        };

        try
        {
            await handler.HandleAsync(ctx);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
            Log.Error(ex, "Telegram command handler error: {Command} req={ReqId}", name, requestId);
            try
            {
                await client.SendMessage(chatId, $"명령 처리 중 오류가 발생했습니다 (참조: {requestId})", cancellationToken: ct);
            }
            catch { /* swallow */ }
        }
    }
}
