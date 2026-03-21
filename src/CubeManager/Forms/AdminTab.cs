using System.Drawing;
using CubeManager.Controls;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Dialogs;
using CubeManager.Helpers;

namespace CubeManager.Forms;

/// <summary>
/// 관리자 탭 — 비밀번호 인증 후 접근.
/// 현금잔액 수기 보정, 월별 지각/조퇴 통계, 매출 통계.
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

    private Panel _authPanel = null!;
    private Panel _mainPanel = null!;

    // 현금 보정
    private readonly DateTimePicker _dtpCashDate;
    private readonly NumericUpDown _numCashAmount;
    private readonly TextBox _txtCashNote;

    // 지각/조퇴 통계
    private readonly DateTimePicker _dtpStatMonth;
    private readonly DataGridView _gridAttendStats;

    // 매출 통계
    private readonly DataGridView _gridSalesStats;

    // Summary Cards
    private readonly SummaryCardRow _summaryCards;

    // 직원 관리
    private readonly DataGridView _gridEmployees;

    // 미끼관리
    private readonly DataGridView _gridMice;

    // 체크리스트 관리
    private readonly DataGridView _gridChecklist;

    public AdminTab(IConfigRepository configRepo, ISalesService salesService,
        IAttendanceService attendanceService, IEmployeeService employeeService,
        IMicePopupRepository miceRepo, IChecklistRepository checklistRepo,
        Data.Database database)
    {
        _configRepo = configRepo;
        _salesService = salesService;
        _attendanceService = attendanceService;
        _employeeService = employeeService;
        _miceRepo = miceRepo;
        _checklistRepo = checklistRepo;
        _database = database;
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
        _summaryCards = new SummaryCardRow();

        BuildAuthPanel();
        BuildMainPanel();

        Controls.Add(_mainPanel);
        Controls.Add(_authPanel);

        _mainPanel.Visible = false;
        _authPanel.Visible = true;
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

        // 중앙 정렬
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

    // ========== 메인 화면 ==========
    private void BuildMainPanel()
    {
        _mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

        // 상단 헤더
        var header = new Label
        {
            Text = "관리자 대시보드",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Top, Height = 40
        };

        // Summary Cards
        _summaryCards.AddCard("이번 달 매출", "₩0", ColorPalette.AccentGreen.Main, ColorPalette.AccentGreen.Light);
        _summaryCards.AddCard("이번 달 지출", "₩0", ColorPalette.AccentRed.Main, ColorPalette.AccentRed.Light);
        _summaryCards.AddCard("지각 건수", "0건", ColorPalette.AccentOrange.Main, ColorPalette.AccentOrange.Light);
        _summaryCards.AddCard("현금 잔액", "₩0", ColorPalette.AccentBlue.Main, ColorPalette.AccentBlue.Light);

        // === DB 백업 + 현금 보정 패널 (한 줄) ===
        var utilPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 0)
        };

        var btnBackup = ButtonFactory.CreateSuccess("💾 DB 백업", 120);
        btnBackup.Click += BtnBackup_Click;
        utilPanel.Controls.Add(btnBackup);

        var lblBackupInfo = new Label
        {
            Text = "자동 백업: 매주 월/금 오후 5시",
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextTertiary,
            Size = new Size(250, 32),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(10, 0, 0, 0)
        };
        utilPanel.Controls.Add(lblBackupInfo);

        // === 현금 보정 패널 ===
        var cashPanel = new GroupBox
        {
            Text = "현금 잔액 수기 보정",
            Dock = DockStyle.Top, Height = 80,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(10, 5, 10, 5)
        };

        var cashFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 0)
        };

        var nf = new Font("맑은 고딕", 10f, FontStyle.Regular);
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

        // === 하단 분할: 지각/조퇴(좌) + 매출 통계(우) ===
        var bottomSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 500,
            SplitterWidth = 5,
            BackColor = ColorPalette.Border
        };

        // 좌: 지각/조퇴 통계
        var attendPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 5, 5, 0) };
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
            new DataGridViewTextBoxColumn { Name = "LateCount", HeaderText = "지각", FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "EarlyCount", HeaderText = "조퇴", FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "OnTimeCount", HeaderText = "정상", FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "TotalDays", HeaderText = "총 근무일", FillWeight = 14,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "LateRate", HeaderText = "지각률", FillWeight = 14,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } }
        );

        attendPanel.Controls.Add(_gridAttendStats);
        attendPanel.Controls.Add(attendHeader);

        // 우: 매출 통계
        var salesPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5, 5, 0, 0) };
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
            new DataGridViewTextBoxColumn { Name = "TotalAmt", HeaderText = "총매출", FillWeight = 18,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("맑은 고딕", 10f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "CashBal", HeaderText = "현금잔액", FillWeight = 16, DefaultCellStyle = GridTheme.AmountStyle }
        );

        salesPanel.Controls.Add(_gridSalesStats);
        salesPanel.Controls.Add(salesHeader);

        bottomSplit.Panel1.Controls.Add(attendPanel);
        bottomSplit.Panel2.Controls.Add(salesPanel);

        // === 직원 관리 패널 (하단) ===
        var employeePanel = new GroupBox
        {
            Text = "직원 관리",
            Dock = DockStyle.Bottom, Height = 200,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(10, 5, 10, 5)
        };

        var empToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 35,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 2)
        };

        var btnAddEmp = ButtonFactory.CreatePrimary("+ 직원 추가", 110);
        btnAddEmp.Height = 28;
        btnAddEmp.Font = nf;
        btnAddEmp.Click += BtnAddEmployee_Click;
        empToolbar.Controls.Add(btnAddEmp);

        var btnDeactivateEmp = ButtonFactory.CreateDanger("비활성화", 90);
        btnDeactivateEmp.Height = 28;
        btnDeactivateEmp.Font = nf;
        btnDeactivateEmp.Margin = new Padding(10, 0, 0, 0);
        btnDeactivateEmp.Click += BtnDeactivateEmployee_Click;
        empToolbar.Controls.Add(btnDeactivateEmp);

        GridTheme.ApplyTheme(_gridEmployees);
        _gridEmployees.AllowUserToAddRows = false;
        _gridEmployees.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "EmpId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "EmpName", HeaderText = "이름", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "EmpWage", HeaderText = "시급", FillWeight = 20,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "EmpPhone", HeaderText = "전화번호", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "EmpStatus", HeaderText = "상태", FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } }
        );
        _gridEmployees.ReadOnly = true;
        _gridEmployees.CellDoubleClick += GridEmployees_CellDoubleClick;

        employeePanel.Controls.Add(_gridEmployees);
        employeePanel.Controls.Add(empToolbar);

        // === 미끼관리 패널 ===
        var micePanel = new GroupBox
        {
            Text = "미끼관리 (팝업)",
            Dock = DockStyle.Bottom, Height = 180,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(10, 5, 10, 5)
        };

        var miceToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 35,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 2)
        };

        var btnAddMice = ButtonFactory.CreatePrimary("+ 등록", 80);
        btnAddMice.Height = 28;
        btnAddMice.Font = nf;
        btnAddMice.Click += BtnAddMice_Click;
        miceToolbar.Controls.Add(btnAddMice);

        GridTheme.ApplyTheme(_gridMice);
        _gridMice.AllowUserToAddRows = false;
        _gridMice.ReadOnly = true;
        _gridMice.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "MiceId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "MiceTitle", HeaderText = "제목", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "MiceContent", HeaderText = "내용", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "MiceInterval", HeaderText = "간격(분)", FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewCheckBoxColumn { Name = "MiceActive", HeaderText = "활성", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "MiceLastShown", HeaderText = "마지막표시", FillWeight = 25 }
        );
        _gridMice.ReadOnly = false;
        _gridMice.Columns["MiceTitle"]!.ReadOnly = true;
        _gridMice.Columns["MiceContent"]!.ReadOnly = true;
        _gridMice.Columns["MiceInterval"]!.ReadOnly = true;
        _gridMice.Columns["MiceLastShown"]!.ReadOnly = true;
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

        micePanel.Controls.Add(_gridMice);
        micePanel.Controls.Add(miceToolbar);

        // === 체크리스트 관리 패널 ===
        var checklistPanel = new GroupBox
        {
            Text = "체크리스트 관리",
            Dock = DockStyle.Bottom, Height = 200,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(10, 5, 10, 5)
        };

        var checklistToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 35,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 2)
        };

        var cmbDay = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(80, 28), Font = nf
        };
        cmbDay.Items.AddRange(new object[] { "월", "화", "수", "목", "금", "토", "일" });
        cmbDay.SelectedIndex = 0;
        cmbDay.Tag = "dayCombo";
        cmbDay.SelectedIndexChanged += async (_, _) => await LoadChecklistTemplatesAsync();
        checklistToolbar.Controls.Add(cmbDay);

        var btnAddChecklist = ButtonFactory.CreatePrimary("+ 추가", 80);
        btnAddChecklist.Height = 28;
        btnAddChecklist.Font = nf;
        btnAddChecklist.Margin = new Padding(10, 0, 0, 0);
        btnAddChecklist.Click += BtnAddChecklist_Click;
        checklistToolbar.Controls.Add(btnAddChecklist);

        GridTheme.ApplyTheme(_gridChecklist);
        _gridChecklist.AllowUserToAddRows = false;
        _gridChecklist.ReadOnly = true;
        _gridChecklist.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "ClId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "ClTask", HeaderText = "할일", FillWeight = 60 },
            new DataGridViewTextBoxColumn { Name = "ClOrder", HeaderText = "순서", FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "ClActive", HeaderText = "활성", FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewButtonColumn { Name = "ClDelete", HeaderText = "삭제", FillWeight = 10, Text = "삭제",
                UseColumnTextForButtonValue = true }
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

        checklistPanel.Controls.Add(_gridChecklist);
        checklistPanel.Controls.Add(checklistToolbar);

        // 레이아웃 조립 (역순)
        _mainPanel.Controls.Add(bottomSplit);
        _mainPanel.Controls.Add(checklistPanel);
        _mainPanel.Controls.Add(micePanel);
        _mainPanel.Controls.Add(employeePanel);
        _mainPanel.Controls.Add(cashPanel);
        _mainPanel.Controls.Add(utilPanel);
        _mainPanel.Controls.Add(_summaryCards);
        _mainPanel.Controls.Add(header);
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
            {
                // 양수: 현금 수입으로 기록
                await _salesService.AddSaleItemAsync(date, desc, amount, "cash", "revenue");
            }
            else
            {
                // 음수: 현금 지출로 기록 (양수 변환)
                await _salesService.AddSaleItemAsync(date, desc, Math.Abs(amount), "cash", "expense");
            }

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

                // 지각 있으면 빨간 강조
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

            // 지출 합계 계산
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

    // ========== 미끼관리 ==========
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
            FormBorderStyle = FormBorderStyle.FixedDialog,
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
            dlg.Tag = new Core.Models.MicePopup
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

        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Tag is not Core.Models.MicePopup popup) return;

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
    private async Task LoadChecklistTemplatesAsync()
    {
        try
        {
            // 요일 콤보에서 선택된 요일 가져오기
            var cmbDay = _gridChecklist.Parent?.Controls
                .OfType<FlowLayoutPanel>()
                .FirstOrDefault()?.Controls
                .OfType<ComboBox>()
                .FirstOrDefault(c => c.Tag?.ToString() == "dayCombo");

            var dayIndex = cmbDay?.SelectedIndex ?? 0;
            // ComboBox: 0=월,1=화,...6=일 → DB: 0=일,1=월,...6=토
            var dbDay = dayIndex < 6 ? dayIndex + 1 : 0;

            var templates = (await _checklistRepo.GetTemplatesByDayAsync(dbDay)).ToList();
            _gridChecklist.Rows.Clear();

            foreach (var t in templates)
            {
                var idx = _gridChecklist.Rows.Add();
                var row = _gridChecklist.Rows[idx];
                row.Cells["ClId"].Value = t.Id;
                row.Cells["ClTask"].Value = t.TaskText;
                row.Cells["ClOrder"].Value = t.SortOrder;
                row.Cells["ClActive"].Value = t.IsActive ? "활성" : "비활성";
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"체크리스트 로드 실패: {ex.Message}", ToastType.Error);
        }
    }

    private async void BtnAddChecklist_Click(object? sender, EventArgs e)
    {
        // 요일 콤보에서 선택된 요일 가져오기
        var cmbDay = _gridChecklist.Parent?.Controls
            .OfType<FlowLayoutPanel>()
            .FirstOrDefault()?.Controls
            .OfType<ComboBox>()
            .FirstOrDefault(c => c.Tag?.ToString() == "dayCombo");

        var dayIndex = cmbDay?.SelectedIndex ?? 0;
        var dbDay = dayIndex < 6 ? dayIndex + 1 : 0;
        var dayNames = new[] { "월", "화", "수", "목", "금", "토", "일" };
        var dayName = dayIndex < dayNames.Length ? dayNames[dayIndex] : "월";

        using var dlg = new Form
        {
            Text = $"체크리스트 추가 ({dayName}요일)",
            Size = new Size(400, 180),
            FormBorderStyle = FormBorderStyle.FixedDialog,
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
            var existing = (await _checklistRepo.GetTemplatesByDayAsync(dbDay)).ToList();
            var template = new Core.Models.ChecklistTemplate
            {
                DayOfWeek = dbDay,
                TaskText = taskText,
                SortOrder = existing.Count + 1,
                IsActive = true
            };
            await _checklistRepo.InsertTemplateAsync(template);
            ToastNotification.Show("체크리스트 항목이 추가되었습니다.", ToastType.Success);
            await LoadChecklistTemplatesAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }
}
