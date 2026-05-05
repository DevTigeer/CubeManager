using System.Runtime.Versioning;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Telegram.Commands.Common;
using CubeManager.Telegram.Imaging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CubeManager.Telegram.Commands.Reports;

[SupportedOSPlatform("windows")]
public sealed class ChecklistCommandHandler : ICommandHandler
{
    private readonly IChecklistRepository _repo;
    private readonly IBotImageRenderer _renderer;

    public ChecklistCommandHandler(IChecklistRepository repo, IBotImageRenderer renderer)
    {
        _repo = repo;
        _renderer = renderer;
    }

    public string Command => "checklist";
    public IReadOnlyList<string> Aliases => new[] { "체크리스트" };
    public string Description => "체크리스트 진행률 (인자 없음=오늘, /checklist YYYY-MM-DD)";

    public async Task HandleAsync(CommandContext ctx)
    {
        var date = DateArgParser.ParseSingleDateOrToday(ctx.Args);
        var dateStr = DateArgParser.FormatDate(date);

        var records = (await _repo.GetRecordsForDateAsync(dateStr))
            .OrderBy(r => r.Role).ThenBy(r => r.SortOrder)
            .ToList();

        var done = records.Count(r => r.IsChecked);
        var total = records.Count;
        var rate = total == 0 ? 0 : (int)Math.Round(done * 100.0 / total);

        var headers = new[] { "역할", "항목", "완료", "체크자" };
        var rows = new List<IReadOnlyList<string>>();
        foreach (var r in records)
        {
            rows.Add(new[]
            {
                FormatRole(r.Role),
                r.TaskText ?? "-",
                r.IsChecked ? "O" : "·",
                r.IsChecked ? (r.CheckedBy ?? "-") : "-"
            });
        }

        using var stream = _renderer.RenderTable(
            $"체크리스트 — {done}/{total} ({rate}%)",
            DateArgParser.FormatDateKorean(date),
            headers,
            rows,
            $"조회 시각: {DateTime.Now:yyyy-MM-dd HH:mm}");

        var photo = InputFile.FromStream(stream, $"checklist-{dateStr}.png");
        await ctx.Client.SendPhoto(ctx.ChatId, photo, cancellationToken: ctx.CancellationToken);
    }

    private static string FormatRole(string? role) => role switch
    {
        "open" => "오픈",
        "close" => "마감",
        "middle1" => "미들1",
        "middle2" => "미들2",
        "all" => "공통",
        _ => role ?? "-"
    };
}
