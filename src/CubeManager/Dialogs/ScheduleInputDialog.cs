using System.Drawing;
using CubeManager.Core.Helpers;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Dialogs;

public class ScheduleInputDialog : Form
{
    private readonly ComboBox _cmbEmployee;
    private readonly ComboBox _cmbStart;
    private readonly ComboBox _cmbEnd;
    private readonly CheckBox[] _dayChecks = new CheckBox[7];
    private readonly ComboBox _cmbMonth;
    private readonly CheckBox[] _weekChecks = new CheckBox[5];
    private int _year;

    public int SelectedEmployeeId { get; private set; }
    public string StartTime => _cmbStart.Text;
    public string EndTime => _cmbEnd.Text;
    public DayOfWeek[] SelectedDays => _dayChecks
        .Where(c => c.Checked)
        .Select(c => (DayOfWeek)c.Tag!)
        .ToArray();

    public int SelectedYear => _year;
    public int SelectedMonth => _cmbMonth.SelectedIndex + 1;
    public int[]? SelectedWeekNums
    {
        get
        {
            var selected = _weekChecks
                .Where(c => c.Checked)
                .Select(c => (int)c.Tag!)
                .ToArray();
            // 전부 체크 또는 아무것도 체크 안 하면 null (전체 적용)
            return selected.Length > 0 && selected.Length < _weekChecks.Count(c => c.Enabled)
                ? selected : null;
        }
    }

    public ScheduleInputDialog(IEnumerable<Employee> employees, DateTime? defaultDate = null)
    {
        var now = defaultDate ?? DateTime.Today;
        _year = now.Year;

        Text = "스케줄 추가";
        Size = new Size(400, 400);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);

        var y = 15;

        // 직원 선택
        Controls.Add(new Label { Text = "직원:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        _cmbEmployee = new ComboBox
        {
            Location = new Point(90, y), Size = new Size(270, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (var emp in employees)
            _cmbEmployee.Items.Add(emp);
        _cmbEmployee.DisplayMember = "Name";
        if (_cmbEmployee.Items.Count > 0) _cmbEmployee.SelectedIndex = 0;
        Controls.Add(_cmbEmployee);

        y += 38;

        // 시간
        Controls.Add(new Label { Text = "출근:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        _cmbStart = CreateTimeCombo(new Point(90, y));
        Controls.Add(new Label { Text = "퇴근:", Location = new Point(205, y + 2), Size = new Size(40, 22) });
        _cmbEnd = CreateTimeCombo(new Point(255, y));
        _cmbEnd.SelectedIndex = Math.Min(14, _cmbEnd.Items.Count - 1); // 기본 17:00
        Controls.Add(_cmbStart);
        Controls.Add(_cmbEnd);

        y += 40;

        // 월 선택
        Controls.Add(new Label { Text = "월:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        _cmbMonth = new ComboBox
        {
            Location = new Point(90, y), Size = new Size(80, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        for (var m = 1; m <= 12; m++) _cmbMonth.Items.Add($"{m}월");
        _cmbMonth.SelectedIndex = now.Month - 1;
        _cmbMonth.SelectedIndexChanged += CmbMonth_Changed;
        Controls.Add(_cmbMonth);

        // 연도 표시
        Controls.Add(new Label
        {
            Text = $"{_year}년",
            Location = new Point(180, y + 2), Size = new Size(60, 22),
            ForeColor = ColorPalette.TextSecondary
        });

        y += 38;

        // 주차 선택
        Controls.Add(new Label { Text = "주차:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        for (var i = 0; i < 5; i++)
        {
            _weekChecks[i] = new CheckBox
            {
                Text = $"{i + 1}주",
                Location = new Point(90 + i * 58, y),
                Size = new Size(55, 25),
                Tag = i + 1,
                Checked = true
            };
            Controls.Add(_weekChecks[i]);
        }
        UpdateWeekCheckboxes();

        y += 38;

        // 요일 체크박스
        Controls.Add(new Label { Text = "요일:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        var dayNames = new[] { "월", "화", "수", "목", "금", "토", "일" };
        var dayValues = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        for (var i = 0; i < 7; i++)
        {
            _dayChecks[i] = new CheckBox
            {
                Text = dayNames[i],
                Location = new Point(90 + i * 42, y),
                Size = new Size(42, 25),
                Tag = dayValues[i],
                Checked = i < 5 // 기본: 월~금 체크
            };
            Controls.Add(_dayChecks[i]);
        }

        // 기본 날짜의 요일만 체크
        if (defaultDate.HasValue)
        {
            foreach (var c in _dayChecks) c.Checked = false;
            var dow = (int)defaultDate.Value.DayOfWeek;
            var idx = dow == 0 ? 6 : dow - 1; // 월=0 ~ 일=6
            _dayChecks[idx].Checked = true;

            // 기본 날짜의 주차만 체크
            var weekOfMonth = TimeHelper.GetWeekOfMonth(defaultDate.Value);
            for (var i = 0; i < 5; i++)
                _weekChecks[i].Checked = (i + 1) == weekOfMonth;
        }

        y += 50;
        var btnOk = new Button
        {
            Text = "적용", Location = new Point(200, y), Size = new Size(80, 35),
            BackColor = ColorPalette.Primary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.None
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (_, _) =>
        {
            if (_cmbEmployee.SelectedItem is Employee emp)
            {
                SelectedEmployeeId = emp.Id;
                DialogResult = DialogResult.OK;
            }
        };

        var btnCancel = new Button
        {
            Text = "취소", Location = new Point(290, y), Size = new Size(80, 35),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void CmbMonth_Changed(object? sender, EventArgs e)
    {
        UpdateWeekCheckboxes();
    }

    private void UpdateWeekCheckboxes()
    {
        var month = _cmbMonth.SelectedIndex + 1;
        var totalWeeks = TimeHelper.GetTotalWeeks(_year, month);
        for (var i = 0; i < 5; i++)
        {
            _weekChecks[i].Enabled = (i + 1) <= totalWeeks;
            if (!_weekChecks[i].Enabled) _weekChecks[i].Checked = false;
        }
    }

    private static ComboBox CreateTimeCombo(Point location)
    {
        var cmb = new ComboBox
        {
            Location = location, Size = new Size(90, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (var slot in TimeHelper.TimeSlots)
            cmb.Items.Add(slot);
        if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        return cmb;
    }
}
