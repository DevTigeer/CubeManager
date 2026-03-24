using System.Drawing;
using CubeManager.Dialogs;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>
/// 상단 헤더 바 (50px).
/// 좌측: 앱 이름 | 우측 끝: [─] [✕] (고정 위치)
/// 그 사이: 지점명, 아이콘, 시각 (Dock.Right)
/// </summary>
public class HeaderPanel : Panel
{
    private readonly Label _lblTime;
    private readonly Button _btnMinimize;
    private readonly Button _btnClose;
    private System.Windows.Forms.Timer? _timer;

    public event Action? RefreshRequested;

    public HeaderPanel()
    {
        Dock = DockStyle.Top;
        Height = 50;
        BackColor = ColorPalette.Card;
        Padding = new Padding(16, 0, 0, 0); // 우측 padding 없음 (버튼이 끝에 붙음)
        DoubleBuffered = true;

        // ═══ 좌측: 앱 이름 (Dock.Left) ═══
        var lblApp = new Label
        {
            Text = "CubeManager",
            Font = DesignTokens.FontPageTitle,
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 14, 0, 0)
        };

        // ═══ 우측 끝: 최소화 + 닫기 (Anchor 고정, Dock 미사용) ═══
        _btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(46, 50),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TabStop = false
        };
        _btnClose.FlatAppearance.BorderSize = 0;
        _btnClose.FlatAppearance.MouseOverBackColor = ColorPalette.Danger;
        _btnClose.Click += (_, _) => FindForm()?.Close();

        _btnMinimize = new Button
        {
            Text = "─",
            Font = new Font("Segoe UI", 11f),
            ForeColor = ColorPalette.TextSecondary,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(46, 50),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TabStop = false
        };
        _btnMinimize.FlatAppearance.BorderSize = 0;
        _btnMinimize.FlatAppearance.MouseOverBackColor = ColorPalette.HoverBg;
        _btnMinimize.Click += (_, _) =>
        {
            var form = FindForm();
            if (form != null) form.WindowState = FormWindowState.Minimized;
        };

        // ═══ 중간 영역 (Dock.Right): 시각, 아이콘, 지점명 ═══
        _lblTime = new Label
        {
            Font = DesignTokens.FontBody,
            ForeColor = ColorPalette.TextSecondary,
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 16, 100, 0) // 우측 100px = 버튼 2개 공간 확보
        };
        UpdateTime();

        var tip = new ToolTip();

        var btnRefresh = ButtonFactory.CreateIcon("🔄");
        btnRefresh.Dock = DockStyle.Right;
        btnRefresh.Margin = new Padding(0, 8, 4, 8);
        tip.SetToolTip(btnRefresh, "데이터 새로고침 (모든 탭 최신화)");
        btnRefresh.Click += (_, _) => RefreshRequested?.Invoke();

        var btnCalc = ButtonFactory.CreateIcon("🧮");
        btnCalc.Dock = DockStyle.Right;
        btnCalc.Margin = new Padding(0, 8, 0, 8);
        tip.SetToolTip(btnCalc, "간이 계산기");
        btnCalc.Click += (_, _) =>
        {
            using var dlg = new CalculatorDialog();
            dlg.ShowDialog(FindForm());
        };

        var lblBranch = new Label
        {
            Text = "인천구월점",
            Font = DesignTokens.FontBodyLarge,
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 16, 12, 0)
        };

        // ═══ Controls 추가 ═══
        // Dock 컨트롤
        Controls.Add(lblApp);
        Controls.Add(_lblTime);
        Controls.Add(btnCalc);
        Controls.Add(btnRefresh);
        Controls.Add(lblBranch);

        // Anchor 컨트롤 (Dock 위에 떠 있음)
        Controls.Add(_btnMinimize);
        Controls.Add(_btnClose);
        _btnClose.BringToFront();
        _btnMinimize.BringToFront();

        // 드래그로 창 이동
        var dragging = false;
        var dragStart = Point.Empty;
        MouseDown += (_, me) => { dragging = true; dragStart = me.Location; };
        MouseMove += (_, me) =>
        {
            if (!dragging) return;
            var form = FindForm();
            if (form == null) return;
            form.Location = new Point(
                form.Location.X + me.X - dragStart.X,
                form.Location.Y + me.Y - dragStart.Y);
        };
        MouseUp += (_, _) => dragging = false;

        // 타이머 (필드로 저장하여 dispose 가능)
        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) => UpdateTime();
        _timer.Start();

        // 초기 위치 + Resize 시 위치 갱신
        Resize += (_, _) => LayoutWindowButtons();
        HandleCreated += (_, _) => LayoutWindowButtons();
    }

    /// <summary>최소화/닫기 버튼 위치를 우측 끝에 고정</summary>
    private void LayoutWindowButtons()
    {
        var rightEdge = Width;
        _btnClose.Location = new Point(rightEdge - _btnClose.Width, 0);
        _btnMinimize.Location = new Point(rightEdge - _btnClose.Width - _btnMinimize.Width, 0);
    }

    private void UpdateTime()
    {
        _lblTime.Text = DateTime.Now.ToString("yyyy-MM-dd (ddd)  HH:mm:ss");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _timer?.Stop(); _timer?.Dispose(); }
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(ColorPalette.Border, 1);
        e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
    }
}
