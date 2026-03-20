using System.Drawing;
using System.Drawing.Drawing2D;

namespace CubeManager.Helpers;

/// <summary>
/// 디자인 시스템 기반 표준 버튼 생성 팩토리.
/// 2025 업데이트: 둥근 모서리(6px), 호버 효과, 다크 모드 대응.
/// </summary>
public static class ButtonFactory
{
    private const int DefaultHeight = 34;
    private const int MinWidth = 76;
    private const int Radius = 6;

    /// <summary>파란 배경 주요 버튼 (강조)</summary>
    public static Button CreatePrimary(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Primary, ColorPalette.TextWhite, width);
        btn.Font = new Font("맑은 고딕", 10f, FontStyle.Bold);
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.Primary700;
        return btn;
    }

    /// <summary>테두리 보조 버튼</summary>
    public static Button CreateSecondary(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Surface, ColorPalette.Primary, width);
        btn.FlatAppearance.BorderColor = ColorPalette.Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.Primary50;
        return btn;
    }

    /// <summary>위험 액션 버튼 (빨간 배경)</summary>
    public static Button CreateDanger(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Danger, ColorPalette.TextWhite, width);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(211, 47, 47);
        return btn;
    }

    /// <summary>성공/확인 버튼 (초록 배경)</summary>
    public static Button CreateSuccess(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Success, ColorPalette.TextWhite, width);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(56, 142, 60);
        return btn;
    }

    /// <summary>투명 텍스트 버튼 (Ghost)</summary>
    public static Button CreateGhost(string text, int width = 0)
    {
        var btn = CreateBase(text, Color.Transparent, ColorPalette.TextSecondary, width);
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.NavHoverBg;
        btn.FlatAppearance.MouseDownBackColor = ColorPalette.Border;
        return btn;
    }

    /// <summary>날짜 네비게이션 화살표 버튼 (◀ ▶)</summary>
    public static Button CreateNavArrow(string arrow)
    {
        var btn = CreateGhost(arrow, 36);
        btn.Font = new Font("맑은 고딕", 11f);
        return btn;
    }

    /// <summary>아이콘 전용 소형 버튼 (32x32)</summary>
    public static Button CreateIcon(string icon, Color? color = null)
    {
        var btn = CreateGhost(icon, 32);
        btn.Height = 32;
        btn.Font = new Font("Segoe UI Emoji", 12f);
        if (color.HasValue) btn.ForeColor = color.Value;
        return btn;
    }

    private static Button CreateBase(string text, Color backColor, Color foreColor, int width)
    {
        var btn = new Button
        {
            Text = text,
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("맑은 고딕", 10f),
            Height = DefaultHeight,
            Width = width > 0 ? width : Math.Max(MinWidth, text.Length * 13 + 32),
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 4, 0)
        };
        btn.FlatAppearance.BorderSize = 0;

        // 둥근 모서리 (Region 기반 — 성능 영향 0)
        btn.Resize += (_, _) => ApplyRoundedRegion(btn);
        btn.HandleCreated += (_, _) => ApplyRoundedRegion(btn);

        return btn;
    }

    private static void ApplyRoundedRegion(Button btn)
    {
        if (btn.Width <= 0 || btn.Height <= 0) return;
        var path = new GraphicsPath();
        var d = Radius * 2;
        var rect = new Rectangle(0, 0, btn.Width, btn.Height);
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        btn.Region = new Region(path);
    }
}
