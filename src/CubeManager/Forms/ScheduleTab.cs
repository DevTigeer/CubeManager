using System.Drawing;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Controls;
using CubeManager.Dialogs;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class ScheduleTab : UserControl
{
    private readonly IScheduleService _scheduleService;
    private readonly IEmployeeService _employeeService;
    private readonly IHolidayRepository _holidayRepo;
    private readonly IWorkPartRepository _workPartRepo;
    private readonly TimeTablePanel _timeTable;
    private readonly Label _lblDateRange;
    private readonly Label _lblWeekSub;
    private readonly Panel _summaryPanel;
    private int _year, _month, _weekNum;

    public ScheduleTab(IScheduleService scheduleService, IEmployeeService employeeService,
        IHolidayRepository holidayRepo, IWorkPartRepository workPartRepo)
    {
        _scheduleService = scheduleService;
        _employeeService = employeeService;
        _holidayRepo = holidayRepo;
        _workPartRepo = workPartRepo;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(12);

        // ── 상단 헤더 ──
        var topBar = new Panel { Dock = DockStyle.Top, Height = 50 };

        // 좌: 네비게이션
        var _tip = new ToolTip();

        var btnPrev = ButtonFactory.CreateGhost("◀", 36);
        btnPrev.Location = new Point(0, 10);
        btnPrev.Click += (_, _) => Navigate(-1);
        _tip.SetToolTip(btnPrev, "이전 주");

        var btnNext = ButtonFactory.CreateGhost("▶", 36);
        btnNext.Location = new Point(40, 10);
        btnNext.Click += (_, _) => Navigate(1);
        _tip.SetToolTip(btnNext, "다음 주");

        // 중앙: 날짜 범위 (주 정보)
        _lblDateRange = new Label
        {
            AutoSize = false, Size = new Size(220, 24),
            Location = new Point(90, 4),
            Font = new Font("맑은 고딕", 13f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _lblWeekSub = new Label
        {
            AutoSize = false, Size = new Size(160, 18),
            Location = new Point(90, 28),
            Font = new Font("맑은 고딕", 8.5f),
            ForeColor = ColorPalette.TextTertiary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // 우: 스케줄 추가
        var btnAdd = ButtonFactory.CreatePrimary("+ 스케줄 추가", 120);
        btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAdd.Location = new Point(topBar.Width - 132, 10);
        btnAdd.Click += BtnAddSchedule_Click;
        _tip.SetToolTip(btnAdd, "새 스케줄 추가 (직원/시간/요일 선택)");

        topBar.Controls.AddRange([btnPrev, btnNext, _lblDateRange, _lblWeekSub, btnAdd]);
        topBar.Resize += (_, _) => btnAdd.Location = new Point(topBar.Width - 132, 10);

        // ── 하단: 직원별 주간 요약 ──
        _summaryPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            Padding = new Padding(0, 6, 0, 0)
        };

        // ── 중앙: TimeTable ──
        _timeTable = new TimeTablePanel { Dock = DockStyle.Fill };
        _timeTable.EmptyCellDoubleClicked += TimeTable_EmptyCellDoubleClicked;
        _timeTable.BlockClicked += TimeTable_BlockClicked;
        _timeTable.BlockEditRequested += TimeTable_BlockEditRequested;

        Controls.Add(_timeTable);
        Controls.Add(_summaryPanel);
        Controls.Add(topBar);

        // 현재 주차 초기화
        var now = DateTime.Today;
        _year = now.Year;
        _month = now.Month;
        _weekNum = TimeHelper.GetWeekOfMonth(now);
        _ = LoadWeekAsync();
    }

    private void Navigate(int direction)
    {
        _weekNum += direction;
        var totalWeeks = TimeHelper.GetTotalWeeks(_year, _month);

        if (_weekNum > totalWeeks)
        {
            _month++;
            if (_month > 12) { _month = 1; _year++; }
            _weekNum = 1;
        }
        else if (_weekNum < 1)
        {
            _month--;
            if (_month < 1) { _month = 12; _year--; }
            _weekNum = TimeHelper.GetTotalWeeks(_year, _month);
        }

        _ = LoadWeekAsync();
    }

    private async Task LoadWeekAsync()
    {
        try
        {
            var (start, end) = TimeHelper.GetWeekRange(_year, _month, _weekNum);

            // 헤더 업데이트: 날짜 범위 우선, 주차 보조
            _lblDateRange.Text = $"{start:yyyy.MM.dd} - {end:MM.dd}";
            _lblWeekSub.Text = $"{_month}월 {_weekNum}주차";

            var schedules = await _scheduleService.GetWeekScheduleAsync(_year, _month, _weekNum);

            var holidays = await _holidayRepo.GetByYearAsync(start.Year);
            var holidayDates = new HashSet<string>(holidays
                .Where(h => !h.IsWeekend)
                .Select(h => h.HolidayDate));

            _timeTable.SetData(schedules, start, end, holidayDates);

            // 직원별 주간 요약 렌더링
            RenderWeeklySummary(schedules.ToList(), start, end);
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"스케줄 로드 실패: {ex.Message}", ToastType.Error);
        }
    }

    /// <summary>하단 직원별 주간 요약 (색상 점 + 이름 + 총시간 + 근무일)</summary>
    private void RenderWeeklySummary(List<Schedule> schedules, DateTime start, DateTime end)
    {
        _summaryPanel.Controls.Clear();

        if (schedules.Count == 0)
        {
            _summaryPanel.Controls.Add(new Label
            {
                Text = "등록된 스케줄이 없습니다.",
                Font = new Font("맑은 고딕", 9f),
                ForeColor = ColorPalette.TextTertiary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            });
            return;
        }

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0)
        };

        var empGroups = schedules
            .GroupBy(s => new { s.EmployeeId, s.EmployeeName })
            .OrderBy(g => g.Key.EmployeeName);

        foreach (var grp in empGroups)
        {
            var name = grp.Key.EmployeeName ?? $"ID:{grp.Key.EmployeeId}";
            var totalHours = grp.Sum(s => TimeHelper.CalcHours(s.StartTime, s.EndTime));

            // 시간 포맷
            var hoursStr = totalHours % 1 == 0 ? $"{(int)totalHours}h" : $"{totalHours:F1}h";

            var chip = new Panel
            {
                Size = new Size(180, 36),
                Margin = new Padding(0, 0, 8, 0)
            };

            // 색상 점
            var dot = new Panel
            {
                Size = new Size(10, 10),
                Location = new Point(4, 13),
                BackColor = ColorPalette.GetEmployeeColor(
                    grp.Key.EmployeeId % ColorPalette.EmployeeColors.Length)
            };
            // 둥근 점
            dot.Paint += (_, pe) =>
            {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var b = new SolidBrush(dot.BackColor);
                pe.Graphics.FillEllipse(b, 0, 0, 9, 9);
            };

            var lblName = new Label
            {
                Text = $"{name}  {hoursStr}",
                Location = new Point(18, 0),
                Size = new Size(158, 36),
                Font = new Font("맑은 고딕", 8.5f),
                ForeColor = ColorPalette.Text,
                TextAlign = ContentAlignment.MiddleLeft
            };

            chip.Controls.AddRange([dot, lblName]);
            flow.Controls.Add(chip);
        }

        _summaryPanel.Controls.Add(flow);
    }

    private async void BtnAddSchedule_Click(object? sender, EventArgs e)
    {
        var employees = (await _employeeService.GetActiveAsync()).ToList();
        if (employees.Count == 0)
        {
            ToastNotification.Show("활성 직원이 없습니다. 관리자 탭에서 추가하세요.", ToastType.Warning);
            return;
        }

        var parts = (await _workPartRepo.GetActiveAsync()).ToList();
        using var dlg = new ScheduleInputDialog(employees, parts);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await AddScheduleFromDialog(dlg);
            ToastNotification.Show("스케줄이 등록되었습니다.", ToastType.Success);
            await LoadWeekAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"스케줄 등록 실패: {ex.Message}", ToastType.Error);
        }
    }

    private async void TimeTable_EmptyCellDoubleClicked(object? sender, EmptyCellClickEventArgs e)
    {
        var employees = (await _employeeService.GetActiveAsync()).ToList();
        if (employees.Count == 0) return;

        var parts = (await _workPartRepo.GetActiveAsync()).ToList();
        using var dlg = new ScheduleInputDialog(employees, parts, e.Date);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await AddScheduleFromDialog(dlg);
            await LoadWeekAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    /// <summary>다이얼로그 결과로 스케줄 추가. 시간은 항상 StartTime~EndTime (파트 범위 병합됨).</summary>
    private async Task AddScheduleFromDialog(ScheduleInputDialog dlg)
    {
        // 파트 복수 선택 시 전체 범위가 이미 StartTime/EndTime에 반영됨
        await _scheduleService.AddScheduleAsync(
            dlg.SelectedEmployeeId, dlg.StartTime, dlg.EndTime,
            dlg.SelectedDays, dlg.SelectedYear, dlg.SelectedMonth,
            dlg.SelectedWeekNums);
    }

    private async void TimeTable_BlockClicked(object? sender, ScheduleBlockClickEventArgs e)
    {
        var result = MessageBox.Show(
            $"{e.Schedule.EmployeeName}\n{e.Schedule.WorkDate} {e.Schedule.StartTime}~{e.Schedule.EndTime}\n\n삭제하시겠습니까?",
            "스케줄", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        await _scheduleService.DeleteScheduleAsync(e.Schedule.Id);
        ToastNotification.Show("스케줄이 삭제되었습니다.", ToastType.Success);
        await LoadWeekAsync();
    }

    private async void TimeTable_BlockEditRequested(object? sender, ScheduleBlockClickEventArgs e)
    {
        var employees = (await _employeeService.GetActiveAsync()).ToList();
        if (employees.Count == 0) return;

        // 직원 선택 다이얼로그
        using var dlg = new Form
        {
            Text = "직원 변경",
            Size = new Size(300, 160),
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ColorPalette.Surface
        };

        var lbl = new Label
        {
            Text = $"{e.Schedule.EmployeeName} ({e.Schedule.StartTime}~{e.Schedule.EndTime}) → 변경할 직원:",
            Location = new Point(15, 12), Size = new Size(260, 36),
            Font = DesignTokens.FontBody, ForeColor = ColorPalette.Text
        };

        var cmb = new ComboBox
        {
            Location = new Point(15, 52), Size = new Size(260, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = DesignTokens.FontBody
        };
        foreach (var emp in employees) cmb.Items.Add(emp);
        cmb.DisplayMember = "Name";
        // 현재 직원 선택
        var currentIdx = employees.FindIndex(emp => emp.Id == e.Schedule.EmployeeId);
        if (currentIdx >= 0) cmb.SelectedIndex = currentIdx;

        var btnOk = ButtonFactory.CreatePrimary("변경", 80);
        btnOk.Location = new Point(110, 95);
        btnOk.DialogResult = DialogResult.OK;
        var btnCancel = ButtonFactory.CreateGhost("취소", 80);
        btnCancel.Location = new Point(200, 95);
        btnCancel.DialogResult = DialogResult.Cancel;

        dlg.Controls.AddRange([lbl, cmb, btnOk, btnCancel]);
        dlg.AcceptButton = btnOk;
        dlg.CancelButton = btnCancel;

        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (cmb.SelectedItem is not Employee selected) return;
        if (selected.Id == e.Schedule.EmployeeId) return; // 같은 직원이면 무시

        try
        {
            await _scheduleService.ChangeEmployeeAsync(e.Schedule.Id, selected.Id);
            ToastNotification.Show($"직원이 {selected.Name}(으)로 변경되었습니다.", ToastType.Success);
            await LoadWeekAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"변경 실패: {ex.Message}", ToastType.Error);
        }
    }

    /// <summary>외부에서 호출하여 데이터 새로고침</summary>
    public async Task RefreshAsync() => await LoadWeekAsync();
}
