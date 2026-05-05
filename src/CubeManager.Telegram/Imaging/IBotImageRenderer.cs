using System.Runtime.Versioning;

namespace CubeManager.Telegram.Imaging;

[SupportedOSPlatform("windows")]
public interface IBotImageRenderer
{
    /// <summary>제목 + 키-값 행들. 카드형 PNG.</summary>
    Stream RenderKeyValueCard(string title, string? subtitle, IReadOnlyList<KvRow> rows, string? footer = null);

    /// <summary>제목 + 표 헤더 + 행들. 표형 PNG.</summary>
    Stream RenderTable(string title, string? subtitle, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, string? footer = null);
}

public sealed record KvRow(string Label, string Value, string? Accent = null);
