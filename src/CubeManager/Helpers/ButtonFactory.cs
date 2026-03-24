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

    /// <summary>테두리 보조 버튼 (무채색 기본)</summary>
    public static Button CreateSecondary(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Surface, ColorPalette.Text, width);
        btn.FlatAppearance.BorderColor = ColorPalette.Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.HoverBg;
        return btn;
    }

    /// <summary>위험 액션 버튼 (빨간 포인트)</summary>
    public static Button CreateDanger(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Danger, ColorPalette.TextWhite, width);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
            Math.Max(ColorPalette.Danger.R - 20, 0),
            Math.Max(ColorPalette.Danger.G - 20, 0),
            Math.Max(ColorPalette.Danger.B - 20, 0));
        return btn;
    }

    /// <summary>성공/확인 버튼 (초록 포인트)</summary>
    public static Button CreateSuccess(string text, int width = 0)
    {
        var btn = CreateBase(text, ColorPalette.Success, ColorPalette.TextWhite, width);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
            Math.Max(ColorPalette.Success.R - 20, 0),
            Math.Max(ColorPalette.Success.G - 20, 0),
            Math.Max(ColorPalette.Success.B - 20, 0));
        return btn;
    }

    /// <summary>투명 텍스트 버튼 (무채색 Ghost)</summary>
    public static Button CreateGhost(string text, int width = 0)
    {
        var btn = CreateBase(text, Color.Transparent, ColorPalette.TextSecondary, width);
        btn.FlatAppearance.MouseOverBackColor = ColorPalette.HoverBg;
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
            Margin = new Padding(DesignTokens.SpaceXS, 0, DesignTokens.SpaceXS, 0)
        };
        btn.FlatAppearance.BorderSize = 0;

        // Pressed 효과: 살짝 눌림감 (1px 오프셋)
        btn.MouseDown += (_, _) => btn.Padding = new Padding(0, 1, 0, 0);
        btn.MouseUp += (_, _) => btn.Padding = Padding.Empty;

        // Focus 효과: Primary 테두리 (키보드 접근성)
        btn.GotFocus += (_, _) =>
        {
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.BorderColor = ColorPalette.Primary;
        };
        btn.LostFocus += (_, _) =>
        {
            btn.FlatAppearance.BorderSize = 0;
        };

        // Disabled 상태: 투명도 낮춤
        btn.EnabledChanged += (_, _) =>
        {
            if (!btn.Enabled)
            {
                btn.BackColor = Color.FromArgb(100, backColor);
                btn.ForeColor = Color.FromArgb(100, foreColor);
                btn.Cursor = Cursors.Default;
            }
            else
            {
                btn.BackColor = backColor;
                btn.ForeColor = foreColor;
                btn.Cursor = Cursors.Hand;
            }
        };

        // 둥근 모서리 (Region 기반 — 성능 영향 0)
        btn.Resize += (_, _) => ApplyRoundedRegion(btn);
        btn.HandleCreated += (_, _) => ApplyRoundedRegion(btn);

        return btn;
    }

    /// <summary>
    /// 비동기 작업 중 버튼 비활성화 + 텍스트 변경 (로딩 표시).
    /// 사용법: await ButtonFactory.RunWithLoadingAsync(btn, "처리 중...", async () => { ... });
    /// </summary>
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
