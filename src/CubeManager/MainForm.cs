using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Controls;
using CubeManager.Forms;
using CubeManager.Helpers;
using Serilog;

namespace CubeManager;

public class MainForm : Form
{
    private readonly IServiceProvider _sp;
    private readonly SideNavPanel _sideNav;
    private readonly Panel _contentPanel;
    private readonly Dictionary<int, UserControl?> _tabCache = new();

    private static readonly string[] TabNames =
    [
        "예약/매출", "스케줄", "급여", "업무자료",
        "인수인계", "물품", "출퇴근", "설정"
    ];

    public MainForm(IServiceProvider serviceProvider)
    {
        _sp = serviceProvider;
        Text = "CubeManager v0.2.0";
        Size = new Size(1280, 768);
        MinimumSize = new Size(1024, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ColorPalette.Background;
        Font = new Font("맑은 고딕", 10f);

        // === 레이아웃 구조 ===
        // StatusBar (하단)
        var statusBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = ColorPalette.Primary900,
            Padding = new Padding(10, 0, 10, 0)
        };
        var statusLabel = new Label
        {
            Text = "CubeManager v0.2.0",
            ForeColor = Color.FromArgb(180, 200, 255),
            Font = new Font("맑은 고딕", 8.5f),
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        statusBar.Controls.Add(statusLabel);

        // HeaderPanel (상단)
        var header = new HeaderPanel();

        // SideNavPanel (좌측)
        _sideNav = new SideNavPanel();
        _sideNav.TabSelected += OnTabSelected;

        // ContentPanel (중앙)
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorPalette.Background,
            Padding = new Padding(0)
        };

        // 중요: Dock 순서 = 바깥부터 안으로
        Controls.Add(_contentPanel);   // Fill (마지막 추가 = 남은 공간)
        Controls.Add(_sideNav);        // Left
        Controls.Add(header);          // Top
        Controls.Add(statusBar);       // Bottom

        // 첫 번째 탭 로드
        LoadTab(0);
    }

    private void OnTabSelected(int index)
    {
        LoadTab(index);
    }

    private void LoadTab(int index)
    {
        if (!_tabCache.ContainsKey(index))
        {
            Log.Information("탭 로드: {TabName}", TabNames[index]);
            _tabCache[index] = CreateTab(index);
        }

        // ContentPanel 콘텐츠 교체
        _contentPanel.SuspendLayout();
        _contentPanel.Controls.Clear();
        var tab = _tabCache[index];
        if (tab != null)
        {
            tab.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(tab);
        }
        _contentPanel.ResumeLayout();

        _sideNav.SelectedIndex = index;
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
