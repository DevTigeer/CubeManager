using CubeManager.Telegram.Commands;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CubeManager.Telegram;

public sealed class TelegramBotWorker : ITelegramBotWorker
{
    private readonly ITelegramBotConfigService _configService;
    private readonly CommandRouter _router;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private TelegramBotClient? _bot;
    private CancellationTokenSource? _cts;
    private HashSet<long> _allowedChatIds = new();
    private long _ownerChatId;
    private bool _running;

    public TelegramBotWorker(ITelegramBotConfigService configService, CommandRouter router)
    {
        _configService = configService;
        _router = router;
    }

    public bool IsRunning => _running;

    public async Task StartAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            if (_running)
            {
                Log.Information("Telegram bot already running");
                return;
            }

            var options = await _configService.LoadAsync();
            if (!options.IsConfigured)
            {
                Log.Information("Telegram bot 미설정 (token/chat_id/enabled 중 하나 누락) — 시작 안 함");
                return;
            }

            _allowedChatIds = options.AllowedChatIds.ToHashSet();
            _ownerChatId = options.OwnerChatId;
            // 점주 DM이 그룹 화이트리스트에 없어도 명령 받도록 자동 추가
            if (_ownerChatId != 0) _allowedChatIds.Add(_ownerChatId);
            _cts = new CancellationTokenSource();
            _bot = new TelegramBotClient(options.Token);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message },
                DropPendingUpdates = true
            };

            _bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token);

            _running = true;
            Log.Information("Telegram bot started (allowed chats: {Count})", _allowedChatIds.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Telegram bot 시작 실패");
            _running = false;
            _cts?.Dispose();
            _cts = null;
            _bot = null;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            if (!_running) return;

            try { _cts?.Cancel(); } catch { /* ignore */ }
            _cts?.Dispose();
            _cts = null;
            _bot = null;
            _running = false;
            Log.Information("Telegram bot stopped");
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task RestartAsync()
    {
        await StopAsync();
        await StartAsync();
    }

    public async Task<TestSendResult> SendTestMessageAsync(string token, long chatId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new TestSendResult(false, "토큰이 비어있습니다.");

        try
        {
            var bot = new TelegramBotClient(token);
            var msg = $"✅ CubeManager 연결 OK\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            await bot.SendMessage(chatId, msg, cancellationToken: ct);
            return new TestSendResult(true, null);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Telegram test send 실패 chat={ChatId}", chatId);
            return new TestSendResult(false, ex.Message);
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Message is not { } message) return;
            if (string.IsNullOrEmpty(message.Text)) return;

            var chatId = message.Chat.Id;
            if (!_allowedChatIds.Contains(chatId))
            {
                Log.Warning("Telegram unauthorized chat_id {ChatId} text={Text}", chatId, message.Text);
                return; // 미허용 chat에는 무응답 (정보 노출 차단)
            }

            await _router.RouteAsync(bot, chatId, message.Text, _ownerChatId, ct);
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            Log.Error(ex, "Telegram update handler unhandled exception");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Log.Error(ex, "Telegram polling error");
        return Task.CompletedTask;
    }
}
