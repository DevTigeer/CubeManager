using System.Runtime.Versioning;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CubeManager.Telegram.Commands.Reports;

[SupportedOSPlatform("windows")]
public sealed class SalesCommandHandler : ICommandHandler
{
    private readonly ISalesService _sales;
    private readonly IReservationRepository _reservations;
    private readonly IBotImageRenderer _renderer;

    public SalesCommandHandler(ISalesService sales, IReservationRepository reservations, IBotImageRenderer renderer)
    {
        _sales = sales;
        _reservations = reservations;
        _renderer = renderer;
    }

    public string Command => "sales";
    public IReadOnlyList<string> Aliases => new[] { "오늘매출", "매출" };
    public string Description => "매출 조회 (인자 없음=오늘, /sales YYYY-MM-DD)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var date = DateArgParser.ParseSingleDateOrToday(ctx.Args);
        var dateStr = DateArgParser.FormatDate(date);

        var daily = await _sales.GetDailySalesAsync(dateStr);
        var cash = await _sales.GetCashBalanceAsync(dateStr);
        var reservations = await _reservations.GetByDateAsync(dateStr);
        var resCount = reservations.Count(r => r.Status == "confirmed");

        var card = daily?.CardAmount ?? 0;
        var cashAmt = daily?.CashAmount ?? 0;
        var transfer = daily?.TransferAmount ?? 0;
        var total = daily?.TotalRevenue ?? 0;

        var rows = new List<KvRow>
        {
            new("카드", $"{card:N0} 원", "#1565C0"),
            new("현금", $"{cashAmt:N0} 원", "#2E7D32"),
            new("계좌이체", $"{transfer:N0} 원", "#E65100"),
            new("합계", $"{total:N0} 원", "#212933"),
            new("예약 건수", $"{resCount} 건"),
        };
        if (cash != null)
            rows.Add(new KvRow("현금 마감", $"{cash.ClosingBalance:N0} 원"));

        using var stream = _renderer.RenderKeyValueCard(
            "매출 요약",
            DateArgParser.FormatDateKorean(date),
            rows,
            $"조회 시각: {DateTime.Now:yyyy-MM-dd HH:mm}");

        var photo = InputFile.FromStream(stream, $"sales-{dateStr}.png");
        await ctx.Client.SendPhoto(ctx.ChatId, photo, cancellationToken: ctx.CancellationToken);
    }
}
