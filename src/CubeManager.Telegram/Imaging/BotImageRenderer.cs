using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace CubeManager.Telegram.Imaging;

[SupportedOSPlatform("windows")]
public sealed class BotImageRenderer : IBotImageRenderer
{
    private const int Width = 800;
    private const int Padding = 24;
    private const string FontFamily = "맑은 고딕";

    private static readonly Color BgColor = Color.FromArgb(0xFA, 0xFB, 0xFC);
    private static readonly Color CardColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(0xE2, 0xE6, 0xEA);
    private static readonly Color TextColor = Color.FromArgb(0x21, 0x29, 0x33);
    private static readonly Color SubTextColor = Color.FromArgb(0x6C, 0x75, 0x7D);
    private static readonly Color HeaderBgColor = Color.FromArgb(0xF1, 0xF3, 0xF5);
    private static readonly Color AccentColor = Color.FromArgb(0x1E, 0x88, 0xE5);

    public Stream RenderKeyValueCard(string title, string? subtitle, IReadOnlyList<KvRow> rows, string? footer = null)
    {
        const int rowHeight = 44;
        var titleH = string.IsNullOrEmpty(subtitle) ? 56 : 80;
        var footerH = string.IsNullOrEmpty(footer) ? 0 : 32;
        var height = Padding + titleH + 12 + rows.Count * rowHeight + 12 + footerH + Padding;

        using var bmp = new Bitmap(Width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BgColor);

        var cardRect = new Rectangle(Padding, Padding, Width - Padding * 2, height - Padding * 2);
        DrawCard(g, cardRect);

        using var titleFont = new Font(FontFamily, 18f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var subtitleFont = new Font(FontFamily, 13f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var labelFont = new Font(FontFamily, 14f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var valueFont = new Font(FontFamily, 16f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var footerFont = new Font(FontFamily, 11f, FontStyle.Regular, GraphicsUnit.Pixel);

        var contentLeft = cardRect.Left + 24;
        var contentRight = cardRect.Right - 24;
        var y = cardRect.Top + 20;

        using (var titleBrush = new SolidBrush(TextColor))
            g.DrawString(title, titleFont, titleBrush, contentLeft, y);
        y += 28;

        if (!string.IsNullOrEmpty(subtitle))
        {
            using var subBrush = new SolidBrush(SubTextColor);
            g.DrawString(subtitle, subtitleFont, subBrush, contentLeft, y);
            y += 24;
        }
        y += 8;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (i > 0)
            {
                using var pen = new Pen(BorderColor, 1f);
                g.DrawLine(pen, contentLeft, y, contentRight, y);
            }
            var rowTop = y + 8;

            using (var labelBrush = new SolidBrush(SubTextColor))
                g.DrawString(row.Label, labelFont, labelBrush, contentLeft, rowTop);

            var valueColor = TryParseColor(row.Accent) ?? TextColor;
            using (var valueBrush = new SolidBrush(valueColor))
            {
                var size = g.MeasureString(row.Value, valueFont);
                g.DrawString(row.Value, valueFont, valueBrush, contentRight - size.Width, rowTop);
            }
            y += rowHeight;
        }

        if (!string.IsNullOrEmpty(footer))
        {
            using var footerBrush = new SolidBrush(SubTextColor);
            g.DrawString(footer, footerFont, footerBrush, contentLeft, cardRect.Bottom - 24);
        }

        return ToPngStream(bmp);
    }

    public Stream RenderTable(string title, string? subtitle, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, string? footer = null)
    {
        const int rowHeight = 36;
        const int headerHeight = 40;
        var titleH = string.IsNullOrEmpty(subtitle) ? 56 : 80;
        var footerH = string.IsNullOrEmpty(footer) ? 0 : 32;
        var emptyH = rows.Count == 0 ? rowHeight : 0;
        var height = Padding + titleH + 12 + headerHeight + rows.Count * rowHeight + emptyH + 12 + footerH + Padding;

        using var bmp = new Bitmap(Width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BgColor);

        var cardRect = new Rectangle(Padding, Padding, Width - Padding * 2, height - Padding * 2);
        DrawCard(g, cardRect);

        using var titleFont = new Font(FontFamily, 18f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var subtitleFont = new Font(FontFamily, 13f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var headerFont = new Font(FontFamily, 13f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var cellFont = new Font(FontFamily, 13f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var footerFont = new Font(FontFamily, 11f, FontStyle.Regular, GraphicsUnit.Pixel);

        var contentLeft = cardRect.Left + 24;
        var contentRight = cardRect.Right - 24;
        var y = cardRect.Top + 20;

        using (var titleBrush = new SolidBrush(TextColor))
            g.DrawString(title, titleFont, titleBrush, contentLeft, y);
        y += 28;
        if (!string.IsNullOrEmpty(subtitle))
        {
            using var subBrush = new SolidBrush(SubTextColor);
            g.DrawString(subtitle, subtitleFont, subBrush, contentLeft, y);
            y += 24;
        }
        y += 8;

        var colCount = headers.Count;
        var tableWidth = contentRight - contentLeft;
        var colWidth = tableWidth / colCount;

        // 헤더 배경
        using (var headerBg = new SolidBrush(HeaderBgColor))
            g.FillRectangle(headerBg, contentLeft, y, tableWidth, headerHeight);
        using (var pen = new Pen(BorderColor, 1f))
            g.DrawRectangle(pen, contentLeft, y, tableWidth, headerHeight);

        for (var c = 0; c < colCount; c++)
        {
            using var headerBrush = new SolidBrush(TextColor);
            g.DrawString(headers[c], headerFont, headerBrush,
                new RectangleF(contentLeft + c * colWidth + 8, y + 10, colWidth - 16, headerHeight),
                StringFormat.GenericDefault);
        }
        y += headerHeight;

        if (rows.Count == 0)
        {
            using var emptyBrush = new SolidBrush(SubTextColor);
            var msg = "데이터 없음";
            var size = g.MeasureString(msg, cellFont);
            g.DrawString(msg, cellFont, emptyBrush,
                contentLeft + (tableWidth - size.Width) / 2, y + 10);
            y += rowHeight;
        }
        else
        {
            for (var r = 0; r < rows.Count; r++)
            {
                if (r % 2 == 1)
                {
                    using var stripeBrush = new SolidBrush(Color.FromArgb(0xF8, 0xF9, 0xFA));
                    g.FillRectangle(stripeBrush, contentLeft, y, tableWidth, rowHeight);
                }
                using (var pen = new Pen(BorderColor, 1f))
                    g.DrawLine(pen, contentLeft, y + rowHeight, contentRight, y + rowHeight);

                var row = rows[r];
                for (var c = 0; c < Math.Min(colCount, row.Count); c++)
                {
                    using var cellBrush = new SolidBrush(TextColor);
                    g.DrawString(row[c], cellFont, cellBrush,
                        new RectangleF(contentLeft + c * colWidth + 8, y + 8, colWidth - 16, rowHeight),
                        StringFormat.GenericDefault);
                }
                y += rowHeight;
            }
        }

        // 표 외곽선
        using (var pen = new Pen(BorderColor, 1f))
            g.DrawRectangle(pen, contentLeft, cardRect.Top + 20 + 28 + (string.IsNullOrEmpty(subtitle) ? 0 : 24) + 8,
                tableWidth, headerHeight + (rows.Count == 0 ? rowHeight : rows.Count * rowHeight));

        if (!string.IsNullOrEmpty(footer))
        {
            using var footerBrush = new SolidBrush(SubTextColor);
            g.DrawString(footer, footerFont, footerBrush, contentLeft, cardRect.Bottom - 24);
        }

        return ToPngStream(bmp);
    }

    private static void DrawCard(Graphics g, Rectangle rect)
    {
        using var bg = new SolidBrush(CardColor);
        g.FillRectangle(bg, rect);
        using var pen = new Pen(BorderColor, 1f);
        g.DrawRectangle(pen, rect);
    }

    private static Color? TryParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try { return ColorTranslator.FromHtml(hex); }
        catch { return null; }
    }

    private static MemoryStream ToPngStream(Bitmap bmp)
    {
        var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        return ms;
    }
}
