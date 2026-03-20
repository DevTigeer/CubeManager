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

        // 하단 미세 그림자
        using var shadowPen = new Pen(ColorPalette.ShadowLight);
        g.DrawLine(shadowPen,
            Radius, cardRect.Bottom + 1,
            cardRect.Right - Radius, cardRect.Bottom + 1);

        // 카드 배경 (호버 시 약간 어두운 배경)
        var bgColor = _isHovered ? ColorPalette.CardHover : ColorPalette.Surface;
        using var bgBrush = new SolidBrush(bgColor);
        g.FillPath(bgBrush, path);

        // 테두리
        using var borderPen = new Pen(ColorPalette.Border, 1);
        g.DrawPath(borderPen, path);

        var pad = 16;

        // 아이콘 원형 배경 (36x36)
        var iconRect = new Rectangle(pad, pad, 36, 36);
        using var iconBgBrush = new SolidBrush(_accentLightColor);
        g.FillEllipse(iconBgBrush, iconRect);

        // 아이콘 원형 안에 작은 원 (Accent 색상, 시각적 포인트)
        var innerRect = new Rectangle(pad + 10, pad + 10, 16, 16);
        using var innerBrush = new SolidBrush(_accentColor);
        g.FillEllipse(innerBrush, innerRect);

        var textX = pad + 36 + 12; // 아이콘 우측

        // 라벨 (Title)
        using var titleFont = new Font("맑은 고딕", 9f, FontStyle.Regular);
        using var titleBrush = new SolidBrush(ColorPalette.TextSecondary);
        g.DrawString(_title, titleFont, titleBrush, textX, pad);

        // 메인 값 (Value)
        using var valueFont = new Font("맑은 고딕", 18f, FontStyle.Bold);
        using var valueBrush = new SolidBrush(ColorPalette.Text);
        g.DrawString(_value, valueFont, valueBrush, textX, pad + 18);

        // 서브텍스트
        if (!string.IsNullOrEmpty(_subText))
        {
            using var subFont = new Font("맑은 고딕", 8.5f, FontStyle.Regular);
            var subColor = _subText.StartsWith('▲') ? ColorPalette.Success :
                           _subText.StartsWith('▼') ? ColorPalette.Danger :
                           ColorPalette.TextTertiary;
            using var subBrush = new SolidBrush(subColor);
            g.DrawString(_subText, subFont, subBrush, textX, pad + 48);
        }
    }
}
