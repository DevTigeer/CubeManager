using System.Drawing;
using System.Drawing.Drawing2D;

namespace CubeManager.Helpers;

public enum ToastType { Success, Warning, Error }

/// <summary>
/// 하단 우측 토스트 알림. 좌측 4px 컬러 바 + 라이트 배경 패턴.
/// 3초 후 자동 사라짐. 복수 토스트 스택.
/// </summary>
public class ToastNotification : Form
{
    private static readonly List<ToastNotification> _activeToasts = [];
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Color _barColor;

    private ToastNotification(string message, ToastType type)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(320, 50);

        // 타입별 색상 매핑 (좌측 바, 배경, 글자)
        var (barColor, bgColor, fgColor) = type switch
        {
            ToastType.Success => (ColorPalette.Success, ColorPalette.SuccessLight, Color.FromArgb(46, 125, 50)),
            ToastType.Warning => (ColorPalette.Warning, ColorPalette.WarningLight, Color.FromArgb(230, 81, 0)),
            ToastType.Error   => (ColorPalette.Danger, ColorPalette.DangerLight, Color.FromArgb(198, 40, 40)),
            _ => (ColorPalette.Info, ColorPalette.InfoLight, ColorPalette.Primary700)
        };

        _barColor = barColor;
        BackColor = bgColor;

        // 좌측 바 자리 확보 (Padding)
        var lbl = new Label
        {
            Text = message,
            ForeColor = fgColor,
            Font = new Font("맑은 고딕", 11f, FontStyle.Regular),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 10, 0)  // 좌측 16px (4px bar + 12px gap)
        };
        Controls.Add(lbl);

        // 3초 후 자동 닫기
        _timer = new System.Windows.Forms.Timer { Interval = 3000 };
        _timer.Tick += (_, _) => CloseToast();
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;

        // 좌측 4px 컬러 바
        using var barBrush = new SolidBrush(_barColor);
        g.FillRectangle(barBrush, 0, 0, 4, Height);

        // 1px 테두리 (subtle)
        using var borderPen = new Pen(ColorPalette.Border, 1);
        g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }

    public static void Show(string message, ToastType type)
    {
        if (Application.OpenForms.Count == 0) return;

        var toast = new ToastNotification(message, type);
        PositionToast(toast);
        _activeToasts.Add(toast);
        toast.Show();
    }

    private static void PositionToast(ToastNotification toast)
    {
        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        var yOffset = _activeToasts.Count * 60;
        toast.Location = new Point(
            screen.Right - toast.Width - 16,
            screen.Bottom - toast.Height - 16 - yOffset);
    }

    private void CloseToast()
    {
        _timer.Stop();
        _timer.Dispose();
        _activeToasts.Remove(this);
        Close();
        Dispose();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        CloseToast();
    }
}
