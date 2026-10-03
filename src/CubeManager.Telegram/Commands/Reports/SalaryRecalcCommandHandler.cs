using System.Runtime.Versioning;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Serilog;
using Telegram.Bot;

namespace CubeManager.Telegram.Commands.Reports;

/// <summary>
/// 월 급여 재계산 후 급여표 전송. 앱 급여탭 "재계산" 버튼과 동일 (수기 수정 행은 유지).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SalaryRecalcCommandHandler : ICommandHandler
{
    private readonly ISalaryService _salary;
    private readonly IBotImageRenderer _renderer;

    public SalaryRecalcCommandHandler(ISalaryService salary, IBotImageRenderer renderer)
    {
        _salary = salary;
        _renderer = renderer;
    }

    public string Command => "salary_recalc";
    public IReadOnlyList<string> Aliases => new[] { "급여계산", "급여재계산" };
    public string Description => "월 급여 재계산 후 급여표 (인자 없음=이번달, /salary_recalc YYYY-MM)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var firstDay = DateArgParser.ParseMonthOrCurrent(ctx.Args);
        var ym = DateArgParser.FormatMonth(firstDay);

        await _salary.CalculateAllAsync(ym);
        Log.Information("[AUDIT] Telegram 급여 재계산: chat={ChatId} month={Month}", ctx.ChatId, ym);

        await SalaryCommandHandler.SendSalaryTableAsync(ctx, _salary, _renderer, firstDay, ym);
    }
}
