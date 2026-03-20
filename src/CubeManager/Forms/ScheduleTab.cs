using System.Drawing;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Controls;
using CubeManager.Dialogs;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class ScheduleTab : UserControl
{
    private readonly IScheduleService _scheduleService;
    private readonly IEmployeeService _employeeService;
    private readonly TimeTablePanel _timeTable;
    private readonly Label _lblWeekInfo;
    private readonly Label _lblSummary;
    private int _year, _month, _weekNum;

    public ScheduleTab(IScheduleService scheduleService, IEmployeeService employeeService)
    {
        _scheduleService = scheduleService;
        _employeeService = employeeService;
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Padding = new Padding(10);

        // Top bar
        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 45,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 5)
        };

        var btnPrev = CreateNavButton("◀ 이전주");
        btnPrev.Click += (_, _) => Navigate(-1);

        _lblWeekInfo = new Label
        {
            Size = new Size(280, 32),
            Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(5, 0, 5, 0)
        };

        var btnNext = CreateNavButton("다음주 ▶");
        btnNext.Click += (_, _) => Navigate(1);

        var btnAdd = ButtonFactory.CreatePrimary("+ 스케줄 추가", 130);
        btnAdd.Margin = new Padding(20, 0, 0, 0);
        btnAdd.Click += BtnAddSchedule_Click;

        topBar.Controls.AddRange([btnPrev, _lblWeekInfo, btnNext, btnAdd]);

        // TimeTable
        _timeTable = new TimeTablePanel { Dock = DockStyle.Fill };
        _timeTable.EmptyCellDoubleClicked += TimeTable_EmptyCellDoubleClicked;
        _timeTable.BlockClicked += TimeTable_BlockClicked;

        // Bottom summary
        _lblSummary = new Label
        {
            Dock = DockStyle.Bottom, Height = 30,
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 0, 0, 0)
        };

        Controls.Add(_timeTable);
        Controls.Add(_lblSummary);
        Controls.Add(topBar);

        // 현재 주차 초기화
        var now = DateTime.Today;
        _year = now.Year;
        _month = now.Month;
        _weekNum = TimeHelper.GetWeekOfMonth(now);
        _ = LoadWeekAsync();
    }

    private static Button CreateNavButton(string text) =>
        ButtonFactory.CreateGhost(text, 100);

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
            _lblWeekInfo.Text = $"{_year}년 {_month}월  {_weekNum}주차 ({start:M/d} ~ {end:M/d})";

            var schedules = await _scheduleService.GetWeekScheduleAsync(_year, _month, _weekNum);
            _timeTable.SetData(schedules, start, end);

            // 근무시간 요약
            var empHours = schedules
                .GroupBy(s => s.EmployeeName ?? $"ID:{s.EmployeeId}")
                .Select(g => $"{g.Key}: {g.Sum(s => TimeHelper.CalcHours(s.StartTime, s.EndTime)):F1}h")
                .ToList();
            _lblSummary.Text = empHours.Count > 0
                ? string.Join("  │  ", empHours)
                : "등록된 스케줄이 없습니다.";
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"스케줄 로드 실패: {ex.Message}", ToastType.Error);
        }
    }

    private async void BtnAddSchedule_Click(object? sender, EventArgs e)
    {
        var employees = (await _employeeService.GetActiveAsync()).ToList();
        if (employees.Count == 0)
        {
            ToastNotification.Show("활성 직원이 없습니다. 설정 탭에서 추가하세요.", ToastType.Warning);
            return;
        }

        using var dlg = new ScheduleInputDialog(employees);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _scheduleService.AddScheduleAsync(
                dlg.SelectedEmployeeId, dlg.StartTime, dlg.EndTime,
                dlg.SelectedDays, dlg.SelectedYear, dlg.SelectedMonth,
                dlg.SelectedWeekNums);
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

        using var dlg = new ScheduleInputDialog(employees, e.Date);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _scheduleService.AddScheduleAsync(
                dlg.SelectedEmployeeId, dlg.StartTime, dlg.EndTime,
                dlg.SelectedDays, dlg.SelectedYear, dlg.SelectedMonth,
                dlg.SelectedWeekNums);
            await LoadWeekAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
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
}
