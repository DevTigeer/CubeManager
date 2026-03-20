using System.Drawing;
using System.Drawing.Drawing2D;

namespace CubeManager.Helpers;

public enum ToastType { Success, Warning, Error, Info }

/// <summary>
/// 하단 우측 토스트 알림.
/// 2025 업데이트: 8px 둥근 모서리, 타입 아이콘, 미세 그림자.
/// </summary>
public class ToastNotification : Form
{
    private static readonly List<ToastNotification> _activeToasts = [];
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Color _barColor;
    private const int Radius = 8;

    private static readonly Dictionary<ToastType, string> TypeIcons = new()
    {
        [ToastType.Success] = "✓",
        [ToastType.Warning] = "⚠",
        [ToastType.Error] = "✕",
        [ToastType.Info] = "ℹ"
    };

    private ToastNotification(string message, ToastType type)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(340, 52);

        var (barColor, bgColor, fgColor) = type switch
        {
            ToastType.Success => (ColorPalette.Success, ColorPalette.SuccessLight, ColorPalette.Success),
            ToastType.Warning => (ColorPalette.Warning, ColorPalette.WarningLight, ColorPalette.Warning),
            ToastType.Error   => (ColorPalette.Danger, ColorPalette.DangerLight, ColorPalette.Danger),
            _ => (ColorPalette.Info, ColorPalette.InfoLight, ColorPalette.Info)
        };

        _barColor = barColor;
        BackColor = bgColor;

        var icon = TypeIcons[type];
        var lbl = new Label
        {
            Text = $" {icon}  {message}",
            ForeColor = fgColor,
            Font = new Font("맑은 고딕", 10.5f),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 10, 0)
        };
        Controls.Add(lbl);

        // 둥근 모서리
        Resize += (_, _) => ApplyRoundedRegion();
        HandleCreated += (_, _) => ApplyRoundedRegion();

        _timer = new System.Windows.Forms.Timer { Interval = 3000 };
        _timer.Tick += (_, _) => CloseToast();
        _timer.Start();
    }

    private void ApplyRoundedRegion()
    {
        if (Width <= 0 || Height <= 0) return;
        var path = new GraphicsPath();
        var d = Radius * 2;
        var rect = new Rectangle(0, 0, Width, Height);
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // 좌측 4px 컬러 바
        using var barBrush = new SolidBrush(_barColor);
        g.FillRectangle(barBrush, 0, 0, 4, Height);
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
        var yOffset = _activeToasts.Count * 62;
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
