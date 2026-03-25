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
    private readonly System.Windows.Forms.Timer _backupTimer;
    private readonly System.Windows.Forms.Timer _miceTimer;
    private readonly System.Windows.Forms.Timer _alertTimer;
    private Label? _alertBadge;

    private static readonly string[] TabNames =
    [
        "예약/매출", "스케줄", "체크리스트", "출퇴근",
        "인수인계", "무료이용권", "물품", "업무자료", "테마힌트", "설정", "관리자"
    ];

    public MainForm(IServiceProvider serviceProvider)
    {
        _sp = serviceProvider;
        Text = "CubeManager v0.2.0";
        MinimumSize = new Size(1024, 600);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;  // 기본 타이틀바 제거
        BackColor = ColorPalette.Surface;
        ForeColor = ColorPalette.Text;
        Font = DesignTokens.FontBody;

        // === 레이아웃 구조 ===
        // HeaderPanel (상단)
        var header = new HeaderPanel();
        header.RefreshRequested += OnRefreshRequested;

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

        // 자동 백업 타이머 (10분마다 체크, 월/금 17시에 실행)
        _backupTimer = new System.Windows.Forms.Timer { Interval = 600_000 }; // 10분
        _backupTimer.Tick += BackupTimer_Tick;
        _backupTimer.Enabled = true;

        // 미끼관리 타이머 (1분마다 체크)
        _miceTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _miceTimer.Tick += MiceTimer_Tick;
        _miceTimer.Enabled = true;

        // 알림 시스템 타이머 (5분마다 체크)
        _alertTimer = new System.Windows.Forms.Timer { Interval = 300_000 }; // 5분
        _alertTimer.Tick += AlertTimer_Tick;
        _alertTimer.Enabled = true;

        // 헤더에 알림 뱃지 추가
        _alertBadge = new Label
        {
            Text = "", Visible = false,
            Size = new Size(24, 18), AutoSize = false,
            Font = new Font("맑은 고딕", 8f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorPalette.Danger,
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(header.Width - 200, 16)
        };
        _alertBadge.Paint += (_, pe) =>
        {
            // 둥근 뱃지
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, _alertBadge.Width - 1, _alertBadge.Height - 1);
            _alertBadge.Region = new Region(path);
        };
        header.Controls.Add(_alertBadge);
        _ = UpdateAlertBadgeAsync();

        // 전역 모던 스타일 적용
        ControlFactory.EnableGlobalDialogStyling(this);

        // 첫 번째 탭 로드
        LoadTab(0);
    }

    private DateTime _lastBackupDate = DateTime.MinValue;

    private async void BackupTimer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;

        // 월요일 or 금요일, 17시대(17:00~17:09), 오늘 아직 안 했으면
        if (now.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Friday
            && now.Hour == 17 && now.Minute < 10
            && _lastBackupDate.Date != now.Date)
        {
            _lastBackupDate = now;
            try
            {
                var db = _sp.GetRequiredService<Data.Database>();
                var path = await db.BackupAsync();
                Log.Information("자동 백업 완료: {Path}", path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "자동 백업 실패");
            }
        }
    }

    private async void MiceTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var repo = _sp.GetRequiredService<IMicePopupRepository>();
            var popups = await repo.GetActiveAsync();
            var now = DateTime.Now;

            foreach (var popup in popups)
            {
                var lastShown = string.IsNullOrEmpty(popup.LastShownAt)
                    ? DateTime.MinValue
                    : DateTime.Parse(popup.LastShownAt);

                if ((now - lastShown).TotalMinutes >= popup.IntervalMinutes)
                {
                    _miceTimer.Enabled = false; // 팝업 중 타이머 일시 중단
                    using var dlg = new Dialogs.MicePopupDialog(popup.Title, popup.Content);
                    dlg.ShowDialog(this);
                    await repo.UpdateLastShownAsync(popup.Id, now.ToString("yyyy-MM-dd HH:mm:ss"));
                    _miceTimer.Enabled = true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "미끼 팝업 처리 실패");
        }
    }

    private async void AlertTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var alertService = _sp.GetRequiredService<IAlertService>();

            // 4가지 알림 검사 실행
            await alertService.CheckChecklistDelayAsync();
            await alertService.CheckHandoverUnreadAsync();
            await alertService.CheckNoShowAsync();
            await alertService.CheckLateAccumulateAsync();

            // 뱃지 업데이트
            await UpdateAlertBadgeAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "알림 시스템 체크 실패");
        }
    }

    private async Task UpdateAlertBadgeAsync()
    {
        try
        {
            var alertService = _sp.GetRequiredService<IAlertService>();
            var count = await alertService.GetUnresolvedCountAsync();

            if (_alertBadge != null)
            {
                _alertBadge.Visible = count > 0;
                _alertBadge.Text = count > 99 ? "99" : count.ToString();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "알림 뱃지 업데이트 실패");
        }
    }

    private void OnRefreshRequested()
    {
        // 캐시된 탭 모두 Dispose 후 클리어
        foreach (var tab in _tabCache.Values)
            tab?.Dispose();
        _tabCache.Clear();

        // 현재 탭 재생성
        LoadTab(_sideNav.SelectedIndex);
        Log.Information("전체 탭 새로고침 완료");

        Helpers.ToastNotification.Show("데이터를 최신 상태로 새로고침했습니다.", Helpers.ToastType.Success);
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
            if (_tabCache[index] != null)
                ControlFactory.ApplyModernStyle(_tabCache[index]!);
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
                _sp.GetRequiredService<IReservationScraperService>(),
                _sp.GetRequiredService<IReservationRepository>()),
        1 => new ScheduleTab(
                _sp.GetRequiredService<IScheduleService>(),
                _sp.GetRequiredService<IEmployeeService>(),
                _sp.GetRequiredService<IHolidayRepository>(),
                _sp.GetRequiredService<IWorkPartRepository>()),
        2 => new ChecklistTab(
                _sp.GetRequiredService<IChecklistRepository>(),
                _sp.GetRequiredService<IScheduleService>(),
                _sp.GetRequiredService<IEmployeeService>()),
        3 => new AttendanceTab(
                _sp.GetRequiredService<IAttendanceService>(),
                _sp.GetRequiredService<IEmployeeService>(),
                _sp.GetRequiredService<IScheduleService>()),
        4 => new HandoverTab(_sp.GetRequiredService<IHandoverRepository>()),
        5 => new FreePassTab(
                _sp.GetRequiredService<IFreePassRepository>()),
        6 => new InventoryTab(_sp.GetRequiredService<IInventoryRepository>()),
        7 => new DocumentTab(),
        8 => new ThemeHintTab(
                _sp.GetRequiredService<IThemeRepository>(),
                _sp.GetRequiredService<IThemeExportService>()),
        9 => new SettingsTab(
                _sp.GetRequiredService<IReservationScraperService>(),
                _sp.GetRequiredService<IConfigRepository>()),
        10 => new AdminTab(
                _sp.GetRequiredService<IConfigRepository>(),
                _sp.GetRequiredService<ISalesService>(),
                _sp.GetRequiredService<IAttendanceService>(),
                _sp.GetRequiredService<IEmployeeService>(),
                _sp.GetRequiredService<IMicePopupRepository>(),
                _sp.GetRequiredService<IChecklistRepository>(),
                _sp.GetRequiredService<Data.Database>(),
                _sp.GetRequiredService<ISalaryService>(),
                _sp.GetRequiredService<IAlertService>(),
                _sp.GetRequiredService<IWorkPartRepository>()),
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
