using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Core.Helpers;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>주간 타임테이블 커스텀 GDI+ Panel</summary>
public class TimeTablePanel : Panel
{
    private List<Schedule> _schedules = [];
    private DateTime _weekStart;
    private DateTime _weekEnd;
    private HashSet<string> _holidayDates = new();
    private int _headerHeight = 36;
    private int _timeColWidth = 60;
    private readonly Dictionary<int, int> _employeeColorIndex = new();
    private int _nextColorIndex;

    // 클릭 이벤트
    public event EventHandler<ScheduleBlockClickEventArgs>? BlockClicked;
    public event EventHandler<EmptyCellClickEventArgs>? EmptyCellDoubleClicked;

    public TimeTablePanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    public void SetData(IEnumerable<Schedule> schedules, DateTime weekStart, DateTime weekEnd, HashSet<string>? holidayDates = null)
    {
        _schedules = schedules.ToList();
        _weekStart = weekStart;
        _weekEnd = weekEnd;
        _holidayDates = holidayDates ?? new();
        Invalidate();
    }

    private Color GetEmployeeColor(int employeeId)
    {
        if (!_employeeColorIndex.TryGetValue(employeeId, out var idx))
        {
            idx = _nextColorIndex++;
            _employeeColorIndex[employeeId] = idx;
        }
        return ColorPalette.GetEmployeeColor(idx);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var slots = TimeHelper.TimeSlots;
        var days = (int)(_weekEnd - _weekStart).TotalDays + 1;
        if (days <= 0) return;

        var cellW = (Width - _timeColWidth) / days;
        var cellH = Math.Max(18, (Height - _headerHeight) / slots.Length);

        // 헤더 (요일)
        using var headerFont = new Font("맑은 고딕", 9f, FontStyle.Bold);
        using var cellFont = new Font("맑은 고딕", 8f);
        using var headerBrush = new SolidBrush(ColorPalette.Background);
        using var borderPen = new Pen(ColorPalette.Border);
        using var textBrush = new SolidBrush(ColorPalette.Text);
        using var subTextBrush = new SolidBrush(ColorPalette.TextSecondary);

        g.FillRectangle(headerBrush, 0, 0, Width, _headerHeight);
        var dayNames = new[] { "일", "월", "화", "수", "목", "금", "토" };
        for (var d = 0; d < days; d++)
        {
            var date = _weekStart.AddDays(d);
            var x = _timeColWidth + d * cellW;
            var txt = $"{date:M/d} ({dayNames[(int)date.DayOfWeek]})";
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isHoliday = _holidayDates.Contains(date.ToString("yyyy-MM-dd"));
            var isRedDay = isWeekend || isHoliday;
            using var dayBrush = new SolidBrush(isRedDay ? ColorPalette.Danger : ColorPalette.Text);
            g.DrawString(txt, headerFont, dayBrush,
                new RectangleF(x, 0, cellW, _headerHeight),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            g.DrawLine(borderPen, x, 0, x, Height);
        }

        // 시간 라벨 + 격자
        for (var r = 0; r < slots.Length; r++)
        {
            var y = _headerHeight + r * cellH;
            g.DrawLine(borderPen, 0, y, Width, y);
            if (r % 2 == 0) // 정시만 표시
            {
                g.DrawString(slots[r], cellFont, subTextBrush,
                    new RectangleF(2, y, _timeColWidth - 4, cellH),
                    new StringFormat { LineAlignment = StringAlignment.Center });
            }
        }

        // 스케줄 블록 렌더링 (겹침 시 N등분)
        using var nameFont = new Font("맑은 고딕", 10f, FontStyle.Bold);
        using var timeFont = new Font("맑은 고딕", 8.5f, FontStyle.Regular);
        var grouped = _schedules.GroupBy(s => new { s.EmployeeId, s.WorkDate });
        var blockList = new List<(int dayIdx, int startSlot, int endSlot, Schedule sched)>();

        foreach (var group in grouped)
        {
            var sched = group.First();
            var date = DateTime.Parse(sched.WorkDate);
            var dayIdx = (int)(date - _weekStart).TotalDays;
            if (dayIdx < 0 || dayIdx >= days) continue;

            var startSlot = Array.IndexOf(slots, sched.StartTime);
            var endSlot = Array.IndexOf(slots, sched.EndTime);
            if (startSlot < 0 || endSlot < 0) continue;
            if (endSlot <= startSlot) endSlot = slots.Length - 1;

            blockList.Add((dayIdx, startSlot, endSlot, sched));
        }

        // 같은 날짜에서 시간이 겹치는 블록끼리 그룹핑
        var dayGroups = blockList.GroupBy(b => b.dayIdx);
        foreach (var dayGroup in dayGroups)
        {
            var dayBlocks = dayGroup.OrderBy(b => b.startSlot).ToList();

            // 각 블록에 대해 겹침 수와 인덱스 계산
            for (var i = 0; i < dayBlocks.Count; i++)
            {
                var current = dayBlocks[i];
                var overlapping = dayBlocks
                    .Where(other => other.startSlot < current.endSlot && other.endSlot > current.startSlot)
                    .OrderBy(o => o.startSlot).ThenBy(o => o.sched.EmployeeId)
                    .ToList();

                var overlapCount = overlapping.Count;
                var overlapIndex = overlapping.FindIndex(o =>
                    o.sched.EmployeeId == current.sched.EmployeeId &&
                    o.sched.WorkDate == current.sched.WorkDate);
                if (overlapIndex < 0) overlapIndex = 0;

                var totalW = cellW - 4;
                var blockW = totalW / overlapCount;
                var x = _timeColWidth + current.dayIdx * cellW + 2 + overlapIndex * blockW;
                var y = _headerHeight + current.startSlot * cellH + 1;
                var h = (current.endSlot - current.startSlot) * cellH - 2;

                var color = GetEmployeeColor(current.sched.EmployeeId);
                var rect = new Rectangle(x, y, blockW, Math.Max(h, cellH));

                // 블록 배경 (4px 라운드)
                using var blockPath = RoundedCard.CreateRoundedPath(rect, 4);
                using var fillBrush = new SolidBrush(color);
                g.FillPath(fillBrush, blockPath);

                // 좌측 악센트 바 (3px, 좌상+좌하만 라운드)
                var accentRect = new Rectangle(rect.X, rect.Y, 3, rect.Height);
                var accentColor = Color.FromArgb(
                    Math.Max(color.R - 60, 0),
                    Math.Max(color.G - 60, 0),
                    Math.Max(color.B - 60, 0));
                using var accentPath = CreateLeftRoundedPath(accentRect, 4);
                using var accentBrush = new SolidBrush(accentColor);
                g.FillPath(accentBrush, accentPath);

                // 이름 표시 (Bold, 좌측 8px 오프셋)
                var name = current.sched.EmployeeName ?? $"ID:{current.sched.EmployeeId}";
                using var nameBrush = new SolidBrush(ColorPalette.Text);
                var textX = rect.X + 8;
                var textY = rect.Y + 2;
                var textW = rect.Width - 10;
                g.DrawString(name, nameFont, nameBrush,
                    new RectangleF(textX, textY, textW, nameFont.Height + 2),
                    new StringFormat { Trimming = StringTrimming.EllipsisCharacter });

                // 시간 표시 (블록 높이 > 40px일 때)
                if (rect.Height > 40)
                {
                    var timeText = $"{current.sched.StartTime}~{current.sched.EndTime}";
                    using var timeBrush = new SolidBrush(accentColor);
                    g.DrawString(timeText, timeFont, timeBrush,
                        new RectangleF(textX, textY + nameFont.Height + 1, textW, timeFont.Height + 2),
                        new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                }
            }
        }

        // 현재 시간 빨간 점선 표시
        var today = DateTime.Today;
        if (today >= _weekStart && today <= _weekEnd)
        {
            var now = DateTime.Now;
            var nowMinutes = now.Hour * 60 + now.Minute;
            // 자정 보정 (00:00~09:59 = +24h)
            if (nowMinutes < 600) nowMinutes += 1440;

            var startMinutes = 10 * 60; // 10:00
            var nowY = _headerHeight + (nowMinutes - startMinutes) / 30.0f * cellH;

            if (nowY > _headerHeight && nowY < Height)
            {
                using var timePen = new Pen(ColorPalette.Danger, 1.5f) { DashStyle = DashStyle.Dash };
                g.DrawLine(timePen, _timeColWidth, nowY, Width, nowY);

                // 좌측 빨간 점
                g.FillEllipse(Brushes.Red, _timeColWidth - 4, nowY - 3, 6, 6);
            }
        }
    }

    /// <summary>좌측만 라운드된 경로 생성 (악센트 바용)</summary>
    private static GraphicsPath CreateLeftRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);                       // 좌상
        path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);         // 우상→우하 (직선)
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);               // 좌하
        path.CloseFigure();

        return path;
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        var (dayIdx, slotIdx) = HitTest(e.Location);
        if (dayIdx < 0 || slotIdx < 0) return;

        var date = _weekStart.AddDays(dayIdx);
        var slot = TimeHelper.TimeSlots[slotIdx];

        // 해당 셀에 스케줄이 있으면 블록 클릭
        var hit = _schedules.FirstOrDefault(s =>
            s.WorkDate == date.ToString("yyyy-MM-dd") &&
            Array.IndexOf(TimeHelper.TimeSlots, s.StartTime) <= slotIdx &&
            Array.IndexOf(TimeHelper.TimeSlots, s.EndTime) > slotIdx);

        if (hit != null)
            BlockClicked?.Invoke(this, new ScheduleBlockClickEventArgs(hit));
        else
            EmptyCellDoubleClicked?.Invoke(this, new EmptyCellClickEventArgs(date, slot));
    }

    private (int dayIdx, int slotIdx) HitTest(Point pt)
    {
        var days = (int)(_weekEnd - _weekStart).TotalDays + 1;
        var cellW = (Width - _timeColWidth) / Math.Max(days, 1);
        var cellH = Math.Max(18, (Height - _headerHeight) / TimeHelper.TimeSlots.Length);

        if (pt.X < _timeColWidth || pt.Y < _headerHeight) return (-1, -1);

        var dayIdx = (pt.X - _timeColWidth) / cellW;
        var slotIdx = (pt.Y - _headerHeight) / cellH;

        if (dayIdx >= days || slotIdx >= TimeHelper.TimeSlots.Length) return (-1, -1);
        return (dayIdx, slotIdx);
    }
}

public class ScheduleBlockClickEventArgs(Schedule schedule) : EventArgs
{
    public Schedule Schedule { get; } = schedule;
}

public class EmptyCellClickEventArgs(DateTime date, string timeSlot) : EventArgs
{
    public DateTime Date { get; } = date;
    public string TimeSlot { get; } = timeSlot;
}
