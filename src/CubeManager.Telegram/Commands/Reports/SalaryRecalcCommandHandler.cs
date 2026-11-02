using System.Runtime.Versioning;
using System.Text;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Telegram.Commands.Common;
using Serilog;
using Telegram.Bot;

namespace CubeManager.Telegram.Commands.Reports;

/// <summary>
/// 월 급여 재계산 (앱 급여탭 "재계산"과 동일 — 수기 수정 행·비활성 직원은 제외).
/// 월 범위를 지원하고, 재계산 전후로 바뀐 금액을 직원별 텍스트로 회신.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SalaryRecalcCommandHandler : ICommandHandler
{
    private const int MaxMonths = 12;
    private const int MaxMessageLength = 3500; // 텔레그램 4096자 제한 여유

    private readonly ISalaryService _salary;
    private readonly IEmployeeService _employee;

    public SalaryRecalcCommandHandler(ISalaryService salary, IEmployeeService employee)
    {
        _salary = salary;
        _employee = employee;
    }

    public string Command => "salary_recalc";
    public IReadOnlyList<string> Aliases => new[] { "급여계산", "급여재계산" };
    public string Description => "급여 재계산 + 변동 내역 (인자 없음=이번달, /salary_recalc YYYY-MM 또는 YYYY-MM~YYYY-MM)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (!DateArgParser.TryParseMonthRange(ctx.Args, today, out var from, out var to))
        {
            await ctx.Client.SendMessage(ctx.ChatId,
                "월 형식이 올바르지 않습니다.\n예) /급여계산 2026-10\n예) /급여계산 2026-05~2026-10",
                cancellationToken: ctx.CancellationToken);
            return;
        }

        var monthCount = (to.Year - from.Year) * 12 + to.Month - from.Month + 1;
        if (monthCount > MaxMonths)
        {
            await ctx.Client.SendMessage(ctx.ChatId, $"한 번에 최대 {MaxMonths}개월까지 재계산할 수 있습니다.",
                cancellationToken: ctx.CancellationToken);
            return;
        }

        var activeIds = (await _employee.GetActiveAsync()).Select(e => e.Id).ToHashSet();

        var blocks = new List<string>();
        for (var m = from; m <= to; m = m.AddMonths(1))
        {
            var ym = DateArgParser.FormatMonth(m);
            var before = (await _salary.GetMonthlySalaryTableAsync(ym)).ToDictionary(r => r.EmployeeId);
            await RecalculateAsync(ctx, _salary, ym);
            var after = (await _salary.GetMonthlySalaryTableAsync(ym)).ToList();
            blocks.Add(BuildMonthSummary(ym, before, after, activeIds));
        }

        // 4096자 제한 → 월 단위로 묶어 나눠 전송
        var sb = new StringBuilder("💰 급여 재계산 완료\n");
        foreach (var block in blocks)
        {
            if (sb.Length + block.Length > MaxMessageLength)
            {
                await ctx.Client.SendMessage(ctx.ChatId, sb.ToString(), cancellationToken: ctx.CancellationToken);
                sb.Clear();
            }
            sb.AppendLine().Append(block);
        }
        await ctx.Client.SendMessage(ctx.ChatId, sb.ToString(), cancellationToken: ctx.CancellationToken);
    }

    /// <summary>월 급여 재계산 + 감사 로그. /급여 에서도 재사용.</summary>
    internal static async Task RecalculateAsync(CommandContext ctx, ISalaryService salary, string ym)
    {
        await salary.CalculateAllAsync(ym);
        Log.Information("[AUDIT] Telegram 급여 재계산: chat={ChatId} month={Month}", ctx.ChatId, ym);
    }

    private static string BuildMonthSummary(
        string ym, Dictionary<int, SalaryRecord> before, List<SalaryRecord> after, HashSet<int> activeIds)
    {
        var sb = new StringBuilder();
        if (after.Count == 0)
        {
            sb.AppendLine($"[{ym}] 급여 기록 없음");
            return sb.ToString();
        }

        long grossBefore = before.Values.Sum(r => (long)r.GrossSalary);
        long grossAfter = after.Sum(r => (long)r.GrossSalary);
        sb.AppendLine($"[{ym}] {after.Count}명 · 총지급 {grossBefore:N0} → {grossAfter:N0} ({FormatDiff(grossAfter - grossBefore)})");

        var unchanged = 0;
        var skipped = new List<string>();
        foreach (var r in after.OrderBy(r => r.EmployeeName))
        {
            var name = r.EmployeeName ?? $"#{r.EmployeeId}";
            if (r.IsManualEdit) { skipped.Add($"{name}(수기 수정)"); continue; }
            if (!activeIds.Contains(r.EmployeeId)) { skipped.Add($"{name}(비활성 직원)"); continue; }

            if (!before.TryGetValue(r.EmployeeId, out var b))
            {
                sb.AppendLine($" · {name} 신규 총지급 {r.GrossSalary:N0}");
                continue;
            }

            var changes = new List<string>();
            AddChange(changes, "기본급", b.BaseSalary, r.BaseSalary);
            AddChange(changes, "공휴일수당", b.HolidayBonus, r.HolidayBonus);
            AddChange(changes, "식비", b.MealAllowance, r.MealAllowance);
            AddChange(changes, "택시비", b.TaxiAllowance, r.TaxiAllowance);
            if (changes.Count == 0) { unchanged++; continue; }

            sb.AppendLine($" · {name} 총지급 {FormatDiff(r.GrossSalary - b.GrossSalary)} ({string.Join(", ", changes)})");
        }

        if (unchanged > 0) sb.AppendLine($" · 변동 없음 {unchanged}명");
        if (skipped.Count > 0) sb.AppendLine($" · 재계산 제외: {string.Join(", ", skipped)}");
        return sb.ToString();
    }

    private static void AddChange(List<string> changes, string label, int before, int after)
    {
        if (before != after) changes.Add($"{label} {before:N0} → {after:N0}");
    }

    private static string FormatDiff(long diff) => diff switch
    {
        > 0 => $"+{diff:N0}",
        < 0 => $"{diff:N0}",
        _ => "변동 없음"
    };
}
