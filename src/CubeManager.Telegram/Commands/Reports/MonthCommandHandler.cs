using System.Runtime.Versioning;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CubeManager.Telegram.Commands.Reports;

[SupportedOSPlatform("windows")]
public sealed class MonthCommandHandler : ICommandHandler
{
    private readonly ISalesService _sales;
    private readonly IBotImageRenderer _renderer;

    public MonthCommandHandler(ISalesService sales, IBotImageRenderer renderer)
    {
        _sales = sales;
        _renderer = renderer;
    }

    public string Command => "month";
    public IReadOnlyList<string> Aliases => new[] { "이번달매출", "월매출" };
    public string Description => "월간 매출 (인자 없음=이번달, /month YYYY-MM)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var firstDay = DateArgParser.ParseMonthOrCurrent(ctx.Args);
        var daysInMonth = DateTime.DaysInMonth(firstDay.Year, firstDay.Month);

        long totalCard = 0, totalCash = 0, totalTransfer = 0, totalRevenue = 0;
        var daysWithSales = 0;
        var topRows = new List<(DateOnly Date, int Revenue)>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var d = new DateOnly(firstDay.Year, firstDay.Month, day);
            if (d > DateOnly.FromDateTime(DateTime.Today)) break;
            var ds = await _sales.GetDailySalesAsync(DateArgParser.FormatDate(d));
            if (ds == null) continue;
            totalCard += ds.CardAmount;
            totalCash += ds.CashAmount;
            totalTransfer += ds.TransferAmount;
            totalRevenue += ds.TotalRevenue;
            if (ds.TotalRevenue > 0)
            {
                daysWithSales++;
                topRows.Add((d, ds.TotalRevenue));
            }
        }

        var avg = daysWithSales > 0 ? totalRevenue / daysWithSales : 0;
        var top5 = topRows.OrderByDescending(r => r.Revenue).Take(5).ToList();

        var headers = new[] { "구분", "금액" };
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "카드", $"{totalCard:N0} 원" },
            new[] { "현금", $"{totalCash:N0} 원" },
            new[] { "계좌이체", $"{totalTransfer:N0} 원" },
            new[] { "합계", $"{totalRevenue:N0} 원" },
            new[] { "영업일수", $"{daysWithSales} 일" },
            new[] { "일평균", $"{avg:N0} 원" },
        };
        foreach (var (d, r) in top5)
            rows.Add(new[] { $"TOP {top5.IndexOf((d, r)) + 1} · {DateArgParser.FormatDate(d)}", $"{r:N0} 원" });

        using var stream = _renderer.RenderTable(
            "월간 매출 요약",
            $"{firstDay:yyyy-MM} ({firstDay:yyyy}년 {firstDay.Month}월)",
            headers,
            rows,
            $"조회 시각: {DateTime.Now:yyyy-MM-dd HH:mm}");

        var photo = InputFile.FromStream(stream, $"month-{firstDay:yyyy-MM}.png");
        await ctx.Client.SendPhoto(ctx.ChatId, photo, cancellationToken: ctx.CancellationToken);
    }
}
