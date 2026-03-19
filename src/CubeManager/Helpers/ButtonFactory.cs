using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// 디자인 시스템 기반 표준 버튼 생성 팩토리.
/// Primary / Secondary / Danger / Ghost 4종.
/// </summary>
public static class ButtonFactory
{
    private static readonly Font BtnFont = new("맑은 고딕", 10f, FontStyle.Bold);
    private const int DefaultHeight = 36;
    private const int MinWidth = 80;
    private const int PaddingH = 24;

    /// <summary>파란 배경 주요 버튼</summary>
    public static Button CreatePrimary(string text, int width = 0) =>
        Create(text, ColorPalette.Primary, ColorPalette.TextWhite, width);

    /// <summary>테두리 보조 버튼</summary>
    public static Button CreateSecondary(string text, int width = 0)
    {
        var btn = Create(text, ColorPalette.Surface, ColorPalette.Primary, width);
        btn.FlatAppearance.BorderColor = ColorPalette.Primary;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.Primary50;
        return btn;
    }

    /// <summary>빨간 테두리 위험 버튼</summary>
    public static Button CreateDanger(string text, int width = 0)
    {
        var btn = Create(text, ColorPalette.Surface, ColorPalette.Danger, width);
        btn.FlatAppearance.BorderColor = ColorPalette.Danger;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.DangerLight;
        return btn;
    }

    /// <summary>투명 텍스트 버튼 (Ghost)</summary>
    public static Button CreateGhost(string text, int width = 0)
    {
        var btn = Create(text, Color.Transparent, ColorPalette.TextSecondary, width);
        btn.Height = 32;
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.NavHoverBg;
        return btn;
    }

    /// <summary>날짜 네비게이션 화살표 버튼 (◀ ▶)</summary>
    public static Button CreateNavArrow(string arrow) =>
        CreateGhost(arrow, 32);

    private static Button Create(string text, Color backColor, Color foreColor, int width)
    {
        var btn = new Button
        {
            Text = text,
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Font = BtnFont,
            Height = DefaultHeight,
            Width = width > 0 ? width : Math.Max(MinWidth, text.Length * 14 + PaddingH * 2),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }
}
