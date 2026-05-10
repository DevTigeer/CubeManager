using System.Runtime.Versioning;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CubeManager.Telegram.Commands.Reports;

[SupportedOSPlatform("windows")]
public sealed class SalaryCommandHandler : ICommandHandler
{
    private readonly ISalaryService _salary;
    private readonly IBotImageRenderer _renderer;

    public SalaryCommandHandler(ISalaryService salary, IBotImageRenderer renderer)
    {
        _salary = salary;
        _renderer = renderer;
    }

    public string Command => "salary";
    public IReadOnlyList<string> Aliases => new[] { "급여", "급여관리" };
    public string Description => "월간 급여표 (인자 없음=이번달, /salary YYYY-MM)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var firstDay = DateArgParser.ParseMonthOrCurrent(ctx.Args);
        var ym = DateArgParser.FormatMonth(firstDay);

        var records = (await _salary.GetMonthlySalaryTableAsync(ym))
            .OrderBy(r => r.EmployeeName)
            .ToList();

        var headers = new[] { "직원", "근무", "기본급", "식비/택시", "공휴일", "총지급", "원천", "실수령" };
        var rows = new List<IReadOnlyList<string>>();

        long sumBase = 0, sumGross = 0, sumNet = 0, sumTax = 0;
        foreach (var r in records)
        {
            sumBase += r.BaseSalary;
            sumGross += r.GrossSalary;
            sumNet += r.NetSalary;
            sumTax += r.Tax33;

            rows.Add(new[]
            {
                r.EmployeeName ?? $"#{r.EmployeeId}",
                $"{r.TotalHours:0.0}h",
                $"{r.BaseSalary:N0}",
                $"{r.MealAllowance:N0}/{r.TaxiAllowance:N0}",
                r.HolidayBonus > 0 ? $"{r.HolidayBonus:N0}" : "-",
                $"{r.GrossSalary:N0}",
                $"{r.Tax33:N0}",
                $"{r.NetSalary:N0}"
            });
        }

        if (records.Count > 0)
        {
            rows.Add(new[]
            {
                "합계", "-", $"{sumBase:N0}", "-", "-",
                $"{sumGross:N0}", $"{sumTax:N0}", $"{sumNet:N0}"
            });
        }

        using var stream = _renderer.RenderTable(
            $"급여 요약 — {records.Count}명",
            $"{firstDay:yyyy-MM} ({firstDay.Year}년 {firstDay.Month}월)",
            headers,
            rows,
            $"조회 시각: {DateTime.Now:yyyy-MM-dd HH:mm} · 단위: 원");

        var photo = InputFile.FromStream(stream, $"salary-{ym}.png");
        await ctx.Client.SendPhoto(ctx.ChatId, photo, cancellationToken: ctx.CancellationToken);
    }
}
