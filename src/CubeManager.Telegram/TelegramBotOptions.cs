namespace CubeManager.Telegram;

/// <summary>
/// 텔레그램 봇 런타임 옵션 (DB에서 로드된 평문 상태).
/// </summary>
public sealed class TelegramBotOptions
{
    public string Token { get; init; } = string.Empty;
    public IReadOnlyList<long> AllowedChatIds { get; init; } = Array.Empty<long>();
    public bool Enabled { get; init; }

    public bool IsConfigured => Enabled
        && !string.IsNullOrWhiteSpace(Token)
        && AllowedChatIds.Count > 0;

    /// <summary>
    /// "12345, -100123, 67890" 형태 문자열 → long[] 파싱.
    /// 숫자가 아닌 항목은 무시.
    /// </summary>
    public static IReadOnlyList<long> ParseChatIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<long>();
        return raw
            .Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => long.TryParse(s, out var v) ? (long?)v : null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .ToArray();
    }

    public static string FormatChatIds(IReadOnlyList<long> ids) =>
        string.Join(", ", ids);
}
