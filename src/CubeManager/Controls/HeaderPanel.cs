using System.Drawing;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>
/// 상단 헤더 바 (50px). 좌측 앱 이름 + 우측 지점명/시각.
/// </summary>
public class HeaderPanel : Panel
{
    private readonly Label _lblTime;

    public HeaderPanel()
    {
        Dock = DockStyle.Top;
        Height = 50;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(16, 0, 16, 0);

        // 하단 border 효과 (Paint에서 처리)
        DoubleBuffered = true;

        // 좌측: 앱 이름
        var lblApp = new Label
        {
            Text = "CubeManager",
            Font = new Font("맑은 고딕", 14f, FontStyle.Bold),
            ForeColor = ColorPalette.Primary,
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 14, 0, 0)
        };

        // 우측: 시각
        _lblTime = new Label
        {
            Font = new Font("맑은 고딕", 10f),
            ForeColor = ColorPalette.TextSecondary,
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 16, 0, 0)
        };
        UpdateTime();

        // 우측: 지점명
        var lblBranch = new Label
        {
            Text = "인천구월점",
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 16, 12, 0)
        };

        Controls.Add(_lblTime);
        Controls.Add(lblBranch);
        Controls.Add(lblApp);

        // 1초 타이머
        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (_, _) => UpdateTime();
        timer.Start();
    }

    private void UpdateTime()
    {
        _lblTime.Text = DateTime.Now.ToString("yyyy-MM-dd (ddd)  HH:mm:ss");
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // 하단 1px border
        using var pen = new Pen(ColorPalette.Border, 1);
        e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
    }
}
