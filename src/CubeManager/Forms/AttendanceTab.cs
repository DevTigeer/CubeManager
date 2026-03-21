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
        Padding = new Padding(15);

        // Header
        var header = new Label
        {
            Text = $"출/퇴근 관리     {DateTime.Today:yyyy-MM-dd (ddd)}",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Top, Height = 40
        };

        // === 오늘 근무 현황 ===
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

        _btnClockIn = ButtonFactory.CreatePrimary("출  근");
        _btnClockIn.Location = new Point(10, 48);
        _btnClockIn.Size = new Size(110, 38);
        _btnClockIn.Font = new Font("맑은 고딕", 13f, FontStyle.Bold);
        _btnClockIn.Click += BtnClockIn_Click;

        _btnClockOut = ButtonFactory.CreateDanger("퇴  근");
        _btnClockOut.Location = new Point(130, 48);
        _btnClockOut.Size = new Size(110, 38);
        _btnClockOut.Font = new Font("맑은 고딕", 13f, FontStyle.Bold);
        _btnClockOut.Click += BtnClockOut_Click;

        btnPanel.Controls.AddRange([lblEmp, _cmbEmployee, _btnClockIn, _btnClockOut, _lblClock]);

        leftPanel.Controls.Add(btnPanel);
        leftPanel.Controls.Add(_gridToday);
        leftPanel.Controls.Add(lblToday);

        Controls.Add(leftPanel);
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
            var employees = (await _employeeService.GetActiveAsync()).ToList();
            var schedules = (await _scheduleService.GetByDateAsync(today)).ToList();
            var records = (await _attendanceService.GetTodayStatusAsync()).ToList();

            _gridToday.Rows.Clear();
            foreach (var emp in employees)
            {
                var s = schedules.FirstOrDefault(x => x.EmployeeId == emp.Id);
                var att = records.FirstOrDefault(r => r.EmployeeId == emp.Id);
                var idx = _gridToday.Rows.Add();
                var row = _gridToday.Rows[idx];

                row.Cells[0].Value = emp.Name;
                row.Cells[1].Value = s?.StartTime ?? "-";
                row.Cells[2].Value = s?.EndTime ?? "-";

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
