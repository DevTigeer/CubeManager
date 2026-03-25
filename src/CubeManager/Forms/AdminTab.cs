using System.Drawing;
using CubeManager.Controls;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Dialogs;
using CubeManager.Helpers;

namespace CubeManager.Forms;

/// <summary>
/// 관리자 탭 — 비밀번호 인증 후 접근. 내부 TabControl로 구성.
/// </summary>
public class AdminTab : UserControl
{
    private readonly IConfigRepository _configRepo;
    private readonly ISalesService _salesService;
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    private readonly IMicePopupRepository _miceRepo;
    private readonly IChecklistRepository _checklistRepo;
    private readonly Data.Database? _database;
    private readonly ISalaryService? _salaryService2;
    private readonly IAlertService? _alertService;

    private Panel _authPanel = null!;
    private Panel _mainPanel = null!;

    // ===== 대시보드 탭 =====
    private readonly DateTimePicker _dtpCashDate;
    private readonly NumericUpDown _numCashAmount;
    private readonly TextBox _txtCashNote;
    private readonly DateTimePicker _dtpStatMonth;
    private readonly DataGridView _gridAttendStats;
    private readonly DataGridView _gridSalesStats;
    private readonly SummaryCardRow _summaryCards;

    // ===== 직원 관리 탭 =====
    private readonly DataGridView _gridEmployees;

    // ===== 알람 탭 =====
    private readonly DataGridView _gridMice;

    // ===== 체크리스트 관리 탭 =====
    private readonly DataGridView _gridChecklist;

    // ===== 파트 관리 탭 =====
    private readonly IWorkPartRepository _workPartRepo;
    private readonly DataGridView _gridParts;

    // ===== 출퇴근 이력 탭 =====
    private readonly DataGridView _gridAttendHistory;
    private readonly DateTimePicker _dtpAttendStart;
    private readonly DateTimePicker _dtpAttendEnd;

    // 역할 표시 이름 매핑
    private static readonly Dictionary<string, string> RoleNames = new()
    {
        ["open"] = "오픈",
        ["close"] = "마감",
        ["middle1"] = "1미들",
        ["middle2"] = "2미들",
        ["all"] = "전체"
    };

    public AdminTab(IConfigRepository configRepo, ISalesService salesService,
        IAttendanceService attendanceService, IEmployeeService employeeService,
        IMicePopupRepository miceRepo, IChecklistRepository checklistRepo,
        Data.Database database, ISalaryService salaryService, IAlertService alertService,
        IWorkPartRepository workPartRepo)
    {
        _configRepo = configRepo;
        _salesService = salesService;
        _attendanceService = attendanceService;
        _employeeService = employeeService;
        _miceRepo = miceRepo;
        _checklistRepo = checklistRepo;
        _database = database;
        _salaryService2 = salaryService;
        _alertService = alertService;
        _workPartRepo = workPartRepo;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;

        // 컨트롤 초기화
        _dtpCashDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Size = new Size(130, 25) };
        _numCashAmount = new NumericUpDown { Minimum = -10_000_000, Maximum = 100_000_000, Increment = 1000, ThousandsSeparator = true, Size = new Size(150, 25) };
        _txtCashNote = new TextBox { Size = new Size(200, 25), PlaceholderText = "보정 사유 (예: 잔돈 오차)" };
        _dtpStatMonth = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy년 MM월", ShowUpDown = true, Size = new Size(150, 25), Value = DateTime.Today };
        _gridAttendStats = new DataGridView { Dock = DockStyle.Fill };
        _gridSalesStats = new DataGridView { Dock = DockStyle.Fill };
        _gridEmployees = new DataGridView { Dock = DockStyle.Fill };
        _gridMice = new DataGridView { Dock = DockStyle.Fill };
        _gridChecklist = new DataGridView { Dock = DockStyle.Fill };
        _gridParts = new DataGridView { Dock = DockStyle.Fill };
        _gridAttendHistory = new DataGridView { Dock = DockStyle.Fill };
        _dtpAttendStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Size = new Size(130, 25) };
        _dtpAttendEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Size = new Size(130, 25) };
        _summaryCards = new SummaryCardRow();

        BuildAuthPanel();
        BuildMainPanel();

        Controls.Add(_mainPanel);
        Controls.Add(_authPanel);

        _mainPanel.Visible = false;
        _authPanel.Visible = true;

        // 탭 떠날 때 인증 초기화 (재진입 시 비밀번호 다시 입력)
        VisibleChanged += (_, _) =>
        {
            if (!Visible)
            {
                _mainPanel.Visible = false;
                _authPanel.Visible = true;
            }
        };
    }

    // ========== 인증 화면 ==========
    private void BuildAuthPanel()
    {
        _authPanel = new Panel { Dock = DockStyle.Fill, BackColor = ColorPalette.Background };

        var centerPanel = new Panel
        {
            Size = new Size(350, 200),
            BackColor = ColorPalette.Surface,
            Anchor = AnchorStyles.None
        };

        var lblTitle = new Label
        {
            Text = "🔒 관리자 인증",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top, Height = 50
        };

        var lblDesc = new Label
        {
            Text = "이 탭은 관리자 인증이 필요합니다.",
            Font = new Font("맑은 고딕", 10f),
            ForeColor = ColorPalette.TextSecondary,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top, Height = 30
        };

        var btnAuth = ButtonFactory.CreatePrimary("비밀번호 입력", 150);
        btnAuth.Height = 40;
        btnAuth.Font = new Font("맑은 고딕", 11f);
        btnAuth.Anchor = AnchorStyles.None;
        btnAuth.Click += (_, _) =>
        {
            if (AdminAuthDialog.Authenticate(_configRepo, this))
            {
                _authPanel.Visible = false;
                _mainPanel.Visible = true;
                _ = LoadAllStatsAsync();
            }
        };

        centerPanel.Controls.Add(btnAuth);
        centerPanel.Controls.Add(lblDesc);
        centerPanel.Controls.Add(lblTitle);

        _authPanel.Resize += (_, _) =>
        {
            centerPanel.Location = new Point(
                (_authPanel.Width - centerPanel.Width) / 2,
                (_authPanel.Height - centerPanel.Height) / 2);
            btnAuth.Location = new Point(
                (centerPanel.Width - btnAuth.Width) / 2, 120);
        };

        _authPanel.Controls.Add(centerPanel);
    }

    // ========== 메인 화면 (TabControl) ==========
    private void BuildMainPanel()
    {
        _mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

        var header = new Label
        {
            Text = "관리자 대시보드",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Top, Height = 40
        };

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = DesignTokens.FontBody,
            Padding = new Point(12, 4),
            Appearance = TabAppearance.FlatButtons  // 탭 테두리 제거
        };

        tabControl.TabPages.Add(BuildDashboardTab());
        tabControl.TabPages.Add(BuildSalaryTab());
        tabControl.TabPages.Add(BuildAttendanceHistoryTab());
        tabControl.TabPages.Add(BuildEmployeeTab());
        tabControl.TabPages.Add(BuildMiceTab());
        tabControl.TabPages.Add(BuildChecklistTab());
        tabControl.TabPages.Add(BuildWorkPartTab());
        tabControl.TabPages.Add(BuildAlertHistoryTab());

        _mainPanel.Controls.Add(tabControl);
        _mainPanel.Controls.Add(header);
    }

    // ==================== 탭 1: 대시보드 ====================
    private TabPage BuildDashboardTab()
    {
        var page = new TabPage("대시보드") { Padding = new Padding(10), BackColor = ColorPalette.Surface };

        _summaryCards.AddCard("이번 달 매출", "₩0", ColorPalette.AccentGreen.Main, ColorPalette.AccentGreen.Light);
        _summaryCards.AddCard("이번 달 지출", "₩0", ColorPalette.AccentRed.Main, ColorPalette.AccentRed.Light);
        _summaryCards.AddCard("지각 건수", "0건", ColorPalette.AccentOrange.Main, ColorPalette.AccentOrange.Light);
        _summaryCards.AddCard("현금 잔액", "₩0", ColorPalette.AccentBlue.Main, ColorPalette.AccentBlue.Light);

        // DB 백업 + 유틸
        var utilPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 0)
        };

        var btnBackup = ButtonFactory.CreateSuccess("💾 DB 백업", 120);
        btnBackup.Click += BtnBackup_Click;
        utilPanel.Controls.Add(btnBackup);

        var btnDbReset = ButtonFactory.CreateDanger("⚠ DB 초기화", 120);
        btnDbReset.Click += BtnDbReset_Click;
        utilPanel.Controls.Add(btnDbReset);

        var btnChangePw = ButtonFactory.CreateSecondary("비밀번호 변경", 120);
        btnChangePw.Click += BtnChangePw_Click;
        utilPanel.Controls.Add(btnChangePw);

        utilPanel.Controls.Add(new Label
        {
            Text = "자동 백업: 매주 월/금 오후 5시",
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextTertiary,
            Size = new Size(250, 32),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(10, 0, 0, 0)
        });

        // 현금 보정 (GroupBox 제거 → 섹션 제목 + Panel)
        var cashPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 75,
            BackColor = ColorPalette.Surface,
            Padding = new Padding(0, 0, 0, 8)
        };

        var cashTitle = new Label
        {
            Text = "현금 잔액 수기 보정",
            Dock = DockStyle.Top, Height = 28,
            Font = DesignTokens.FontSectionTitle,
            ForeColor = ColorPalette.TextSecondary,
            Padding = new Padding(0, 4, 0, 0)
        };

        var nf = DesignTokens.FontBody;
        var cashFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 0)
        };

        cashFlow.Controls.Add(new Label { Text = "날짜:", Size = new Size(40, 25), Font = nf, TextAlign = ContentAlignment.MiddleRight });
        _dtpCashDate.Font = nf;
        cashFlow.Controls.Add(_dtpCashDate);
        cashFlow.Controls.Add(new Label { Text = "보정 금액:", Size = new Size(75, 25), Font = nf, TextAlign = ContentAlignment.MiddleRight, Margin = new Padding(10, 0, 0, 0) });
        _numCashAmount.Font = nf;
        cashFlow.Controls.Add(_numCashAmount);
        _txtCashNote.Font = nf;
        _txtCashNote.Margin = new Padding(10, 0, 0, 0);
        cashFlow.Controls.Add(_txtCashNote);

        var btnApplyCash = ButtonFactory.CreatePrimary("보정 적용", 90);
        btnApplyCash.Height = 28;
        btnApplyCash.Font = nf;
        btnApplyCash.Margin = new Padding(10, 0, 0, 0);
        btnApplyCash.Click += BtnApplyCash_Click;
        cashFlow.Controls.Add(btnApplyCash);
        cashPanel.Controls.Add(cashFlow);
        cashPanel.Controls.Add(cashTitle);

        // 하단 분할: 지각/조퇴(좌) + 매출 통계(우)
        var bottomSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 500,
            SplitterWidth = 2,
            BackColor = ColorPalette.Divider
        };
        bottomSplit.Panel1.BackColor = ColorPalette.Surface;
        bottomSplit.Panel2.BackColor = ColorPalette.Surface;

        // 좌: 지각/조퇴 통계
        var attendPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 8, 0) };
        var attendHeader = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 35,
            FlowDirection = FlowDirection.LeftToRight
        };
        attendHeader.Controls.Add(new Label
        {
            Text = "월별 지각/조퇴 현황",
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary,
            Size = new Size(180, 30), TextAlign = ContentAlignment.MiddleLeft
        });
        _dtpStatMonth.Font = nf;
        _dtpStatMonth.ValueChanged += async (_, _) => await LoadAttendanceStatsAsync();
        attendHeader.Controls.Add(_dtpStatMonth);

        GridTheme.ApplyTheme(_gridAttendStats);
        _gridAttendStats.AllowUserToAddRows = false;
        _gridAttendStats.ReadOnly = true;
        _gridAttendStats.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "EmpName", HeaderText = "이름", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "LateCount", HeaderText = "지각", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "EarlyCount", HeaderText = "조퇴", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "OnTimeCount", HeaderText = "정상", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "TotalDays", HeaderText = "총 근무일", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "LateRate", HeaderText = "지각률", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } }
        );
        attendPanel.Controls.Add(_gridAttendStats);
        attendPanel.Controls.Add(attendHeader);

        // 우: 매출 통계
        var salesPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 0, 0) };
        var salesHeader = new Label
        {
            Text = "월별 매출 요약",
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary,
            Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleLeft
        };

        GridTheme.ApplyTheme(_gridSalesStats);
        _gridSalesStats.AllowUserToAddRows = false;
        _gridSalesStats.ReadOnly = true;
        _gridSalesStats.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "SaleDate", HeaderText = "날짜", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "CardAmt", HeaderText = "카드", FillWeight = 16, DefaultCellStyle = GridTheme.AmountStyle },
            new DataGridViewTextBoxColumn { Name = "CashAmt", HeaderText = "현금", FillWeight = 16, DefaultCellStyle = GridTheme.AmountStyle },
            new DataGridViewTextBoxColumn { Name = "TransferAmt", HeaderText = "계좌", FillWeight = 16, DefaultCellStyle = GridTheme.AmountStyle },
            new DataGridViewTextBoxColumn { Name = "TotalAmt", HeaderText = "총매출", FillWeight = 18, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("맑은 고딕", 10f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "CashBal", HeaderText = "현금잔액", FillWeight = 16, DefaultCellStyle = GridTheme.AmountStyle }
        );
        salesPanel.Controls.Add(_gridSalesStats);
        salesPanel.Controls.Add(salesHeader);

        bottomSplit.Panel1.Controls.Add(attendPanel);
        bottomSplit.Panel2.Controls.Add(salesPanel);

        // 역순 조립
        page.Controls.Add(bottomSplit);
        page.Controls.Add(cashPanel);
        page.Controls.Add(utilPanel);
        page.Controls.Add(_summaryCards);

        return page;
    }

    // ==================== 탭 2: 급여 관리 ====================
    private TabPage BuildSalaryTab()
    {
        var page = new TabPage("급여 관리") { BackColor = ColorPalette.Surface };

        // SalaryTab을 UserControl로 내장
        if (_salaryService2 != null)
        {
            var salaryControl = new SalaryTab(_salaryService2)
            {
                Dock = DockStyle.Fill
            };
            page.Controls.Add(salaryControl);
        }
        else
        {
            page.Controls.Add(new Label
            {
                Text = "급여 서비스를 불러올 수 없습니다.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ColorPalette.TextSecondary
            });
        }

        return page;
    }

    // ==================== 탭 3: 출퇴근 이력 ====================
    private TabPage BuildAttendanceHistoryTab()
    {
        var page = new TabPage("출퇴근 이력") { Padding = new Padding(10), BackColor = ColorPalette.Surface };

        // 상단: 기간 선택
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 45,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 5)
        };

        filterPanel.Controls.Add(new Label
        {
            Text = "조회 기간:",
            Font = new Font("맑은 고딕", 10f),
            ForeColor = ColorPalette.Text,
            Size = new Size(75, 28),
            TextAlign = ContentAlignment.MiddleLeft
        });
        filterPanel.Controls.Add(_dtpAttendStart);
        filterPanel.Controls.Add(new Label
        {
            Text = "~",
            Font = new Font("맑은 고딕", 10f),
            Size = new Size(20, 28),
            TextAlign = ContentAlignment.MiddleCenter
        });
        filterPanel.Controls.Add(_dtpAttendEnd);

        var btnSearch = ButtonFactory.CreatePrimary("조회", 80);
        btnSearch.Click += async (_, _) => await LoadAttendanceHistoryAsync();
        filterPanel.Controls.Add(btnSearch);

        // 빠른 기간 버튼
        var btnWeek = ButtonFactory.CreateGhost("1주", 55);
        btnWeek.Click += (_, _) => { _dtpAttendStart.Value = DateTime.Today.AddDays(-7); _dtpAttendEnd.Value = DateTime.Today; _ = LoadAttendanceHistoryAsync(); };
        filterPanel.Controls.Add(btnWeek);

        var btnMonth = ButtonFactory.CreateGhost("1개월", 65);
        btnMonth.Click += (_, _) => { _dtpAttendStart.Value = DateTime.Today.AddMonths(-1); _dtpAttendEnd.Value = DateTime.Today; _ = LoadAttendanceHistoryAsync(); };
        filterPanel.Controls.Add(btnMonth);

        var btn3Month = ButtonFactory.CreateGhost("3개월", 65);
        btn3Month.Click += (_, _) => { _dtpAttendStart.Value = DateTime.Today.AddMonths(-3); _dtpAttendEnd.Value = DateTime.Today; _ = LoadAttendanceHistoryAsync(); };
        filterPanel.Controls.Add(btn3Month);

        // 그리드 설정
        GridTheme.ApplyTheme(_gridAttendHistory);
        _gridAttendHistory.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "날짜", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "직원", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "ScheduledStart", HeaderText = "예정출근", Width = 85 },
            new DataGridViewTextBoxColumn { Name = "ScheduledEnd", HeaderText = "예정퇴근", Width = 85 },
            new DataGridViewTextBoxColumn { Name = "ClockIn", HeaderText = "실제출근", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "ClockOut", HeaderText = "실제퇴근", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "InStatus", HeaderText = "출근상태", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "OutStatus", HeaderText = "퇴근상태", Width = 80 }
        );

        page.Controls.Add(_gridAttendHistory);
        page.Controls.Add(filterPanel);

        return page;
    }

    private async Task LoadAttendanceHistoryAsync()
    {
        try
        {
            var start = _dtpAttendStart.Value.ToString("yyyy-MM-dd");
            var end = _dtpAttendEnd.Value.ToString("yyyy-MM-dd");
            var records = await _attendanceService.GetByDateRangeAsync(start, end);

            _gridAttendHistory.Rows.Clear();
            foreach (var r in records.OrderByDescending(x => x.WorkDate).ThenBy(x => x.EmployeeName))
            {
                var inTime = string.IsNullOrEmpty(r.ClockIn) ? "-" : r.ClockIn[11..16]; // HH:mm
                var outTime = string.IsNullOrEmpty(r.ClockOut) ? "-" : r.ClockOut[11..16];
                var inStatus = r.ClockInStatus switch { "late" => "🔴 지각", "on_time" => "✅ 정상", _ => "-" };
                var outStatus = r.ClockOutStatus switch { "early" => "🟡 조퇴", "on_time" => "✅ 정상", _ => "-" };

                _gridAttendHistory.Rows.Add(
                    r.WorkDate,
                    r.EmployeeName ?? $"ID:{r.EmployeeId}",
                    r.ScheduledStart ?? "-",
                    r.ScheduledEnd ?? "-",
                    inTime,
                    outTime,
                    inStatus,
                    outStatus
                );

                // 지각/조퇴 행 강조
                if (r.ClockInStatus == "late" || r.ClockOutStatus == "early")
                {
                    var row = _gridAttendHistory.Rows[_gridAttendHistory.Rows.Count - 1];
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 253, 231); // 연한 노랑
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "출퇴근 이력 조회 실패");
        }
    }

    // ==================== 탭 4: 직원 관리 ====================
    private TabPage BuildEmployeeTab()
    {
        var page = new TabPage("직원 관리") { Padding = new Padding(10), BackColor = ColorPalette.Surface };

        var nf = new Font("맑은 고딕", 10f, FontStyle.Regular);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 2)
        };

        var btnAddEmp = ButtonFactory.CreatePrimary("+ 직원 추가", 110);
        btnAddEmp.Height = 32;
        btnAddEmp.Font = nf;
        btnAddEmp.Click += BtnAddEmployee_Click;
        toolbar.Controls.Add(btnAddEmp);

        var btnDeactivateEmp = ButtonFactory.CreateDanger("비활성화", 90);
        btnDeactivateEmp.Height = 32;
        btnDeactivateEmp.Font = nf;
        btnDeactivateEmp.Margin = new Padding(10, 0, 0, 0);
        btnDeactivateEmp.Click += BtnDeactivateEmployee_Click;
        toolbar.Controls.Add(btnDeactivateEmp);

        GridTheme.ApplyTheme(_gridEmployees);
        _gridEmployees.AllowUserToAddRows = false;
        _gridEmployees.ReadOnly = true;
        _gridEmployees.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "EmpId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "EmpName", HeaderText = "이름", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "EmpWage", HeaderText = "시급", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "EmpPhone", HeaderText = "전화번호", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "EmpStatus", HeaderText = "상태", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } }
        );
        _gridEmployees.CellDoubleClick += GridEmployees_CellDoubleClick;

        page.Controls.Add(_gridEmployees);
        page.Controls.Add(toolbar);
        return page;
    }

    // ==================== 탭 3: 알람 관리 ====================
    private TabPage BuildMiceTab()
    {
        var page = new TabPage("알람") { Padding = new Padding(10), BackColor = ColorPalette.Surface };

        var nf = new Font("맑은 고딕", 10f, FontStyle.Regular);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 2)
        };

        var btnAddMice = ButtonFactory.CreatePrimary("+ 등록", 80);
        btnAddMice.Height = 32;
        btnAddMice.Font = nf;
        btnAddMice.Click += BtnAddMice_Click;
        toolbar.Controls.Add(btnAddMice);

        toolbar.Controls.Add(new Label
        {
            Text = "Delete 키로 선택 항목 삭제  |  활성 체크박스 토글",
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextTertiary,
            Size = new Size(350, 32),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(15, 0, 0, 0)
        });

        GridTheme.ApplyTheme(_gridMice);
        _gridMice.AllowUserToAddRows = false;
        _gridMice.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "MiceId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "MiceTitle", HeaderText = "제목", FillWeight = 25, ReadOnly = true },
            new DataGridViewTextBoxColumn { Name = "MiceContent", HeaderText = "내용", FillWeight = 30, ReadOnly = true },
            new DataGridViewTextBoxColumn { Name = "MiceInterval", HeaderText = "간격(분)", FillWeight = 10, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewCheckBoxColumn { Name = "MiceActive", HeaderText = "활성", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "MiceLastShown", HeaderText = "마지막표시", FillWeight = 25, ReadOnly = true }
        );

        _gridMice.CellContentClick += async (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_gridMice.Columns[e.ColumnIndex].Name != "MiceActive") return;

            var id = (int)_gridMice.Rows[e.RowIndex].Cells["MiceId"].Value;
            var currentActive = (bool)_gridMice.Rows[e.RowIndex].Cells["MiceActive"].Value;
            try
            {
                var all = (await _miceRepo.GetAllAsync()).ToList();
                var popup = all.FirstOrDefault(m => m.Id == id);
                if (popup != null)
                {
                    popup.IsActive = !currentActive;
                    await _miceRepo.UpdateAsync(popup);
                    await LoadMiceAsync();
                }
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
        };

        _gridMice.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Delete || _gridMice.CurrentRow == null) return;
            var id = (int)_gridMice.CurrentRow.Cells["MiceId"].Value;
            var title = _gridMice.CurrentRow.Cells["MiceTitle"].Value?.ToString();
            if (MessageBox.Show($"'{title}' 항목을 삭제하시겠습니까?", "확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                await _miceRepo.DeleteAsync(id);
                ToastNotification.Show("삭제 완료.", ToastType.Success);
                await LoadMiceAsync();
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
        };

        // ─── 하단: HR 자동 알림 설정 ───
        var alertSettingsPanel = new Panel
        {
            Dock = DockStyle.Bottom, Height = 200,
            BackColor = ColorPalette.Surface,
            Padding = new Padding(12, 8, 12, 8)
        };

        // 섹션 제목
        alertSettingsPanel.Controls.Add(new Label
        {
            Text = "⚙️ HR 자동 알림 설정",
            Dock = DockStyle.Top, Height = 28,
            Font = DesignTokens.FontSectionTitle,
            ForeColor = ColorPalette.TextSecondary
        });

        var settingsFont = DesignTokens.FontBodySmall;
        var sy = 22;

        // 1) 체크리스트 미완료 알림
        var chkChecklist = new CheckBox
        {
            Text = "체크리스트 미완료 알림",
            Location = new Point(15, sy), Size = new Size(200, 24),
            Font = settingsFont, Checked = true
        };
        alertSettingsPanel.Controls.Add(chkChecklist);
        alertSettingsPanel.Controls.Add(new Label
        {
            Text = "출근 후", Location = new Point(220, sy + 2), Size = new Size(45, 20),
            Font = settingsFont, ForeColor = ColorPalette.TextSecondary
        });
        var numChecklistMin = new NumericUpDown
        {
            Location = new Point(268, sy - 1), Size = new Size(60, 24),
            Minimum = 10, Maximum = 180, Value = 60, Increment = 10,
            Font = settingsFont
        };
        alertSettingsPanel.Controls.Add(numChecklistMin);
        alertSettingsPanel.Controls.Add(new Label
        {
            Text = "분 이내 50% 미만 시 알림", Location = new Point(332, sy + 2), Size = new Size(200, 20),
            Font = settingsFont, ForeColor = ColorPalette.TextSecondary
        });

        sy += 32;

        // 2) 인수인계 미확인 알림
        var chkHandover = new CheckBox
        {
            Text = "인수인계 미확인 알림",
            Location = new Point(15, sy), Size = new Size(200, 24),
            Font = settingsFont, Checked = true
        };
        alertSettingsPanel.Controls.Add(chkHandover);
        alertSettingsPanel.Controls.Add(new Label
        {
            Text = "출근 후", Location = new Point(220, sy + 2), Size = new Size(45, 20),
            Font = settingsFont, ForeColor = ColorPalette.TextSecondary
        });
        var numHandoverMin = new NumericUpDown
        {
            Location = new Point(268, sy - 1), Size = new Size(60, 24),
            Minimum = 10, Maximum = 120, Value = 30, Increment = 10,
            Font = settingsFont
        };
        alertSettingsPanel.Controls.Add(numHandoverMin);
        alertSettingsPanel.Controls.Add(new Label
        {
            Text = "분 이내 미확인 시 알림", Location = new Point(332, sy + 2), Size = new Size(200, 20),
            Font = settingsFont, ForeColor = ColorPalette.TextSecondary
        });

        sy += 32;

        // 3) 무단결근 자동 감지
        var chkNoShow = new CheckBox
        {
            Text = "무단결근 자동 감지",
            Location = new Point(15, sy), Size = new Size(200, 24),
            Font = settingsFont, Checked = true
        };
        alertSettingsPanel.Controls.Add(chkNoShow);
        alertSettingsPanel.Controls.Add(new Label
        {
            Text = "매일 12:00 이후 스케줄 대비 출근 미기록 시 알림",
            Location = new Point(220, sy + 2), Size = new Size(350, 20),
            Font = settingsFont, ForeColor = ColorPalette.TextSecondary
        });

        sy += 32;

        // 4) 지각 누적 경고
        var chkLateAccum = new CheckBox
        {
            Text = "지각 누적 경고",
            Location = new Point(15, sy), Size = new Size(200, 24),
            Font = settingsFont, Checked = true
        };
        alertSettingsPanel.Controls.Add(chkLateAccum);
        alertSettingsPanel.Controls.Add(new Label
        {
            Text = "월간 지각", Location = new Point(220, sy + 2), Size = new Size(55, 20),
            Font = settingsFont, ForeColor = ColorPalette.TextSecondary
        });
        var numLateThreshold = new NumericUpDown
        {
            Location = new Point(278, sy - 1), Size = new Size(50, 24),
            Minimum = 1, Maximum = 10, Value = 3,
            Font = settingsFont
        };
        alertSettingsPanel.Controls.Add(numLateThreshold);
        alertSettingsPanel.Controls.Add(new Label
        {
            Text = "회 이상 시 경고", Location = new Point(332, sy + 2), Size = new Size(150, 20),
            Font = settingsFont, ForeColor = ColorPalette.TextSecondary
        });

        sy += 36;

        // 저장 버튼
        var btnSaveAlertSettings = ButtonFactory.CreatePrimary("설정 저장", 90);
        btnSaveAlertSettings.Location = new Point(15, sy);
        btnSaveAlertSettings.Height = 30;
        btnSaveAlertSettings.Click += async (_, _) =>
        {
            try
            {
                await _configRepo.SetAsync("alert_checklist_enabled", chkChecklist.Checked ? "1" : "0");
                await _configRepo.SetAsync("alert_checklist_minutes", numChecklistMin.Value.ToString());
                await _configRepo.SetAsync("alert_handover_enabled", chkHandover.Checked ? "1" : "0");
                await _configRepo.SetAsync("alert_handover_minutes", numHandoverMin.Value.ToString());
                await _configRepo.SetAsync("alert_noshow_enabled", chkNoShow.Checked ? "1" : "0");
                await _configRepo.SetAsync("alert_late_enabled", chkLateAccum.Checked ? "1" : "0");
                await _configRepo.SetAsync("alert_late_threshold", numLateThreshold.Value.ToString());
                ToastNotification.Show("알림 설정이 저장되었습니다.", ToastType.Success);
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
        };
        alertSettingsPanel.Controls.Add(btnSaveAlertSettings);

        // 설정 로드
        page.VisibleChanged += async (_, _) =>
        {
            if (!page.Visible) return;
            try
            {
                chkChecklist.Checked = (await _configRepo.GetAsync("alert_checklist_enabled")) != "0";
                var clMin = await _configRepo.GetAsync("alert_checklist_minutes");
                if (int.TryParse(clMin, out var clv)) numChecklistMin.Value = Math.Clamp(clv, 10, 180);

                chkHandover.Checked = (await _configRepo.GetAsync("alert_handover_enabled")) != "0";
                var hoMin = await _configRepo.GetAsync("alert_handover_minutes");
                if (int.TryParse(hoMin, out var hov)) numHandoverMin.Value = Math.Clamp(hov, 10, 120);

                chkNoShow.Checked = (await _configRepo.GetAsync("alert_noshow_enabled")) != "0";
                chkLateAccum.Checked = (await _configRepo.GetAsync("alert_late_enabled")) != "0";
                var ltThr = await _configRepo.GetAsync("alert_late_threshold");
                if (int.TryParse(ltThr, out var ltv)) numLateThreshold.Value = Math.Clamp(ltv, 1, 10);
            }
            catch { /* 첫 실행 시 기본값 사용 */ }
        };

        page.Controls.Add(_gridMice);
        page.Controls.Add(alertSettingsPanel);
        page.Controls.Add(toolbar);
        return page;
    }

    // ==================== 탭 4: 체크리스트 관리 ====================
    private TabPage BuildChecklistTab()
    {
        var page = new TabPage("체크리스트") { Padding = new Padding(10), BackColor = ColorPalette.Surface };

        var nf = new Font("맑은 고딕", 10f, FontStyle.Regular);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 2)
        };

        // 요일 체크박스들
        var dayNames = new[] { "월", "화", "수", "목", "금", "토", "일" };
        var dayCheckBoxes = new CheckBox[7];
        for (var i = 0; i < 7; i++)
        {
            dayCheckBoxes[i] = new CheckBox
            {
                Text = dayNames[i],
                Font = nf,
                Size = new Size(50, 28),
                Checked = i == 0, // 월요일 기본 선택
                Tag = i
            };
            dayCheckBoxes[i].CheckedChanged += async (_, _) => await LoadChecklistTemplatesAsync();
            toolbar.Controls.Add(dayCheckBoxes[i]);
        }

        // 역할 선택
        var cmbRole = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(90, 28), Font = nf,
            Margin = new Padding(15, 0, 0, 0)
        };
        cmbRole.Items.AddRange(new object[] { "전체", "오픈", "마감", "1미들", "2미들" });
        cmbRole.SelectedIndex = 0;
        cmbRole.Tag = "roleCombo";
        toolbar.Controls.Add(cmbRole);

        var btnAdd = ButtonFactory.CreatePrimary("+ 추가", 80);
        btnAdd.Height = 32;
        btnAdd.Font = nf;
        btnAdd.Margin = new Padding(15, 0, 0, 0);
        btnAdd.Click += BtnAddChecklist_Click;
        toolbar.Controls.Add(btnAdd);

        GridTheme.ApplyTheme(_gridChecklist);
        _gridChecklist.AllowUserToAddRows = false;
        _gridChecklist.ReadOnly = true;
        _gridChecklist.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "ClId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "ClDay", HeaderText = "요일", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "ClRole", HeaderText = "역할", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "ClTask", HeaderText = "할일", FillWeight = 45 },
            new DataGridViewTextBoxColumn { Name = "ClOrder", HeaderText = "순서", FillWeight = 8, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "ClActive", HeaderText = "활성", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewButtonColumn { Name = "ClDelete", HeaderText = "삭제", FillWeight = 10, Text = "삭제", UseColumnTextForButtonValue = true }
        );

        _gridChecklist.CellContentClick += async (_, e) =>
        {
            if (e.RowIndex < 0 || _gridChecklist.Columns[e.ColumnIndex].Name != "ClDelete") return;
            var id = (int)_gridChecklist.Rows[e.RowIndex].Cells["ClId"].Value;
            var task = _gridChecklist.Rows[e.RowIndex].Cells["ClTask"].Value?.ToString();
            if (MessageBox.Show($"'{task}' 항목을 삭제하시겠습니까?", "확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                await _checklistRepo.DeleteTemplateAsync(id);
                ToastNotification.Show("삭제 완료.", ToastType.Success);
                await LoadChecklistTemplatesAsync();
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
        };

        page.Controls.Add(_gridChecklist);
        page.Controls.Add(toolbar);
        return page;
    }

    // ========== DB 백업 ==========
    private async void BtnBackup_Click(object? sender, EventArgs e)
    {
        if (_database == null) return;
        if (sender is Button btn) { btn.Enabled = false; btn.Text = "백업 중..."; }

        try
        {
            var path = await _database.BackupAsync();
            ToastNotification.Show($"백업 완료: {Path.GetFileName(path)}", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"백업 실패: {ex.Message}", ToastType.Error);
        }
        finally
        {
            if (sender is Button btn2) { btn2.Enabled = true; btn2.Text = "💾 DB 백업"; }
        }
    }

    // ========== 비밀번호 변경 ==========
    private async void BtnChangePw_Click(object? sender, EventArgs e)
    {
        using var dlg = new AdminPasswordSetupDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _configRepo.SetAsync("admin_password_hash", dlg.PasswordHash);
            ToastNotification.Show("관리자 비밀번호가 변경되었습니다.", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"변경 실패: {ex.Message}", ToastType.Error);
        }
    }

    // ========== DB 초기화 ==========
    private void BtnDbReset_Click(object? sender, EventArgs e)
    {
        // 1단계: 비밀번호 확인
        using var pwDialog = new Form
        {
            Text = "DB 초기화 인증",
            Size = new Size(340, 160),
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ColorPalette.Surface
        };

        var lblMsg = new Label
        {
            Text = "DB 초기화 비밀번호를 입력하세요:",
            Location = new Point(20, 15), Size = new Size(280, 22),
            Font = DesignTokens.FontBody, ForeColor = ColorPalette.Text
        };
        var txtPw = new TextBox
        {
            Location = new Point(20, 45), Size = new Size(280, 28),
            UseSystemPasswordChar = true, Font = DesignTokens.FontBody
        };
        var btnOk = ButtonFactory.CreateDanger("확인", 80);
        btnOk.Location = new Point(120, 85);
        btnOk.DialogResult = DialogResult.OK;
        var btnCancel = ButtonFactory.CreateGhost("취소", 80);
        btnCancel.Location = new Point(210, 85);
        btnCancel.DialogResult = DialogResult.Cancel;

        pwDialog.Controls.AddRange([lblMsg, txtPw, btnOk, btnCancel]);
        pwDialog.AcceptButton = btnOk;
        pwDialog.CancelButton = btnCancel;

        if (pwDialog.ShowDialog(this) != DialogResult.OK) return;

        // 비밀번호 검증
        if (txtPw.Text != "rlawoqja")
        {
            ToastNotification.Show("비밀번호가 일치하지 않습니다.", ToastType.Error);
            return;
        }

        // 2단계: 최종 확인 팝업
        var confirm = MessageBox.Show(
            "⚠ 경고: 모든 데이터가 삭제됩니다.\n\n" +
            "직원, 예약, 매출, 스케줄, 급여, 출퇴근,\n" +
            "인수인계, 체크리스트 등 모든 데이터가 영구 삭제됩니다.\n\n" +
            "정말 초기화하시겠습니까?",
            "DB 초기화 최종 확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        // 3단계: PowerShell로 앱 종료 → DB 삭제 → 재시작
        try
        {
            var appDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CubeManager");
            var dbPath = Path.Combine(appDir, "cubemanager.db");
            var exePath = Application.ExecutablePath;

            // PowerShell 스크립트: 현재 프로세스 종료 대기 → DB 삭제 → 앱 재시작
            var pid = Environment.ProcessId;
            var script = "Start-Sleep -Milliseconds 500; " +
                $"try {{ Wait-Process -Id {pid} -Timeout 10 -ErrorAction SilentlyContinue }} catch {{}}; " +
                $"Remove-Item '{dbPath}' -Force -ErrorAction SilentlyContinue; " +
                $"Remove-Item '{dbPath}-wal' -Force -ErrorAction SilentlyContinue; " +
                $"Remove-Item '{dbPath}-shm' -Force -ErrorAction SilentlyContinue; " +
                $"Start-Process '{exePath}'";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{script}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            System.Diagnostics.Process.Start(psi);

            // 앱 종료
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"초기화 실패: {ex.Message}", ToastType.Error);
        }
    }

    // ========== 현금 보정 ==========
    private async void BtnApplyCash_Click(object? sender, EventArgs e)
    {
        var amount = (int)_numCashAmount.Value;
        if (amount == 0)
        {
            ToastNotification.Show("보정 금액을 입력하세요.", ToastType.Warning);
            return;
        }

        var date = _dtpCashDate.Value.ToString("yyyy-MM-dd");
        var note = _txtCashNote.Text.Trim();
        var desc = $"현금 보정: {note}";

        try
        {
            if (amount > 0)
                await _salesService.AddSaleItemAsync(date, desc, amount, "cash", "revenue");
            else
                await _salesService.AddSaleItemAsync(date, desc, Math.Abs(amount), "cash", "expense");

            ToastNotification.Show($"현금 보정 완료: {(amount > 0 ? "+" : "")}{amount:N0}원", ToastType.Success);
            _numCashAmount.Value = 0;
            _txtCashNote.Clear();
            await LoadAllStatsAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    // ========== 직원 관리 ==========
    private async Task LoadEmployeesAsync()
    {
        try
        {
            var employees = (await _employeeService.GetAllAsync()).ToList();
            _gridEmployees.Rows.Clear();

            foreach (var emp in employees)
            {
                var idx = _gridEmployees.Rows.Add();
                var row = _gridEmployees.Rows[idx];
                row.Cells["EmpId"].Value = emp.Id;
                row.Cells["EmpName"].Value = emp.Name;
                row.Cells["EmpWage"].Value = emp.HourlyWage.ToString("N0");
                row.Cells["EmpPhone"].Value = emp.Phone ?? "";
                row.Cells["EmpStatus"].Value = emp.IsActive ? "활성" : "비활성";
                row.Cells["EmpStatus"].Style.ForeColor =
                    emp.IsActive ? ColorPalette.Success : ColorPalette.MissingRecord;
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"직원 목록 로드 실패: {ex.Message}", ToastType.Error);
        }
    }

    private async void BtnAddEmployee_Click(object? sender, EventArgs e)
    {
        using var dlg = new EmployeeEditDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _employeeService.AddEmployeeAsync(dlg.EmpName, dlg.Wage, dlg.Phone);
            ToastNotification.Show("직원이 추가되었습니다.", ToastType.Success);
            await LoadEmployeesAsync();
        }
        catch (ArgumentException ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Warning);
        }
    }

    private async void BtnDeactivateEmployee_Click(object? sender, EventArgs e)
    {
        if (_gridEmployees.CurrentRow == null) return;
        var id = (int)_gridEmployees.CurrentRow.Cells["EmpId"].Value;
        var name = _gridEmployees.CurrentRow.Cells["EmpName"].Value?.ToString();

        if (MessageBox.Show($"'{name}' 직원을 비활성화하시겠습니까?", "확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            await _employeeService.DeactivateAsync(id);
            ToastNotification.Show($"'{name}' 비활성화 완료.", ToastType.Success);
            await LoadEmployeesAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private async void GridEmployees_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var id = (int)_gridEmployees.Rows[e.RowIndex].Cells["EmpId"].Value;
        var emp = await _employeeService.GetByIdAsync(id);
        if (emp == null) return;

        using var dlg = new EmployeeEditDialog(emp);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _employeeService.UpdateEmployeeAsync(id, dlg.EmpName, dlg.Wage, dlg.Phone);
            ToastNotification.Show("직원 정보가 수정되었습니다.", ToastType.Success);
            await LoadEmployeesAsync();
        }
        catch (ArgumentException ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Warning);
        }
    }

    // ========== 데이터 로드 ==========
    private async Task LoadAllStatsAsync()
    {
        await LoadAttendanceStatsAsync();
        await LoadSalesStatsAsync();
        await LoadSummaryCardsAsync();
        await LoadEmployeesAsync();
        await LoadMiceAsync();
        await LoadChecklistTemplatesAsync();
        await LoadPartsAsync();
    }

    private async Task LoadAttendanceStatsAsync()
    {
        try
        {
            var month = _dtpStatMonth.Value;
            var startDate = new DateTime(month.Year, month.Month, 1).ToString("yyyy-MM-dd");
            var endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month)).ToString("yyyy-MM-dd");

            var employees = (await _employeeService.GetAllAsync()).Where(e => e.IsActive).ToList();
            var allRecords = (await _attendanceService.GetByDateRangeAsync(startDate, endDate)).ToList();

            _gridAttendStats.Rows.Clear();
            var totalLate = 0;

            foreach (var emp in employees)
            {
                var records = allRecords.Where(a => a.EmployeeId == emp.Id).ToList();
                var late = records.Count(a => a.ClockInStatus == "late");
                var early = records.Count(a => a.ClockOutStatus == "early");
                var onTime = records.Count(a => a.ClockInStatus == "on_time");
                var total = records.Count;
                var lateRate = total > 0 ? $"{(double)late / total * 100:F0}%" : "-";

                totalLate += late;

                var idx = _gridAttendStats.Rows.Add();
                var row = _gridAttendStats.Rows[idx];
                row.Cells["EmpName"].Value = emp.Name;
                row.Cells["LateCount"].Value = late;
                row.Cells["EarlyCount"].Value = early;
                row.Cells["OnTimeCount"].Value = onTime;
                row.Cells["TotalDays"].Value = $"{total}일";
                row.Cells["LateRate"].Value = lateRate;

                if (late > 0)
                {
                    row.Cells["LateCount"].Style.ForeColor = ColorPalette.Danger;
                    row.Cells["LateCount"].Style.Font = new Font("맑은 고딕", 10f, FontStyle.Bold);
                }
                if (early > 0)
                {
                    row.Cells["EarlyCount"].Style.ForeColor = ColorPalette.AccentOrange.Main;
                    row.Cells["EarlyCount"].Style.Font = new Font("맑은 고딕", 10f, FontStyle.Bold);
                }
            }

            _summaryCards.UpdateCard(2, $"{totalLate}건");
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private async Task LoadSalesStatsAsync()
    {
        try
        {
            var month = _dtpStatMonth.Value;
            var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
            var today = DateTime.Today;

            _gridSalesStats.Rows.Clear();
            int totalCard = 0, totalCash = 0, totalTransfer = 0, totalRevenue = 0;

            for (var d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(month.Year, month.Month, d);
                if (date > today) break;

                var dateStr = date.ToString("yyyy-MM-dd");
                var daily = await _salesService.GetDailySalesAsync(dateStr);
                var balance = await _salesService.GetCashBalanceAsync(dateStr);

                if (daily == null && balance == null) continue;

                var idx = _gridSalesStats.Rows.Add();
                var row = _gridSalesStats.Rows[idx];
                row.Cells["SaleDate"].Value = date.ToString("MM/dd (ddd)");
                row.Cells["CardAmt"].Value = daily != null ? $"{daily.CardAmount:N0}" : "-";
                row.Cells["CashAmt"].Value = daily != null ? $"{daily.CashAmount:N0}" : "-";
                row.Cells["TransferAmt"].Value = daily != null ? $"{daily.TransferAmount:N0}" : "-";
                row.Cells["TotalAmt"].Value = daily != null ? $"{daily.TotalRevenue:N0}" : "-";
                row.Cells["CashBal"].Value = balance != null ? $"{balance.ClosingBalance:N0}" : "-";

                if (daily != null)
                {
                    totalCard += daily.CardAmount;
                    totalCash += daily.CashAmount;
                    totalTransfer += daily.TransferAmount;
                    totalRevenue += daily.TotalRevenue;
                }
            }

            _summaryCards.UpdateCard(0, $"₩{totalRevenue:N0}");

            var monthExpense = 0;
            for (var d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(month.Year, month.Month, d);
                if (date > today) break;
                var items = await _salesService.GetSaleItemsAsync(date.ToString("yyyy-MM-dd"));
                monthExpense += items.Where(i => i.Category == "expense").Sum(i => i.Amount);
            }
            _summaryCards.UpdateCard(1, $"₩{monthExpense:N0}");
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private async Task LoadSummaryCardsAsync()
    {
        try
        {
            var todayBalance = await _salesService.GetCashBalanceAsync(DateTime.Today.ToString("yyyy-MM-dd"));
            _summaryCards.UpdateCard(3, $"₩{todayBalance?.ClosingBalance ?? 0:N0}");
        }
        catch { /* 무시 */ }
    }

    // ========== 파트 관리 ==========
    private TabPage BuildWorkPartTab()
    {
        var page = new TabPage("파트 관리") { Padding = new Padding(10), BackColor = ColorPalette.Surface };

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 2)
        };

        var btnAdd = ButtonFactory.CreatePrimary("+ 파트 추가", 110);
        btnAdd.Height = 32;
        btnAdd.Click += async (_, _) =>
        {
            using var dlg = new Form
            {
                Text = "파트 추가", Size = new Size(340, 200),
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ColorPalette.Surface
            };
            var y = 15;
            var txtName = new TextBox { Location = new Point(90, y), Size = new Size(220, 25) };
            dlg.Controls.Add(new Label { Text = "파트명:", Location = new Point(15, y + 2), Size = new Size(70, 22), ForeColor = ColorPalette.Text });
            dlg.Controls.Add(txtName);
            y += 35;
            var cmbStart = CreatePartTimeCombo(new Point(90, y));
            dlg.Controls.Add(new Label { Text = "출근시간:", Location = new Point(15, y + 2), Size = new Size(70, 22), ForeColor = ColorPalette.Text });
            dlg.Controls.Add(cmbStart);
            var cmbEnd = CreatePartTimeCombo(new Point(210, y));
            dlg.Controls.Add(new Label { Text = "~", Location = new Point(195, y + 2), Size = new Size(15, 22), ForeColor = ColorPalette.Text });
            dlg.Controls.Add(cmbEnd);
            cmbEnd.SelectedIndex = Math.Min(14, cmbEnd.Items.Count - 1);
            y += 40;
            var btnOk = ButtonFactory.CreatePrimary("추가", 80);
            btnOk.Location = new Point(140, y);
            btnOk.DialogResult = DialogResult.OK;
            var btnCancel = ButtonFactory.CreateGhost("취소", 80);
            btnCancel.Location = new Point(230, y);
            btnCancel.DialogResult = DialogResult.Cancel;
            dlg.Controls.AddRange([btnOk, btnCancel]);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (string.IsNullOrWhiteSpace(txtName.Text)) return;

            try
            {
                var maxOrder = (await _workPartRepo.GetAllAsync()).Max(p => (int?)p.SortOrder) ?? 0;
                await _workPartRepo.InsertAsync(new WorkPart
                {
                    PartName = txtName.Text.Trim(),
                    StartTime = cmbStart.Text,
                    EndTime = cmbEnd.Text,
                    SortOrder = maxOrder + 1
                });
                ToastNotification.Show("파트가 추가되었습니다.", ToastType.Success);
                await LoadPartsAsync();
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
        };
        toolbar.Controls.Add(btnAdd);

        toolbar.Controls.Add(new Label
        {
            Text = "Delete 키로 삭제",
            Font = DesignTokens.FontCaption, ForeColor = ColorPalette.TextTertiary,
            Size = new Size(200, 32), TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(15, 0, 0, 0)
        });

        GridTheme.ApplyTheme(_gridParts);
        _gridParts.AllowUserToAddRows = false;
        _gridParts.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "PartId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "PartName", HeaderText = "파트명", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "PartStart", HeaderText = "출근시간", FillWeight = 20, DefaultCellStyle = GridTheme.CenterStyle },
            new DataGridViewTextBoxColumn { Name = "PartEnd", HeaderText = "퇴근시간", FillWeight = 20, DefaultCellStyle = GridTheme.CenterStyle },
            new DataGridViewTextBoxColumn { Name = "PartOrder", HeaderText = "순서", FillWeight = 10, DefaultCellStyle = GridTheme.CenterStyle },
            new DataGridViewCheckBoxColumn { Name = "PartActive", HeaderText = "활성", FillWeight = 10 }
        );

        _gridParts.CellContentClick += async (_, e) =>
        {
            if (e.RowIndex < 0 || _gridParts.Columns[e.ColumnIndex].Name != "PartActive") return;
            var id = (int)_gridParts.Rows[e.RowIndex].Cells["PartId"].Value;
            var current = (bool)_gridParts.Rows[e.RowIndex].Cells["PartActive"].Value;
            try
            {
                var all = (await _workPartRepo.GetAllAsync()).ToList();
                var part = all.FirstOrDefault(p => p.Id == id);
                if (part != null)
                {
                    part.IsActive = !current;
                    await _workPartRepo.UpdateAsync(part);
                    await LoadPartsAsync();
                }
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
        };

        // 셀 직접 수정 시 DB 저장
        _gridParts.CellValueChanged += async (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var colName = _gridParts.Columns[e.ColumnIndex].Name;
            if (colName is "PartActive") return; // 활성은 CellContentClick에서 처리
            var row = _gridParts.Rows[e.RowIndex];
            var id = (int)row.Cells["PartId"].Value;
            try
            {
                var part = (await _workPartRepo.GetAllAsync()).FirstOrDefault(p => p.Id == id);
                if (part == null) return;
                part.PartName = row.Cells["PartName"].Value?.ToString() ?? part.PartName;
                part.StartTime = row.Cells["PartStart"].Value?.ToString() ?? part.StartTime;
                part.EndTime = row.Cells["PartEnd"].Value?.ToString() ?? part.EndTime;
                if (int.TryParse(row.Cells["PartOrder"].Value?.ToString(), out var order))
                    part.SortOrder = order;
                await _workPartRepo.UpdateAsync(part);
                ToastNotification.Show("파트 정보 저장됨.", ToastType.Success);
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
        };

        _gridParts.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Delete || _gridParts.CurrentRow == null) return;
            var id = (int)_gridParts.CurrentRow.Cells["PartId"].Value;
            var name = _gridParts.CurrentRow.Cells["PartName"].Value?.ToString();
            if (MessageBox.Show($"'{name}' 파트를 삭제하시겠습니까?", "확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                await _workPartRepo.DeleteAsync(id);
                ToastNotification.Show("삭제 완료.", ToastType.Success);
                await LoadPartsAsync();
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
        };

        page.Controls.Add(_gridParts);
        page.Controls.Add(toolbar);
        return page;
    }

    private static ComboBox CreatePartTimeCombo(Point location)
    {
        var cmb = new ComboBox
        {
            Location = location, Size = new Size(100, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = DesignTokens.FontBody
        };
        for (var h = 10; h <= 26; h++)
            for (var m = 0; m < 60; m += 30)
                cmb.Items.Add($"{h:00}:{m:00}");
        cmb.SelectedIndex = 0;
        return cmb;
    }

    private async Task LoadPartsAsync()
    {
        try
        {
            var parts = (await _workPartRepo.GetAllAsync()).ToList();
            _gridParts.Rows.Clear();
            foreach (var p in parts)
                _gridParts.Rows.Add(p.Id, p.PartName, p.StartTime, p.EndTime, p.SortOrder, p.IsActive);
        }
        catch (Exception ex) { ToastNotification.Show($"파트 로드 실패: {ex.Message}", ToastType.Error); }
    }

    // ========== 알람(미끼) 관리 ==========
    private async Task LoadMiceAsync()
    {
        try
        {
            var items = (await _miceRepo.GetAllAsync()).ToList();
            _gridMice.Rows.Clear();

            foreach (var m in items)
            {
                var idx = _gridMice.Rows.Add();
                var row = _gridMice.Rows[idx];
                row.Cells["MiceId"].Value = m.Id;
                row.Cells["MiceTitle"].Value = m.Title;
                row.Cells["MiceContent"].Value = m.Content;
                row.Cells["MiceInterval"].Value = m.IntervalMinutes;
                row.Cells["MiceActive"].Value = m.IsActive;
                row.Cells["MiceLastShown"].Value = m.LastShownAt ?? "-";
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"미끼 목록 로드 실패: {ex.Message}", ToastType.Error);
        }
    }

    private async void BtnAddMice_Click(object? sender, EventArgs e)
    {
        using var dlg = new Form
        {
            Text = "미끼 팝업 등록",
            Size = new Size(400, 260),
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false, MinimizeBox = false,
            Font = new Font("맑은 고딕", 10f),
            BackColor = ColorPalette.Surface
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(15)
        };

        var lblTitle = new Label { Text = "제목:", Size = new Size(340, 20) };
        var txtTitle = new TextBox { Size = new Size(340, 28), PlaceholderText = "팝업 제목" };
        var lblContent = new Label { Text = "내용:", Size = new Size(340, 20), Margin = new Padding(0, 5, 0, 0) };
        var txtContent = new TextBox { Size = new Size(340, 50), Multiline = true, PlaceholderText = "팝업 내용" };
        var lblInterval = new Label { Text = "간격(분):", Size = new Size(340, 20), Margin = new Padding(0, 5, 0, 0) };
        var numInterval = new NumericUpDown { Minimum = 1, Maximum = 1440, Value = 60, Size = new Size(100, 28) };

        var btnOk = ButtonFactory.CreatePrimary("등록", 80);
        btnOk.Height = 32;
        btnOk.Margin = new Padding(0, 10, 0, 0);
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                ToastNotification.Show("제목을 입력하세요.", ToastType.Warning);
                return;
            }
            dlg.Tag = new MicePopup
            {
                Title = txtTitle.Text.Trim(),
                Content = txtContent.Text.Trim(),
                IntervalMinutes = (int)numInterval.Value,
                IsActive = true
            };
            dlg.DialogResult = DialogResult.OK;
        };

        flow.Controls.AddRange(new Control[] { lblTitle, txtTitle, lblContent, txtContent, lblInterval, numInterval, btnOk });
        dlg.Controls.Add(flow);

        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Tag is not MicePopup popup) return;

        try
        {
            await _miceRepo.InsertAsync(popup);
            ToastNotification.Show("미끼 팝업이 등록되었습니다.", ToastType.Success);
            await LoadMiceAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    // ========== 체크리스트 관리 ==========
    private CheckBox[] GetDayCheckBoxes()
    {
        // 체크리스트 탭의 toolbar에서 요일 체크박스들 찾기
        var checklistPage = _gridChecklist.Parent;
        var toolbar = checklistPage?.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
        return toolbar?.Controls.OfType<CheckBox>().Where(c => c.Tag is int).ToArray() ?? [];
    }

    private string GetSelectedRole()
    {
        var checklistPage = _gridChecklist.Parent;
        var toolbar = checklistPage?.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
        var cmbRole = toolbar?.Controls.OfType<ComboBox>().FirstOrDefault(c => c.Tag?.ToString() == "roleCombo");
        return (cmbRole?.SelectedIndex ?? 0) switch
        {
            1 => "open",
            2 => "close",
            3 => "middle1",
            4 => "middle2",
            _ => "all"
        };
    }

    private async Task LoadChecklistTemplatesAsync()
    {
        try
        {
            var dayCheckBoxes = GetDayCheckBoxes();
            var selectedDays = dayCheckBoxes
                .Where(c => c.Checked && c.Tag is int)
                .Select(c =>
                {
                    var uiIndex = (int)c.Tag!; // 0=월,1=화,...6=일
                    return uiIndex < 6 ? uiIndex + 1 : 0; // DB: 0=일,1=월,...6=토
                })
                .ToHashSet();

            if (selectedDays.Count == 0)
            {
                _gridChecklist.Rows.Clear();
                return;
            }

            var allTemplates = (await _checklistRepo.GetAllTemplatesAsync()).ToList();
            var filtered = allTemplates
                .Where(t => selectedDays.Contains(t.DayOfWeek))
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.Role)
                .ThenBy(t => t.SortOrder)
                .ToList();

            var dayLabels = new[] { "일", "월", "화", "수", "목", "금", "토" };

            _gridChecklist.Rows.Clear();

            foreach (var t in filtered)
            {
                var idx = _gridChecklist.Rows.Add();
                var row = _gridChecklist.Rows[idx];
                row.Cells["ClId"].Value = t.Id;
                row.Cells["ClDay"].Value = t.DayOfWeek >= 0 && t.DayOfWeek <= 6 ? dayLabels[t.DayOfWeek] : "?";
                row.Cells["ClRole"].Value = RoleNames.GetValueOrDefault(t.Role, t.Role);
                row.Cells["ClTask"].Value = t.TaskText;
                row.Cells["ClOrder"].Value = t.SortOrder;
                row.Cells["ClActive"].Value = t.IsActive ? "활성" : "비활성";

                // 역할별 색상
                var roleColor = t.Role switch
                {
                    "open" => ColorPalette.AccentBlue.Light,
                    "close" => ColorPalette.AccentOrange.Light,
                    "middle1" => ColorPalette.AccentGreen.Light,
                    "middle2" => ColorPalette.AccentGreen.Light,
                    _ => Color.Transparent
                };
                if (roleColor != Color.Transparent)
                    row.Cells["ClRole"].Style.BackColor = roleColor;
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"체크리스트 로드 실패: {ex.Message}", ToastType.Error);
        }
    }

    private async void BtnAddChecklist_Click(object? sender, EventArgs e)
    {
        var dayCheckBoxes = GetDayCheckBoxes();
        var selectedDays = dayCheckBoxes
            .Where(c => c.Checked && c.Tag is int)
            .Select(c => (int)c.Tag!)
            .ToList();

        if (selectedDays.Count == 0)
        {
            ToastNotification.Show("요일을 1개 이상 선택하세요.", ToastType.Warning);
            return;
        }

        var selectedRole = GetSelectedRole();
        var dayNames = new[] { "월", "화", "수", "목", "금", "토", "일" };
        var daysText = string.Join(",", selectedDays.Select(i => dayNames[i]));
        var roleText = RoleNames.GetValueOrDefault(selectedRole, selectedRole);

        using var dlg = new Form
        {
            Text = $"체크리스트 추가 ({daysText} / {roleText})",
            Size = new Size(400, 180),
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false, MinimizeBox = false,
            Font = new Font("맑은 고딕", 10f),
            BackColor = ColorPalette.Surface
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(15)
        };

        var lblTask = new Label { Text = "할일 텍스트:", Size = new Size(340, 20) };
        var txtTask = new TextBox { Size = new Size(340, 28), PlaceholderText = "체크리스트 항목 입력" };

        var btnOk = ButtonFactory.CreatePrimary("추가", 80);
        btnOk.Height = 32;
        btnOk.Margin = new Padding(0, 10, 0, 0);
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtTask.Text))
            {
                ToastNotification.Show("할일을 입력하세요.", ToastType.Warning);
                return;
            }
            dlg.Tag = txtTask.Text.Trim();
            dlg.DialogResult = DialogResult.OK;
        };

        flow.Controls.AddRange(new Control[] { lblTask, txtTask, btnOk });
        dlg.Controls.Add(flow);

        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Tag is not string taskText) return;

        try
        {
            // 선택된 모든 요일에 대해 템플릿 생성
            foreach (var uiDay in selectedDays)
            {
                var dbDay = uiDay < 6 ? uiDay + 1 : 0; // 월=1,...토=6,일=0
                var existing = (await _checklistRepo.GetTemplatesByDayAsync(dbDay)).ToList();
                var template = new ChecklistTemplate
                {
                    DayOfWeek = dbDay,
                    Role = selectedRole,
                    TaskText = taskText,
                    SortOrder = existing.Count + 1,
                    IsActive = true
                };
                await _checklistRepo.InsertTemplateAsync(template);
            }

            ToastNotification.Show("체크리스트 항목이 추가되었습니다.", ToastType.Success);
            await LoadChecklistTemplatesAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    // ==================== 탭 7: 알림 이력 ====================

    private DataGridView _gridAlerts = null!;
    private DateTimePicker _dtpAlertStart = null!;
    private DateTimePicker _dtpAlertEnd = null!;
    private ComboBox _cmbAlertType = null!;

    private TabPage BuildAlertHistoryTab()
    {
        var page = new TabPage("알림 이력") { Padding = new Padding(10), BackColor = ColorPalette.Surface };

        // 상단 필터
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 5)
        };

        filterPanel.Controls.Add(new Label
        {
            Text = "기간:", Size = new Size(40, 28),
            TextAlign = ContentAlignment.MiddleRight
        });
        _dtpAlertStart = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Size = new Size(110, 28),
            Value = DateTime.Today.AddDays(-30)
        };
        filterPanel.Controls.Add(_dtpAlertStart);

        filterPanel.Controls.Add(new Label
        {
            Text = "~", Size = new Size(20, 28),
            TextAlign = ContentAlignment.MiddleCenter
        });
        _dtpAlertEnd = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Size = new Size(110, 28),
            Value = DateTime.Today
        };
        filterPanel.Controls.Add(_dtpAlertEnd);

        _cmbAlertType = new ComboBox
        {
            Size = new Size(130, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(10, 0, 0, 0)
        };
        _cmbAlertType.Items.AddRange(["전체", "체크리스트 지연", "지각", "인수인계 미확인", "무단결근", "지각 누적"]);
        _cmbAlertType.SelectedIndex = 0;
        filterPanel.Controls.Add(_cmbAlertType);

        var btnSearch = ButtonFactory.CreatePrimary("조회", 60);
        btnSearch.Margin = new Padding(10, 0, 0, 0);
        btnSearch.Click += async (_, _) => await LoadAlertHistoryAsync();
        filterPanel.Controls.Add(btnSearch);

        var btnResolve = ButtonFactory.CreateSuccess("해결 처리", 80);
        btnResolve.Margin = new Padding(5, 0, 0, 0);
        btnResolve.Click += async (_, _) =>
        {
            if (_gridAlerts.SelectedRows.Count == 0) return;
            var id = (int)_gridAlerts.SelectedRows[0].Cells["Id"].Value;
            if (_alertService != null)
            {
                await _alertService.ResolveAlertAsync(id, "관리자");
                ToastNotification.Show("알림이 해결 처리되었습니다.", ToastType.Success);
                await LoadAlertHistoryAsync();
            }
        };
        filterPanel.Controls.Add(btnResolve);

        // 그리드
        _gridAlerts = new DataGridView { Dock = DockStyle.Fill };
        GridTheme.ApplyTheme(_gridAlerts);
        _gridAlerts.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50, Visible = false },
            new DataGridViewTextBoxColumn { Name = "AlertDate", HeaderText = "날짜", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "AlertTime", HeaderText = "시각", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "심각도", Width = 70 },
            new DataGridViewTextBoxColumn { Name = "AlertType", HeaderText = "유형", Width = 110 },
            new DataGridViewTextBoxColumn { Name = "EmployeeName", HeaderText = "직원", Width = 90 },
            new DataGridViewTextBoxColumn { Name = "Message", HeaderText = "내용", Width = 350 },
            new DataGridViewCheckBoxColumn { Name = "IsResolved", HeaderText = "해결", Width = 50 }
        );

        page.Controls.Add(_gridAlerts);
        page.Controls.Add(filterPanel);
        return page;
    }

    private async Task LoadAlertHistoryAsync()
    {
        if (_alertService == null) return;
        var start = _dtpAlertStart.Value.ToString("yyyy-MM-dd");
        var end = _dtpAlertEnd.Value.ToString("yyyy-MM-dd");

        string? typeFilter = _cmbAlertType.SelectedIndex switch
        {
            1 => "checklist_delay",
            2 => "late_arrival",
            3 => "handover_unread",
            4 => "no_show",
            5 => "late_accumulate",
            _ => null
        };

        var alerts = await _alertService.GetAlertHistoryAsync(start, end, typeFilter);
        _gridAlerts.Rows.Clear();

        var typeLabels = new Dictionary<string, string>
        {
            ["checklist_delay"] = "체크리스트 지연",
            ["late_arrival"] = "지각",
            ["handover_unread"] = "인수인계 미확인",
            ["no_show"] = "무단결근",
            ["late_accumulate"] = "지각 누적"
        };

        var severityLabels = new Dictionary<string, string>
        {
            ["info"] = "ℹ️ 정보",
            ["warning"] = "⚠️ 경고",
            ["critical"] = "🔴 심각"
        };

        foreach (var a in alerts)
        {
            var typeLabel = typeLabels.GetValueOrDefault(a.AlertType, a.AlertType);
            var sevLabel = severityLabels.GetValueOrDefault(a.Severity, a.Severity);
            _gridAlerts.Rows.Add(a.Id, a.AlertDate, a.AlertTime, sevLabel, typeLabel,
                a.EmployeeName ?? "-", a.Message, a.IsResolved);
        }
    }
}
