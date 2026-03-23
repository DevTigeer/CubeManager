using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Controls;
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
            return selected.Length > 0 && selected.Length < _weekChecks.Count(c => c.Enabled)
                ? selected : null;
        }
    }

    public ScheduleInputDialog(IEnumerable<Employee> employees, DateTime? defaultDate = null)
    {
        var now = defaultDate ?? DateTime.Today;
        _year = now.Year;

        Text = "스케줄 추가";
        Size = new Size(460, 480);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);
        BackColor = ColorPalette.Surface;

        var y = 12;

        // ─── 타이틀 ───
        Controls.Add(new Label
        {
            Text = "📋 스케줄 추가",
            Location = new Point(20, y), Size = new Size(400, 28),
            Font = new Font("맑은 고딕", 14f, FontStyle.Bold),
            ForeColor = ColorPalette.Primary
        });
        y += 32;

        // 구분선
        Controls.Add(new Panel
        {
            Location = new Point(20, y), Size = new Size(400, 1),
            BackColor = ColorPalette.Border
        });
        y += 12;

        // ─── 섹션 1: 직원 선택 ───
        Controls.Add(CreateSectionLabel("직원 선택", y));
        y += 22;
        _cmbEmployee = new ComboBox
        {
            Location = new Point(25, y), Size = new Size(395, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("맑은 고딕", 10.5f)
        };
        foreach (var emp in employees)
            _cmbEmployee.Items.Add(emp);
        _cmbEmployee.DisplayMember = "Name";
        if (_cmbEmployee.Items.Count > 0) _cmbEmployee.SelectedIndex = 0;
        Controls.Add(_cmbEmployee);

        y += 40;

        // ─── 섹션 2: 근무 시간 ───
        Controls.Add(CreateSectionLabel("근무 시간", y));
        y += 22;

        Controls.Add(new Label
        {
            Text = "출근", Location = new Point(25, y + 3), Size = new Size(35, 20),
            Font = new Font("맑은 고딕", 9f), ForeColor = ColorPalette.TextSecondary
        });
        _cmbStart = CreateTimeCombo(new Point(62, y));

        Controls.Add(new Label
        {
            Text = "→", Location = new Point(170, y + 3), Size = new Size(25, 20),
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
            ForeColor = ColorPalette.TextTertiary,
            TextAlign = ContentAlignment.MiddleCenter
        });

        Controls.Add(new Label
        {
            Text = "퇴근", Location = new Point(200, y + 3), Size = new Size(35, 20),
            Font = new Font("맑은 고딕", 9f), ForeColor = ColorPalette.TextSecondary
        });
        _cmbEnd = CreateTimeCombo(new Point(237, y));
        _cmbEnd.SelectedIndex = Math.Min(14, _cmbEnd.Items.Count - 1);

        Controls.Add(_cmbStart);
        Controls.Add(_cmbEnd);

        y += 42;

        // ─── 섹션 3: 적용 기간 ───
        Controls.Add(CreateSectionLabel("적용 기간", y));
        y += 22;

        Controls.Add(new Label
        {
            Text = $"{_year}년", Location = new Point(25, y + 3), Size = new Size(55, 20),
            Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold), ForeColor = ColorPalette.Text
        });
        _cmbMonth = new ComboBox
        {
            Location = new Point(85, y), Size = new Size(75, 28),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        for (var m = 1; m <= 12; m++) _cmbMonth.Items.Add($"{m}월");
        _cmbMonth.SelectedIndex = now.Month - 1;
        _cmbMonth.SelectedIndexChanged += CmbMonth_Changed;
        Controls.Add(_cmbMonth);

        y += 34;

        // 주차 체크박스 (카드형)
        Controls.Add(new Label
        {
            Text = "주차", Location = new Point(25, y + 3), Size = new Size(35, 20),
            Font = new Font("맑은 고딕", 9f), ForeColor = ColorPalette.TextSecondary
        });
        for (var i = 0; i < 5; i++)
        {
            _weekChecks[i] = new CheckBox
            {
                Text = $"{i + 1}주",
                Location = new Point(68 + i * 68, y),
                Size = new Size(62, 26),
                Tag = i + 1,
                Checked = true,
                Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9f),
                BackColor = ColorPalette.NavActiveBg,
                ForeColor = ColorPalette.Primary
            };
            _weekChecks[i].FlatAppearance.BorderSize = 1;
            _weekChecks[i].FlatAppearance.BorderColor = ColorPalette.Primary;
            _weekChecks[i].FlatAppearance.CheckedBackColor = ColorPalette.NavActiveBg;
            var chk = _weekChecks[i];
            chk.CheckedChanged += (_, _) =>
            {
                chk.BackColor = chk.Checked ? ColorPalette.NavActiveBg : ColorPalette.Surface;
                chk.ForeColor = chk.Checked ? ColorPalette.Primary : ColorPalette.TextTertiary;
                chk.FlatAppearance.BorderColor = chk.Checked ? ColorPalette.Primary : ColorPalette.Border;
            };
            Controls.Add(_weekChecks[i]);
        }
        UpdateWeekCheckboxes();

        y += 38;

        // ─── 섹션 4: 요일 ───
        Controls.Add(new Label
        {
            Text = "요일", Location = new Point(25, y + 3), Size = new Size(35, 20),
            Font = new Font("맑은 고딕", 9f), ForeColor = ColorPalette.TextSecondary
        });
        var dayNames = new[] { "월", "화", "수", "목", "금", "토", "일" };
        var dayValues = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        for (var i = 0; i < 7; i++)
        {
            var isWeekend = i >= 5;
            _dayChecks[i] = new CheckBox
            {
                Text = dayNames[i],
                Location = new Point(68 + i * 50, y),
                Size = new Size(46, 26),
                Tag = dayValues[i],
                Checked = i < 5,
                Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
                BackColor = i < 5 ? ColorPalette.NavActiveBg : ColorPalette.Surface,
                ForeColor = isWeekend ? ColorPalette.Danger
                    : i < 5 ? ColorPalette.Primary : ColorPalette.TextTertiary
            };
            _dayChecks[i].FlatAppearance.BorderSize = 1;
            _dayChecks[i].FlatAppearance.BorderColor = i < 5 ? ColorPalette.Primary : ColorPalette.Border;
            var dc = _dayChecks[i];
            var isWE = isWeekend;
            dc.CheckedChanged += (_, _) =>
            {
                dc.BackColor = dc.Checked ? ColorPalette.NavActiveBg : ColorPalette.Surface;
                dc.ForeColor = isWE ? ColorPalette.Danger
                    : dc.Checked ? ColorPalette.Primary : ColorPalette.TextTertiary;
                dc.FlatAppearance.BorderColor = dc.Checked ? ColorPalette.Primary : ColorPalette.Border;
            };
            Controls.Add(_dayChecks[i]);
        }

        // 기본 날짜 설정
        if (defaultDate.HasValue)
        {
            foreach (var c in _dayChecks) { c.Checked = false; }
            var dow = (int)defaultDate.Value.DayOfWeek;
            var idx = dow == 0 ? 6 : dow - 1;
            _dayChecks[idx].Checked = true;

            var weekOfMonth = TimeHelper.GetWeekOfMonth(defaultDate.Value);
            for (var i = 0; i < 5; i++)
                _weekChecks[i].Checked = (i + 1) == weekOfMonth;
        }

        y += 48;

        // 구분선
        Controls.Add(new Panel
        {
            Location = new Point(20, y - 6), Size = new Size(400, 1),
            BackColor = ColorPalette.Border
        });

        // ─── 버튼 영역 ───
        var btnOk = ButtonFactory.CreatePrimary("적용", 100);
        btnOk.Location = new Point(220, y);
        btnOk.Height = 36;
        btnOk.Click += (_, _) =>
        {
            if (_cmbEmployee.SelectedItem is Employee emp)
            {
                SelectedEmployeeId = emp.Id;
                DialogResult = DialogResult.OK;
            }
        };

        var btnCancel = ButtonFactory.CreateSecondary("취소", 90);
        btnCancel.Location = new Point(330, y);
        btnCancel.Height = 36;
        btnCancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange([btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        // Tab 순서
        _cmbEmployee.TabIndex = 0;
        _cmbStart.TabIndex = 1;
        _cmbEnd.TabIndex = 2;
        _cmbMonth.TabIndex = 3;
        btnOk.TabIndex = 10;
        btnCancel.TabIndex = 11;
    }

    private static Label CreateSectionLabel(string text, int y) => new()
    {
        Text = text,
        Location = new Point(25, y), Size = new Size(200, 18),
        Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
        ForeColor = ColorPalette.TextSecondary
    };

    private void CmbMonth_Changed(object? sender, EventArgs e) => UpdateWeekCheckboxes();

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
            Location = location, Size = new Size(95, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("맑은 고딕", 10f)
        };
        foreach (var slot in TimeHelper.TimeSlots)
            cmb.Items.Add(slot);
        if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        return cmb;
    }
}
