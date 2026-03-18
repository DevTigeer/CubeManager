using System.Drawing;

namespace CubeManager.Helpers;

public enum ToastType { Success, Warning, Error }

public class ToastNotification : Form
{
    private static readonly List<ToastNotification> _activeToasts = [];
    private readonly System.Windows.Forms.Timer _timer;

    private ToastNotification(string message, ToastType type)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(320, 50);
        BackColor = type switch
        {
            ToastType.Success => ColorPalette.Success,
            ToastType.Warning => ColorPalette.Warning,
            ToastType.Error => ColorPalette.Danger,
            _ => ColorPalette.Info
        };

        var lbl = new Label
        {
            Text = message,
            ForeColor = type == ToastType.Warning ? ColorPalette.Text : Color.White,
            Font = new Font("맑은 고딕", 11f, FontStyle.Regular),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(10, 0, 10, 0)
        };
        Controls.Add(lbl);

        _timer = new System.Windows.Forms.Timer { Interval = 3000 };
        _timer.Tick += (_, _) => CloseToast();
        _timer.Start();
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
            screen.Right - toast.Width - 20,
            screen.Bottom - toast.Height - 20 - yOffset);
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
