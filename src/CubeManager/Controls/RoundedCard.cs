using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>
/// 2025 트렌드 — 8px 둥근 모서리 카드 패널.
/// 하단 1px 미세 그림자로 절제된 깊이감 표현.
/// Fluent Design의 Surface 카드를 WinForms GDI+로 구현.
/// </summary>
public class RoundedCard : Panel
{
    public int Radius { get; set; } = 8;
    public bool ShowShadow { get; set; } = true;

    public RoundedCard()
    {
        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw, true);

        BackColor = Color.Transparent;
        Padding = new Padding(16);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // 카드 영역 (하단 1px 여유 — 그림자용)
        var cardRect = new Rectangle(0, 0, Width - 1, Height - (ShowShadow ? 2 : 1));

        using var path = CreateRoundedPath(cardRect, Radius);

        // 미세 그림자 (하단 1px)
        if (ShowShadow)
        {
            using var shadowPen = new Pen(ColorPalette.ShadowLight);
            g.DrawLine(shadowPen,
                Radius, cardRect.Bottom + 1,
                cardRect.Right - Radius, cardRect.Bottom + 1);
        }

        // 카드 배경
        using var bg = new SolidBrush(ColorPalette.Surface);
        g.FillPath(bg, path);

        // 테두리
        using var border = new Pen(ColorPalette.Border);
        g.DrawPath(border, path);
    }

    /// <summary>둥근 모서리 경로 생성 (GDI+ 최경량 연산)</summary>
    public static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);                   // 좌상
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);            // 우상
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);     // 우하
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);            // 좌하
        path.CloseFigure();

        return path;
    }
}
