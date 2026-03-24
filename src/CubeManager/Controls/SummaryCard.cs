using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>
/// 통계 카드 컴포넌트. 탭 상단에 4열로 배치하여 핵심 지표를 표시.
/// GDI+ OnPaint로 렌더링 (저사양 최적화, DoubleBuffered).
/// 2025 업데이트: 8px 둥근 모서리 + 하단 1px 미세 그림자.
/// </summary>
public class SummaryCard : Panel
{
    private string _title = "";
    private string _value = "0";
    private string _subText = "";
    private Color _accentColor = ColorPalette.Primary;
    private Color _accentLightColor = ColorPalette.Primary50;
    private bool _isHovered;

    public string Title { get => _title; set { _title = value; Invalidate(); } }
    public string Value { get => _value; set { _value = value; Invalidate(); } }
    public string SubText { get => _subText; set { _subText = value; Invalidate(); } }

    public void SetAccent(Color main, Color light)
    {
        _accentColor = main;
        _accentLightColor = light;
        Invalidate();
    }

    /// <summary>값과 단위를 분리: "1,234,000원" → ("1,234,000", "원")</summary>
    private static (string num, string unit) SplitValueUnit(string value)
    {
        if (string.IsNullOrEmpty(value)) return ("0", "");
        // 끝에서부터 한글/영문 단위를 분리
        var i = value.Length - 1;
        while (i >= 0 && !char.IsDigit(value[i]) && value[i] != ',' && value[i] != '.')
            i--;
        if (i < 0) return (value, "");
        return (value[..(i + 1)], value[(i + 1)..].TrimStart());
    }

    private const int Radius = 8;

    public SummaryCard()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        Height = 100;
        BackColor = Color.Transparent;
        Margin = new Padding(8);

        // 호버 효과
        MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // 카드 영역 (하단 1px 여유)
        var cardRect = new Rectangle(0, 0, Width - 1, Height - 2);
        using var path = RoundedCard.CreateRoundedPath(cardRect, Radius);

        // ── 뉴모피즘 그림자 ──
        // 어두운 그림자 (우하)
        var shadowRect = new Rectangle(cardRect.X + 2, cardRect.Y + 2, cardRect.Width, cardRect.Height);
        using var shadowPath = RoundedCard.CreateRoundedPath(shadowRect, Radius);
        using var darkPen = new Pen(ColorPalette.NeuDark, 1.5f);
        g.DrawPath(darkPen, shadowPath);

        // 카드 배경 (호버 시 미세 변화)
        var bgColor = _isHovered ? ColorPalette.CardHover : ColorPalette.Surface;
        using var bgBrush = new SolidBrush(bgColor);
        g.FillPath(bgBrush, path);

        // 미세 테두리
        using var borderPen = new Pen(ColorPalette.Border, 0.5f);
        g.DrawPath(borderPen, path);

        var pad = DesignTokens.SpaceLG;

        // 아이콘 원형 배경 (36x36)
        var iconRect = new Rectangle(pad, pad, 36, 36);
        using var iconBgBrush = new SolidBrush(_accentLightColor);
        g.FillEllipse(iconBgBrush, iconRect);

        // 아이콘 원형 안에 작은 원 (Accent 색상)
        var innerRect = new Rectangle(pad + 10, pad + 10, 16, 16);
        using var innerBrush = new SolidBrush(_accentColor);
        g.FillEllipse(innerBrush, innerRect);

        var textX = pad + 36 + DesignTokens.SpaceMD;

        // 라벨 (Title) — 캐싱된 폰트
        using var titleBrush = new SolidBrush(ColorPalette.TextSecondary);
        g.DrawString(_title, DesignTokens.FontBodySmall, titleBrush, textX, pad);

        // 메인 값 (Value)
        using var valueBrush = new SolidBrush(ColorPalette.Text);
        var (numPart, unitPart) = SplitValueUnit(_value);
        var numSize = g.MeasureString(numPart, DesignTokens.FontStatValue);
        g.DrawString(numPart, DesignTokens.FontStatValue, valueBrush, textX, pad + 16);

        // 단위
        if (!string.IsNullOrEmpty(unitPart))
        {
            using var unitBrush = new SolidBrush(ColorPalette.TextTertiary);
            g.DrawString(unitPart, DesignTokens.FontBodyLarge, unitBrush,
                textX + numSize.Width - 4, pad + 28);
        }

        // 서브텍스트
        if (!string.IsNullOrEmpty(_subText))
        {
            var subColor = _subText.StartsWith('▲') ? ColorPalette.Success :
                           _subText.StartsWith('▼') ? ColorPalette.Danger :
                           ColorPalette.TextTertiary;
            using var subBrush = new SolidBrush(subColor);
            g.DrawString(_subText, DesignTokens.FontCaption, subBrush, textX, pad + 50);
        }
    }
}
