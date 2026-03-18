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
    private readonly DataGridView _gridHistory;
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
        BackColor = Color.White;
        Padding = new Padding(15);

        // Header
        var header = new Label
        {
            Text = $"출/퇴근 관리     {DateTime.Today:yyyy-MM-dd (ddd)}",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Top, Height = 40
        };

        // Split: Left=오늘현황+버튼, Right=이력
        var splitPanel = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 500
        };

        // === Left Panel ===
        var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 10, 0) };

        var lblToday = new Label
        {
            Text = "오늘 근무 현황",
            Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
            Dock = DockStyle.Top, Height = 30
        };

        _gridToday = CreateGrid();
        _gridToday.Columns.AddRange(
            Col("이름", 80), Col("예정출근", 70), Col("예정퇴근", 70),
            Col("실제출근", 100), Col("실제퇴근", 100));
        _gridToday.Dock = DockStyle.Top;
        _gridToday.Height = 200;

        // 출퇴근 버튼 영역
        var btnPanel = new Panel { Dock = DockStyle.Top, Height = 130, Padding = new Padding(0, 10, 0, 0) };

        var lblEmp = new Label { Text = "직원:", Location = new Point(10, 12), Size = new Size(40, 22) };
        _cmbEmployee = new ComboBox
        {
            Location = new Point(55, 10), Size = new Size(180, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "Name"
        };

        _lblClock = new Label
        {
            Location = new Point(10, 90), Size = new Size(230, 25),
            Font = new Font("맑은 고딕", 12f),
            ForeColor = ColorPalette.TextSecondary,
            Text = DateTime.Now.ToString("현재: HH:mm:ss")
        };

        _btnClockIn = new Button
        {
            Text = "출  근", Location = new Point(10, 48), Size = new Size(110, 38),
            BackColor = ColorPalette.Primary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 13f, FontStyle.Bold)
        };
        _btnClockIn.FlatAppearance.BorderSize = 0;
        _btnClockIn.Click += BtnClockIn_Click;

        _btnClockOut = new Button
        {
            Text = "퇴  근", Location = new Point(130, 48), Size = new Size(110, 38),
            BackColor = ColorPalette.Danger, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 13f, FontStyle.Bold)
        };
        _btnClockOut.FlatAppearance.BorderSize = 0;
        _btnClockOut.Click += BtnClockOut_Click;

        btnPanel.Controls.AddRange([lblEmp, _cmbEmployee, _btnClockIn, _btnClockOut, _lblClock]);

        leftPanel.Controls.Add(btnPanel);
        leftPanel.Controls.Add(_gridToday);
        leftPanel.Controls.Add(lblToday);

        // === Right Panel: 이력 ===
        var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 10, 0, 0) };
        var lblHistory = new Label
        {
            Text = "이번달 출퇴근 이력",
            Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
            Dock = DockStyle.Top, Height = 30
        };
        _gridHistory = CreateGrid();
        _gridHistory.Columns.AddRange(
            Col("날짜", 85), Col("이름", 65), Col("출근", 80), Col("퇴근", 80), Col("상태", 80));
        _gridHistory.Dock = DockStyle.Fill;

        rightPanel.Controls.Add(_gridHistory);
        rightPanel.Controls.Add(lblHistory);

        splitPanel.Panel1.Controls.Add(leftPanel);
        splitPanel.Panel2.Controls.Add(rightPanel);

        Controls.Add(splitPanel);
        Controls.Add(header);

        // 시계 타이머
        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (_, _) => _lblClock.Text = DateTime.Now.ToString("현재: HH:mm:ss");
        timer.Start();

        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        var employees = (await _employeeService.GetActiveAsync()).ToList();
        _cmbEmployee.Items.Clear();
        foreach (var emp in employees) _cmbEmployee.Items.Add(emp);
        if (_cmbEmployee.Items.Count > 0) _cmbEmployee.SelectedIndex = 0;

        await LoadTodayAsync();
    }

    private async Task LoadTodayAsync()
    {
        try
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var schedules = (await _scheduleService.GetByDateAsync(today)).ToList();
            var records = (await _attendanceService.GetTodayStatusAsync()).ToList();

            _gridToday.Rows.Clear();
            foreach (var s in schedules)
            {
                var att = records.FirstOrDefault(r => r.EmployeeId == s.EmployeeId);
                var idx = _gridToday.Rows.Add();
                var row = _gridToday.Rows[idx];

                row.Cells[0].Value = s.EmployeeName;
                row.Cells[1].Value = s.StartTime;
                row.Cells[2].Value = s.EndTime;

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
            }

            // 이력
            if (_cmbEmployee.SelectedItem is Employee emp)
                await LoadHistoryAsync(emp.Id);
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private async Task LoadHistoryAsync(int employeeId)
    {
        var ym = DateTime.Today.ToString("yyyy-MM");
        var history = await _attendanceService.GetMonthlyHistoryAsync(employeeId, ym);
        _gridHistory.Rows.Clear();
        foreach (var h in history)
        {
            var idx = _gridHistory.Rows.Add();
            var row = _gridHistory.Rows[idx];
            row.Cells[0].Value = h.WorkDate;
            row.Cells[1].Value = h.EmployeeName;

            if (h.ClockIn != null)
            {
                row.Cells[2].Value = DateTime.Parse(h.ClockIn).ToString("HH:mm");
                row.Cells[2].Style.ForeColor = h.ClockInStatus == "on_time"
                    ? ColorPalette.OnTime : ColorPalette.Late;
            }
            if (h.ClockOut != null)
            {
                row.Cells[3].Value = DateTime.Parse(h.ClockOut).ToString("HH:mm");
                row.Cells[3].Style.ForeColor = h.ClockOutStatus == "on_time"
                    ? ColorPalette.OnTime : ColorPalette.Late;
            }

            var status = (h.ClockInStatus, h.ClockOutStatus) switch
            {
                ("late", _) => "지각",
                (_, "early") => "조퇴",
                ("on_time", "on_time") => "정상",
                _ => "-"
            };
            row.Cells[4].Value = status;
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

    private static DataGridView CreateGrid() => new()
    {
        AllowUserToAddRows = false, ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false, BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle, GridColor = ColorPalette.Border,
        EnableHeadersVisualStyles = false,
        DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("맑은 고딕", 9f) },
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            BackColor = ColorPalette.Background
        }
    };

    private static DataGridViewTextBoxColumn Col(string header, int width) => new()
    {
        HeaderText = header, Width = width, ReadOnly = true
    };
}
