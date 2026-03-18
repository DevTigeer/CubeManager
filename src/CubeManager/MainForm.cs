using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Forms;
using CubeManager.Helpers;
using Serilog;

namespace CubeManager;

public class MainForm : Form
{
    private readonly IServiceProvider _sp;
    private readonly TabControl _tabControl;
    private readonly Label _statusLabel;
    private readonly Dictionary<int, UserControl?> _tabCache = new();
    private readonly System.Windows.Forms.Timer _clockTimer;

    private static readonly string[] TabNames =
    [
        "예약/매출", "스케줄", "급여", "업무자료",
        "인수인계", "물품", "출퇴근", "설정"
    ];

    public MainForm(IServiceProvider serviceProvider)
    {
        _sp = serviceProvider;
        Text = "CubeManager v0.1.0";
        Size = new Size(1280, 768);
        MinimumSize = new Size(1024, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ColorPalette.Background;
        Font = new Font("맑은 고딕", 10f);

        // Tab Control
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 11f),
            Padding = new Point(12, 6)
        };

        foreach (var name in TabNames)
        {
            _tabControl.TabPages.Add(new TabPage(name) { BackColor = Color.White });
        }

        _tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

        // Status Bar
        var statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = ColorPalette.Primary,
            Padding = new Padding(10, 0, 10, 0)
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Right,
            ForeColor = Color.White,
            Font = new Font("맑은 고딕", 9f),
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Text = DateTime.Now.ToString("yyyy-MM-dd (ddd) HH:mm:ss")
        };

        var appLabel = new Label
        {
            Dock = DockStyle.Left,
            ForeColor = Color.White,
            Font = new Font("맑은 고딕", 9f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Text = "CubeManager"
        };

        statusPanel.Controls.Add(_statusLabel);
        statusPanel.Controls.Add(appLabel);

        Controls.Add(_tabControl);
        Controls.Add(statusPanel);

        // Clock Timer
        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (_, _) =>
            _statusLabel.Text = DateTime.Now.ToString("yyyy-MM-dd (ddd) HH:mm:ss");
        _clockTimer.Start();

        // 첫 번째 탭 로드
        LoadTab(0);
    }

    private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        LoadTab(_tabControl.SelectedIndex);
    }

    private void LoadTab(int index)
    {
        if (_tabCache.ContainsKey(index))
            return;

        Log.Information("탭 로드: {TabName}", TabNames[index]);

        var tab = CreateTab(index);
        _tabCache[index] = tab;
        _tabControl.TabPages[index].Controls.Add(tab);
    }

    private UserControl CreateTab(int index) => index switch
    {
        0 => new ReservationSalesTab(
                _sp.GetRequiredService<ISalesService>(),
                _sp.GetRequiredService<IReservationScraperService>()),
        1 => new ScheduleTab(
                _sp.GetRequiredService<IScheduleService>(),
                _sp.GetRequiredService<IEmployeeService>()),
        2 => new SalaryTab(_sp.GetRequiredService<ISalaryService>()),
        3 => new DocumentTab(),
        4 => new HandoverTab(_sp.GetRequiredService<IHandoverRepository>()),
        5 => new InventoryTab(_sp.GetRequiredService<IInventoryRepository>()),
        6 => new AttendanceTab(
                _sp.GetRequiredService<IAttendanceService>(),
                _sp.GetRequiredService<IEmployeeService>(),
                _sp.GetRequiredService<IScheduleService>()),
        7 => new SettingsTab(
                _sp.GetRequiredService<IEmployeeService>(),
                _sp.GetRequiredService<IReservationScraperService>(),
                _sp.GetRequiredService<IConfigRepository>()),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _clockTimer.Stop();
        _clockTimer.Dispose();

        // DB 종료 처리
        try
        {
            var db = _sp.GetService(typeof(CubeManager.Data.Database)) as Data.Database;
            db?.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DB 종료 처리 실패");
        }

        Log.Information("앱 종료");
        base.OnFormClosing(e);
    }
}
