using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>
/// 뉴모피즘 + 글래스모피즘 근사 카드.
/// 밝은/어두운 그림자 쌍으로 입체감, 반투명 테두리로 깊이감.
/// 성능: GDI+ DrawLine/FillPath만 사용 — CPU 0%.
/// </summary>
public class RoundedCard : Panel
{
    public int Radius { get; set; } = 10;
    public bool ShowShadow { get; set; } = true;
    public bool GlassEffect { get; set; } = false;

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

        // 카드 영역 (그림자 공간 확보)
        var cardRect = new Rectangle(2, 2, Width - 5, Height - 5);
        using var path = CreateRoundedPath(cardRect, Radius);

        if (ShowShadow)
        {
            // ── 뉴모피즘: 밝은 그림자 (좌상) + 어두운 그림자 (우하) ──
            // 어두운 그림자 (우측+하단)
            var shadowRect = new Rectangle(cardRect.X + 2, cardRect.Y + 2, cardRect.Width, cardRect.Height);
            using var shadowPath = CreateRoundedPath(shadowRect, Radius);
            using var darkPen = new Pen(ColorPalette.NeuDark, 1.5f);
            g.DrawPath(darkPen, shadowPath);

            // 밝은 그림자 (좌측+상단) — 라이트모드에서만 보임
            if (!ColorPalette.IsDark)
            {
                var lightRect = new Rectangle(cardRect.X - 1, cardRect.Y - 1, cardRect.Width, cardRect.Height);
                using var lightPath = CreateRoundedPath(lightRect, Radius);
                using var lightPen = new Pen(ColorPalette.NeuLight, 1f);
                g.DrawPath(lightPen, lightPath);
            }
        }

        // ── 카드 배경 ──
        if (GlassEffect)
        {
            // 글래스모피즘 근사: 반투명 배경
            using var glassBrush = new SolidBrush(ColorPalette.GlassBg);
            g.FillPath(glassBrush, path);

            // 상단 흰 하이라이트 (유리 반사 느낌)
            var highlightRect = new Rectangle(cardRect.X, cardRect.Y, cardRect.Width, cardRect.Height / 3);
            using var highlightPath = CreateRoundedPath(highlightRect, Radius);
            using var highlightBrush = new SolidBrush(Color.FromArgb(15, 255, 255, 255));
            g.FillPath(highlightBrush, highlightPath);

            // 흰 테두리 (글래스 경계)
            using var glassBorder = new Pen(ColorPalette.GlassBorder, 1f);
            g.DrawPath(glassBorder, path);
        }
        else
        {
            // 일반 카드 배경
            using var bg = new SolidBrush(ColorPalette.Surface);
            g.FillPath(bg, path);

            // 미세 테두리
            using var border = new Pen(ColorPalette.Border, 0.5f);
            g.DrawPath(border, path);
        }
    }

    /// <summary>둥근 모서리 경로 생성</summary>
    public static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        return path;
    }
}
