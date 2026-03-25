using System.Drawing;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class AttendanceTab : UserControl
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    private readonly IScheduleService _scheduleService;
    private readonly DataGridView _gridToday;
    private readonly ComboBox _cmbEmployee;
    private readonly Label _lblClock;
    private readonly Button _btnClockIn;
    private readonly Button _btnClockOut;

    public AttendanceTab(IAttendanceService attendanceService,
        IEmployeeService employeeService, IScheduleService scheduleService)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
        _scheduleService = scheduleService;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(20);

        // ─── 상단: 출퇴근 버튼 영역 (중앙 배치) ───
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 160, Padding = new Padding(0) };

        // 중앙 정렬을 위한 내부 패널
        var centerPanel = new Panel
        {
            Size = new Size(360, 140),
            Anchor = AnchorStyles.Top
        };

        // 현재 시각 (대형, 중앙)
        _lblClock = new Label
        {
            Location = new Point(0, 0), Size = new Size(360, 45),
            Font = new Font("Segoe UI", 28f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = DateTime.Now.ToString("HH:mm:ss")
        };

        // 날짜
        var lblDate = new Label
        {
            Location = new Point(0, 45), Size = new Size(360, 22),
            Font = DesignTokens.FontBody,
            ForeColor = ColorPalette.TextSecondary,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = DateTime.Today.ToString("yyyy년 MM월 dd일 (ddd)")
        };

        // 직원 선택
        _cmbEmployee = new ComboBox
        {
            Location = new Point(50, 78), Size = new Size(260, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = DesignTokens.FontBody,
            DisplayMember = "Name"
        };

        // 출근/퇴근 버튼
        _btnClockIn = ButtonFactory.CreatePrimary("출  근", 150);
        _btnClockIn.Location = new Point(20, 114);
        _btnClockIn.Size = new Size(150, 40);
        _btnClockIn.Font = new Font("맑은 고딕", 14f, FontStyle.Bold);
        _btnClockIn.Click += BtnClockIn_Click;

        _btnClockOut = ButtonFactory.CreateDanger("퇴  근", 150);
        _btnClockOut.Location = new Point(190, 114);
        _btnClockOut.Size = new Size(150, 40);
        _btnClockOut.Font = new Font("맑은 고딕", 14f, FontStyle.Bold);
        _btnClockOut.Click += BtnClockOut_Click;

        // 키보드 지원
        _cmbEmployee.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; BtnClockIn_Click(null, EventArgs.Empty); }
            else if (e.KeyCode == Keys.Enter && e.Shift) { e.SuppressKeyPress = true; BtnClockOut_Click(null, EventArgs.Empty); }
        };

        centerPanel.Controls.AddRange([_lblClock, lblDate, _cmbEmployee, _btnClockIn, _btnClockOut]);

        // centerPanel을 topPanel 중앙에 배치
        topPanel.Resize += (_, _) =>
        {
            centerPanel.Location = new Point(
                Math.Max(0, (topPanel.Width - centerPanel.Width) / 2), 5);
        };
        topPanel.Controls.Add(centerPanel);

        // ─── 구분선 ───
        var divider = new Panel
        {
            Dock = DockStyle.Top, Height = 1,
            BackColor = ColorPalette.Border,
            Margin = new Padding(0, 5, 0, 5)
        };

        // ─── 표 헤더 ───
        var lblGridTitle = new Label
        {
            Text = "오늘 근무 현황",
            Font = DesignTokens.FontSectionTitle,
            ForeColor = ColorPalette.TextSecondary,
            Dock = DockStyle.Top, Height = 32,
            Padding = new Padding(0, 8, 0, 0)
        };

        // ─── 표 (Fill로 나머지 공간 전부 사용) ───
        _gridToday = CreateGrid();
        _gridToday.Columns.AddRange(
            Col("이름", 100), Col("예정출근", 80), Col("예정퇴근", 80),
            Col("실제출근", 110), Col("실제퇴근", 110), Col("상태", 80));
        _gridToday.Dock = DockStyle.Fill;

        // Dock 역순 추가 (Fill 마지막)
        Controls.Add(_gridToday);     // Fill (나머지 전부)
        Controls.Add(lblGridTitle);   // Top
        Controls.Add(divider);        // Top
        Controls.Add(topPanel);       // Top

        // 시계 타이머
        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (_, _) => _lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
        timer.Start();

        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        // 오늘 스케줄이 있는 직원 우선 + 그 외 활성 직원
        var employees = (await _employeeService.GetActiveAsync()).ToList();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var schedules = (await _scheduleService.GetByDateAsync(today)).ToList();

        // 스케줄 있는 직원 ID
        var scheduledIds = schedules.Select(s => s.EmployeeId).ToHashSet();

        // 콤보: 스케줄 있는 직원 우선
        var sorted = employees
            .OrderByDescending(e => scheduledIds.Contains(e.Id))
            .ThenBy(e => e.Name)
            .ToList();

        _cmbEmployee.Items.Clear();
        foreach (var emp in sorted) _cmbEmployee.Items.Add(emp);
        if (_cmbEmployee.Items.Count > 0) _cmbEmployee.SelectedIndex = 0;

        await LoadTodayAsync();
    }

    private async Task LoadTodayAsync()
    {
        try
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var employees = (await _employeeService.GetActiveAsync()).ToList();
            var schedules = (await _scheduleService.GetByDateAsync(today)).ToList();
            var records = (await _attendanceService.GetTodayStatusAsync()).ToList();

            var scheduledIds = schedules.Select(s => s.EmployeeId).ToHashSet();
            var clockedInIds = records.Select(r => r.EmployeeId).ToHashSet();

            // 오늘 출근해야 하는 사람 + 스케줄 없지만 출근한 사람
            var showEmployees = employees
                .Where(e => scheduledIds.Contains(e.Id) || clockedInIds.Contains(e.Id))
                .OrderByDescending(e => scheduledIds.Contains(e.Id))
                .ThenBy(e => e.Name)
                .ToList();

            _gridToday.Rows.Clear();
            foreach (var emp in showEmployees)
            {
                var s = schedules.FirstOrDefault(x => x.EmployeeId == emp.Id);
                var att = records.FirstOrDefault(r => r.EmployeeId == emp.Id);
                var idx = _gridToday.Rows.Add();
                var row = _gridToday.Rows[idx];

                row.Cells[0].Value = emp.Name;
                row.Cells[1].Value = s?.StartTime ?? "-";
                row.Cells[2].Value = s?.EndTime ?? "-";

                // 스케줄 없이 출근한 직원 표시
                if (!scheduledIds.Contains(emp.Id))
                {
                    row.Cells[1].Value = "(비예정)";
                    row.Cells[2].Value = "(비예정)";
                    row.Cells[1].Style.ForeColor = ColorPalette.TextTertiary;
                    row.Cells[2].Style.ForeColor = ColorPalette.TextTertiary;
                }

                if (att?.ClockIn != null)
                {
                    var t = DateTime.Parse(att.ClockIn).ToString("HH:mm");
                    row.Cells[3].Value = t;
                    row.Cells[3].Style.ForeColor = att.ClockInStatus == "on_time"
                        ? ColorPalette.OnTime : ColorPalette.Late;
                }
                else row.Cells[3].Value = "-";

                if (att?.ClockOut != null)
                {
                    var t = DateTime.Parse(att.ClockOut).ToString("HH:mm");
                    row.Cells[4].Value = t;
                    row.Cells[4].Style.ForeColor = att.ClockOutStatus == "on_time"
                        ? ColorPalette.OnTime : ColorPalette.Late;
                }
                else row.Cells[4].Value = "-";

                // 상태 표시
                var status = att switch
                {
                    null => "미출근",
                    { ClockOut: not null } => "퇴근",
                    { ClockIn: not null } => "근무중",
                    _ => "-"
                };
                row.Cells[5].Value = status;
                row.Cells[5].Style.ForeColor = status switch
                {
                    "근무중" => ColorPalette.Success,
                    "퇴근" => ColorPalette.TextTertiary,
                    "미출근" => ColorPalette.Danger,
                    _ => ColorPalette.TableText
                };
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private async void BtnClockIn_Click(object? sender, EventArgs e)
    {
        if (_cmbEmployee.SelectedItem is not Employee emp) return;
        try
        {
            var result = await _attendanceService.ClockInAsync(emp.Id);
            var msg = result.ClockInStatus == "on_time" ? "정상 출근" : "지각";
            ToastNotification.Show($"{emp.Name} 출근: {msg}",
                result.ClockInStatus == "on_time" ? ToastType.Success : ToastType.Warning);
            await LoadTodayAsync();
        }
        catch (InvalidOperationException ex) { ToastNotification.Show(ex.Message, ToastType.Warning); }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }

    private async void BtnClockOut_Click(object? sender, EventArgs e)
    {
        if (_cmbEmployee.SelectedItem is not Employee emp) return;
        try
        {
            var result = await _attendanceService.ClockOutAsync(emp.Id);
            var msg = result.ClockOutStatus == "on_time" ? "정상 퇴근" : "조퇴";
            ToastNotification.Show($"{emp.Name} 퇴근: {msg}",
                result.ClockOutStatus == "on_time" ? ToastType.Success : ToastType.Warning);
            await LoadTodayAsync();
        }
        catch (InvalidOperationException ex) { ToastNotification.Show(ex.Message, ToastType.Warning); }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView();
        GridTheme.ApplyTheme(grid);
        return grid;
    }

    private static DataGridViewTextBoxColumn Col(string header, int width) => new()
    {
        HeaderText = header, Width = width, ReadOnly = true
    };
}
