using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>
/// 좌측 사이드바 네비게이션. 상시 200px 고정.
/// GDI+ 렌더링. 2025 업데이트: Windows 11 NavigationView Pill 스타일.
/// </summary>
public class SideNavPanel : Panel
{
    public event Action<int>? TabSelected;

    private int _selectedIndex;
    private int _hoverIndex = -1;

    private const int NavWidth = 200;
    private const int ItemHeight = 48;
    private const int LogoHeight = 56;
    private const int IconAreaWidth = 48;
    private const int PillMarginX = 6;    // Pill 좌우 여백
    private const int PillRadius = 6;     // Pill 모서리 반지름
    private const int IndicatorWidth = 3; // 좌측 인디케이터 너비
    private const int IndicatorHeight = 16; // 좌측 인디케이터 높이
    private const int IndicatorRadius = 2; // 인디케이터 모서리

    private static readonly string[] Labels =
        ["예약/매출", "스케줄", "체크리스트", "출퇴근", "인수인계", "무료이용권", "물품", "업무자료", "테마힌트", "설정", "관리자"];

    // MDL2 아이콘 (Windows 10/11 내장 — 일관된 크기/스타일)
    private static readonly string[] Icons = DesignTokens.SideNavIcons;
    // 폴백: 이모지 (MDL2 미지원 환경)
    private static readonly string[] FallbackIcons =
        ["📅", "📋", "✅", "⏰", "📝", "🎫", "📦", "📄", "🔑", "⚙️", "🛡️"];
    private static readonly bool _useMdl2 = IsMdl2Available();

    public int SelectedIndex
    {
        get => _selectedIndex;
        set { _selectedIndex = value; Invalidate(); }
    }

    public SideNavPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        Width = NavWidth;
        Dock = DockStyle.Left;
        BackColor = ColorPalette.Background;  // 가장 어두운 배경
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var idx = HitTest(e.Y);
        if (idx != _hoverIndex)
        {
            _hoverIndex = idx;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverIndex = -1;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var idx = HitTest(e.Y);
        if (idx >= 0 && idx < Labels.Length)
        {
            _selectedIndex = idx;
            TabSelected?.Invoke(idx);
            Invalidate();
        }
    }

    private int HitTest(int y)
    {
        if (y < LogoHeight) return -1;
        var idx = (y - LogoHeight) / ItemHeight;
        return idx >= 0 && idx < Labels.Length ? idx : -1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // 배경 (가장 진한 영역)
        g.Clear(ColorPalette.Background);

        // 우측 border
        using var borderPen = new Pen(ColorPalette.Border, 1);
        g.DrawLine(borderPen, Width - 1, 0, Width - 1, Height);

        // 로고 영역
        DrawLogo(g);

        // 네비게이션 아이템들
        for (var i = 0; i < Labels.Length; i++)
        {
            DrawNavItem(g, i);
        }
    }

    private void DrawLogo(Graphics g)
    {
        // 로고 아이콘 (블랙 원형 배경 + 흰 텍스트)
        var iconBg = new Rectangle(12, 12, 32, 32);
        using var iconBgBrush = new SolidBrush(ColorPalette.Text);
        g.FillEllipse(iconBgBrush, iconBg);
        using var iconFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var whiteBrush = new SolidBrush(ColorPalette.Surface);
        g.DrawString("C", iconFont, whiteBrush, 20, 17);

        // 로고 텍스트 (무채색)
        using var logoBrush = new SolidBrush(ColorPalette.Text);
        using var logoFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        g.DrawString("CubeManager", logoFont, logoBrush, 48, 18);

        // 하단 구분선
        using var divPen = new Pen(ColorPalette.Divider, 1);
        g.DrawLine(divPen, 12, LogoHeight - 1, Width - 12, LogoHeight - 1);
    }

    private void DrawNavItem(Graphics g, int index)
    {
        var y = LogoHeight + index * ItemHeight;
        var isSelected = index == _selectedIndex;
        var isHover = index == _hoverIndex && !isSelected;

        // Pill 배경 영역 (양쪽 마진)
        var pillRect = new Rectangle(
            PillMarginX,
            y + 4,
            Width - PillMarginX * 2 - 1,
            ItemHeight - 8);

        // 배경: Pill 형태
        if (isSelected)
        {
            using var pillPath = CreateRoundedPath(pillRect, PillRadius);
            using var selBrush = new SolidBrush(ColorPalette.NavActiveBg);
            g.FillPath(selBrush, pillPath);

            // 좌측 둥근 인디케이터 (Windows 11 스타일)
            var indicatorY = y + (ItemHeight - IndicatorHeight) / 2;
            var indicatorRect = new Rectangle(2, indicatorY, IndicatorWidth, IndicatorHeight);
            using var indicatorPath = CreateRoundedPath(indicatorRect, IndicatorRadius);
            using var barBrush = new SolidBrush(ColorPalette.NavActive);
            g.FillPath(barBrush, indicatorPath);
        }
        else if (isHover)
        {
            using var pillPath = CreateRoundedPath(pillRect, PillRadius);
            using var hoverBrush = new SolidBrush(ColorPalette.NavHoverBg);
            g.FillPath(hoverBrush, pillPath);
        }

        // 아이콘 색상
        var iconColor = isSelected ? ColorPalette.NavActive :
                        isHover ? ColorPalette.NavHover :
                        ColorPalette.NavDefault;

        // 아이콘 (MDL2 우선, 폴백=이모지)
        var icons = _useMdl2 ? Icons : FallbackIcons;
        using var iconFont = _useMdl2
            ? new Font("Segoe MDL2 Assets", 14f)
            : new Font("Segoe UI Emoji", 14f);
        using var iconBrush = new SolidBrush(iconColor);
        var iconX = (IconAreaWidth - 24) / 2f;
        g.DrawString(icons[index], iconFont, iconBrush, iconX, y + 12);

        // 텍스트 (항상 표시)
        var textColor = isSelected ? ColorPalette.NavActive :
                        isHover ? ColorPalette.Text :
                        ColorPalette.TextSecondary;
        using var textFont = new Font("맑은 고딕", 10.5f, isSelected ? FontStyle.Bold : FontStyle.Regular);
        using var textBrush = new SolidBrush(textColor);
        g.DrawString(Labels[index], textFont, textBrush, IconAreaWidth + 4, y + 14);
    }

    /// <summary>Segoe MDL2 Assets 폰트 사용 가능 여부 확인</summary>
    private static bool IsMdl2Available()
    {
        try
        {
            using var font = new Font("Segoe MDL2 Assets", 12f);
            return font.Name == "Segoe MDL2 Assets";
        }
        catch { return false; }
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
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
