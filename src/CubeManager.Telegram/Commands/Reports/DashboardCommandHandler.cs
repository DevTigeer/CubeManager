using System.Runtime.Versioning;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CubeManager.Telegram.Commands.Reports;

[SupportedOSPlatform("windows")]
public sealed class DashboardCommandHandler : ICommandHandler
{
    private readonly ISalesService _sales;
    private readonly IReservationRepository _reservations;
    private readonly IAttendanceService _attendance;
    private readonly IChecklistRepository _checklist;
    private readonly IBotImageRenderer _renderer;

    public DashboardCommandHandler(
        ISalesService sales,
        IReservationRepository reservations,
        IAttendanceService attendance,
        IChecklistRepository checklist,
        IBotImageRenderer renderer)
    {
        _sales = sales;
        _reservations = reservations;
        _attendance = attendance;
        _checklist = checklist;
        _renderer = renderer;
    }

    public string Command => "dashboard";
    public IReadOnlyList<string> Aliases => new[] { "대시보드", "현황" };
    public string Description => "오늘 종합 현황 (매출+예약+근무+체크리스트)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var date = DateArgParser.ParseSingleDateOrToday(ctx.Args);
        var dateStr = DateArgParser.FormatDate(date);

        var daily = await _sales.GetDailySalesAsync(dateStr);
        var cash = await _sales.GetCashBalanceAsync(dateStr);
        var reservations = (await _reservations.GetByDateAsync(dateStr))
            .Where(r => r.Status == "confirmed").ToList();
        var att = (await _attendance.GetByDateRangeAsync(dateStr, dateStr)).ToList();
        var workingNow = att.Count(a => !string.IsNullOrEmpty(a.ClockIn) && string.IsNullOrEmpty(a.ClockOut));
        var checklist = (await _checklist.GetRecordsForDateAsync(dateStr)).ToList();
        var checkDone = checklist.Count(r => r.IsChecked);
        var checkRate = checklist.Count == 0 ? 0 : (int)Math.Round(checkDone * 100.0 / checklist.Count);
        var headcount = reservations.Sum(r => r.Headcount);

        var rows = new List<KvRow>
        {
            new("매출 합계", $"{(daily?.TotalRevenue ?? 0):N0} 원", "#1565C0"),
            new("  · 카드 / 현금 / 계좌", $"{(daily?.CardAmount ?? 0):N0} / {(daily?.CashAmount ?? 0):N0} / {(daily?.TransferAmount ?? 0):N0}"),
            new("현금 마감 잔액", $"{(cash?.ClosingBalance ?? 0):N0} 원"),
            new("예약 건수", $"{reservations.Count} 건 (총 {headcount}명)"),
            new("출근자", $"{att.Count}명 (현재 근무 {workingNow}명)", "#2E7D32"),
            new("체크리스트", $"{checkDone}/{checklist.Count} ({checkRate}%)",
                checkRate >= 80 ? "#2E7D32" : (checkRate >= 50 ? "#E65100" : "#C62828")),
        };

        using var stream = _renderer.RenderKeyValueCard(
            "오늘의 운영 현황",
            DateArgParser.FormatDateKorean(date),
            rows,
            $"조회 시각: {DateTime.Now:yyyy-MM-dd HH:mm}");

        var photo = InputFile.FromStream(stream, $"dashboard-{dateStr}.png");
        await ctx.Client.SendPhoto(ctx.ChatId, photo, cancellationToken: ctx.CancellationToken);
    }
}
