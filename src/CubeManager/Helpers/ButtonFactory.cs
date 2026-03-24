using System.Drawing;
using System.Drawing.Drawing2D;

namespace CubeManager.Helpers;

/// <summary>
/// 2025 Dark Tone 버튼 팩토리.
/// 뉴모피즘 입체감 + Segoe UI 가독성 폰트 + 8px 라운드.
/// </summary>
public static class ButtonFactory
{
    private const int DefaultHeight = 36;
    private const int MinWidth = 80;
    private const int Radius = 8;

    /// <summary>파란 배경 주요 버튼 (액센트)</summary>
    public static Button CreatePrimary(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Primary, Color.White, width);
        btn.Font = DesignTokens.FontButton;
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.Primary700;
        btn.FlatAppearance.MouseDownBackColor = ColorPalette.Primary900;
        return btn;
    }

    /// <summary>보조 버튼 (어두운 배경 + 밝은 테두리)</summary>
    public static Button CreateSecondary(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Card, ColorPalette.Text, width);
        btn.Font = DesignTokens.FontBody;
        btn.FlatAppearance.BorderColor = ColorPalette.Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.HoverBg;
        btn.FlatAppearance.MouseDownBackColor = ColorPalette.Border;
        return btn;
    }

    /// <summary>위험 버튼 (빨간 포인트)</summary>
    public static Button CreateDanger(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Danger, Color.White, width);
        btn.Font = DesignTokens.FontButton;
        btn.FlatAppearance.MouseOverBackColor = DarkenColor(ColorPalette.Danger, 25);
        btn.FlatAppearance.MouseDownBackColor = DarkenColor(ColorPalette.Danger, 50);
        return btn;
    }

    /// <summary>성공 버튼 (초록 포인트)</summary>
    public static Button CreateSuccess(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Success, Color.White, width);
        btn.Font = DesignTokens.FontButton;
        btn.FlatAppearance.MouseOverBackColor = DarkenColor(ColorPalette.Success, 25);
        btn.FlatAppearance.MouseDownBackColor = DarkenColor(ColorPalette.Success, 50);
        return btn;
    }

    /// <summary>Ghost 버튼 (투명 + 밝은 텍스트)</summary>
    public static Button CreateGhost(string text, int width = 0)
    {
        var btn = CreateBase(text, Color.Transparent, ColorPalette.TextSecondary, width);
        btn.Font = DesignTokens.FontBody;
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.HoverBg;
        btn.FlatAppearance.MouseDownBackColor = ColorPalette.Border;
        return btn;
    }

    /// <summary>날짜 네비게이션 (◀ ▶)</summary>
    public static Button CreateNavArrow(string arrow)
    {
        var btn = CreateGhost(arrow, 36);
        btn.Font = new Font("맑은 고딕", 12f);
        btn.ForeColor = ColorPalette.Text;
        return btn;
    }

    /// <summary>아이콘 버튼 (32x32)</summary>
    public static Button CreateIcon(string icon, Color? color = null)
    {
        var btn = CreateGhost(icon, 34);
        btn.Height = 34;
        btn.Font = new Font("Segoe UI Emoji", 13f);
        btn.ForeColor = color ?? ColorPalette.Text;
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
            Font = new Font("Segoe UI", 10f),
            Height = DefaultHeight,
            Width = width > 0 ? width : Math.Max(MinWidth, text.Length * 14 + 36),
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 4, 0)
        };
        btn.FlatAppearance.BorderSize = 0;

        // 뉴모피즘 Pressed: 1px 눌림 + 배경 약간 어두워짐
        btn.MouseDown += (_, _) => btn.Padding = new Padding(0, 1, 0, 0);
        btn.MouseUp += (_, _) => btn.Padding = Padding.Empty;

        // Focus 표시 제거 (BorderSize 변경이 repaint 잔상 유발)

        // Disabled: 투명도
        btn.EnabledChanged += (_, _) =>
        {
            if (!btn.Enabled)
            {
                btn.BackColor = Color.FromArgb(80, backColor);
                btn.ForeColor = Color.FromArgb(80, foreColor);
                btn.Cursor = Cursors.Default;
            }
            else
            {
                btn.BackColor = backColor;
                btn.ForeColor = foreColor;
                btn.Cursor = Cursors.Hand;
            }
        };

        // 둥근 모서리 8px
        btn.Resize += (_, _) => ApplyRoundedRegion(btn);
        btn.HandleCreated += (_, _) => ApplyRoundedRegion(btn);

        return btn;
    }

    /// <summary>로딩 표시 헬퍼</summary>
    public static async Task RunWithLoadingAsync(Button btn, string loadingText, Func<Task> action)
    {
        var originalText = btn.Text;
        var originalEnabled = btn.Enabled;
        try
        {
            btn.Enabled = false;
            btn.Text = loadingText;
            btn.Cursor = Cursors.WaitCursor;
            await action();
        }
        finally
        {
            btn.Text = originalText;
            btn.Enabled = originalEnabled;
            btn.Cursor = Cursors.Hand;
        }
    }

    private static void ApplyRoundedRegion(Button btn)
    {
        if (btn.Width <= 0 || btn.Height <= 0) return;
        using var path = new GraphicsPath();
        var d = Radius * 2;
        var rect = new Rectangle(0, 0, btn.Width, btn.Height);
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        btn.Region = new Region(path);
    }

    private static Color DarkenColor(Color c, int amount) => Color.FromArgb(
        c.A, Math.Max(c.R - amount, 0), Math.Max(c.G - amount, 0), Math.Max(c.B - amount, 0));
}
