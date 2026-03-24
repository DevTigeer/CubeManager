using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Core.Helpers;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>
/// 주간 타임테이블 — #2D3047 색상 가이드 적용.
/// 배경: MainDark(#1E2335), 헤더: Main(#2D3047), 표 영역: TableBg(#F0F0F0)
/// 블록: 직원별 색상 + 보색(#F18A3D) 선택 바
/// 모든 폰트 Bold.
/// </summary>
public class TimeTablePanel : Panel
{
    private List<Schedule> _schedules = [];
    private DateTime _weekStart;
    private DateTime _weekEnd;
    private HashSet<string> _holidayDates = new();
    private const int HeaderHeight = 42;
    private const int TimeColWidth = 52;
    private const int CardGap = 3;
    private const int CardRadius = 6;
    private const int AccentBarWidth = 4;
    private readonly Dictionary<int, int> _employeeColorIndex = new();
    private int _nextColorIndex;

    // 캐싱된 폰트 (OnPaint에서 매번 생성 방지)
    private static readonly Font _timeAxisFont = new("맑은 고딕", 7.5f, FontStyle.Bold);
    private static readonly Font _cardNameFont = new("맑은 고딕", 10f, FontStyle.Bold);
    private static readonly Font _cardSmallFont = new("맑은 고딕", 8.5f, FontStyle.Bold);
    private static readonly Font _nowLabelFont = new("맑은 고딕", 7f, FontStyle.Bold);

    public event EventHandler<ScheduleBlockClickEventArgs>? BlockClicked;
    public event EventHandler<EmptyCellClickEventArgs>? EmptyCellDoubleClicked;

    public TimeTablePanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        BackColor = ColorPalette.Surface;
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

    private static Color DarkenColor(Color c, int amount) => Color.FromArgb(
        c.A, Math.Max(c.R - amount, 0), Math.Max(c.G - amount, 0), Math.Max(c.B - amount, 0));

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var slots = TimeHelper.TimeSlots;
        var days = (int)(_weekEnd - _weekStart).TotalDays + 1;
        if (days <= 0) return;

        var cellW = (Width - TimeColWidth) / days;
        var cellH = Math.Max(18, (Height - HeaderHeight) / slots.Length);

        // ──── Fonts (캐싱, dispose 금지) ────
        var headerDateFont = DesignTokens.FontTabMenu;
        var headerDayFont = DesignTokens.FontCaption;
        var timeAxisFont = _timeAxisFont;
        var cardNameFont = _cardNameFont;
        var cardSmallFont = _cardSmallFont;

        // ──── 1. 전체 배경: MainDark ────
        using var bgBrush = new SolidBrush(ColorPalette.Background);
        g.FillRectangle(bgBrush, 0, 0, Width, Height);

        // ──── 2. 표 영역 배경: TableBg (밝은 회색, 대비) ────
        using var tableBg = new SolidBrush(ColorPalette.TableBg);
        g.FillRectangle(tableBg, TimeColWidth, HeaderHeight, Width - TimeColWidth, Height - HeaderHeight);

        // ──── 3. 헤더 배경: Main (#2D3047) ────
        using var headerBg = new SolidBrush(ColorPalette.Main);
        g.FillRectangle(headerBg, 0, 0, Width, HeaderHeight);

        // 시간 열 배경: Main
        g.FillRectangle(headerBg, 0, HeaderHeight, TimeColWidth, Height - HeaderHeight);

        // ──── 4. 오늘 열 강조 ────
        var todayDate = DateTime.Today;
        var todayDayIdx = -1;
        if (todayDate >= _weekStart && todayDate <= _weekEnd)
        {
            todayDayIdx = (int)(todayDate - _weekStart).TotalDays;
            var todayX = TimeColWidth + todayDayIdx * cellW;
            // 헤더: 청록 tint
            using var todayHdrBg = new SolidBrush(Color.FromArgb(40, ColorPalette.Primary));
            g.FillRectangle(todayHdrBg, todayX, 0, cellW, HeaderHeight);
            // 본문: 연한 청록 tint
            using var todayBodyBg = new SolidBrush(Color.FromArgb(15, ColorPalette.Primary));
            g.FillRectangle(todayBodyBg, todayX, HeaderHeight, cellW, Height - HeaderHeight);
        }

        // ──── 5. 요일 헤더 ────
        var dayNames = new[] { "일", "월", "화", "수", "목", "금", "토" };
        for (var d = 0; d < days; d++)
        {
            var date = _weekStart.AddDays(d);
            var x = TimeColWidth + d * cellW;
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isHoliday = _holidayDates.Contains(date.ToString("yyyy-MM-dd"));
            var isRedDay = isWeekend || isHoliday;
            var isToday = d == todayDayIdx;

            // 날짜
            var dateText = $"{date.Month}/{date.Day}";
            using var dateBrush = new SolidBrush(
                isToday ? ColorPalette.Accent :     // 오늘 = 주황(보색)
                isRedDay ? ColorPalette.Danger : ColorPalette.Text);
            g.DrawString(dateText, headerDateFont, dateBrush,
                new RectangleF(x, 2, cellW, HeaderHeight / 2f),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            // 요일
            using var dayBrush = new SolidBrush(
                isToday ? Color.FromArgb(200, ColorPalette.Accent) :
                isRedDay ? Color.FromArgb(180, ColorPalette.Danger) : ColorPalette.TextTertiary);
            g.DrawString(dayNames[(int)date.DayOfWeek], headerDayFont, dayBrush,
                new RectangleF(x, HeaderHeight / 2f, cellW, HeaderHeight / 2f),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            // 열 구분선
            if (d > 0)
            {
                using var divPen = new Pen(Color.FromArgb(30, ColorPalette.Border));
                g.DrawLine(divPen, x, HeaderHeight, x, Height);
            }
        }

        // 헤더 하단 구분선
        using var hdrLine = new Pen(ColorPalette.Border);
        g.DrawLine(hdrLine, 0, HeaderHeight, Width, HeaderHeight);

        // ──── 6. 시간 라벨 + 격자 ────
        using var gridPen = new Pen(Color.FromArgb(25, 0, 0, 0));           // 밝은 배경 위 연한 선
        using var halfPen = new Pen(Color.FromArgb(10, 0, 0, 0)) { DashStyle = DashStyle.Dot };
        using var timeTextBrush = new SolidBrush(ColorPalette.TextTertiary);
        for (var r = 0; r < slots.Length; r++)
        {
            var y = HeaderHeight + r * cellH;
            g.DrawLine(r % 2 == 0 ? gridPen : halfPen, TimeColWidth, y, Width, y);

            if (r % 2 == 0)
            {
                g.DrawString(slots[r], timeAxisFont, timeTextBrush,
                    new RectangleF(0, y - 1, TimeColWidth - 4, cellH + 2),
                    new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
            }
        }

        // ──── 7. 스케줄 블록 ────
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

        var dayGroups = blockList.GroupBy(b => b.dayIdx);
        foreach (var dayGroup in dayGroups)
        {
            var dayBlocks = dayGroup.OrderBy(b => b.startSlot).ToList();

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

                var dayX = TimeColWidth + current.dayIdx * cellW;
                var totalW = cellW - CardGap * 2;
                var gapTotal = (overlapCount - 1) * CardGap;
                var blockW = (totalW - gapTotal) / overlapCount;
                var x = dayX + CardGap + overlapIndex * (blockW + CardGap);
                var y = HeaderHeight + current.startSlot * cellH + 2;
                var h = (current.endSlot - current.startSlot) * cellH - 4;

                var empColor = GetEmployeeColor(current.sched.EmployeeId);
                var rect = new Rectangle(x, y, Math.Max(blockW, 20), Math.Max(h, cellH));

                // ── 카드 배경: 직원 색상 그대로 (중채도 파스텔) ──
                using var cardPath = RoundedCard.CreateRoundedPath(rect, CardRadius);
                using var cardFill = new SolidBrush(Color.FromArgb(180, empColor));
                g.FillPath(cardFill, cardPath);

                // 카드 테두리: 약간 어둡게
                using var cardBorder = new Pen(Color.FromArgb(200, DarkenColor(empColor, 40)), 1f);
                g.DrawPath(cardBorder, cardPath);

                // 좌측 컬러바: 진한 버전
                var barRect = new Rectangle(rect.X, rect.Y, AccentBarWidth, rect.Height);
                using var barPath = CreateLeftRoundedPath(barRect, CardRadius);
                using var barBrush = new SolidBrush(DarkenColor(empColor, 60));
                g.FillPath(barBrush, barPath);

                // ── 이름: 흰색 텍스트 (중채도 배경 위 고대비) ──
                var name = current.sched.EmployeeName ?? $"ID:{current.sched.EmployeeId}";
                var innerX = rect.X + AccentBarWidth + 2;
                var innerW = rect.Width - AccentBarWidth - 4;
                var innerH = rect.Height - 4;

                var nameFont = blockW >= 50 ? cardNameFont : cardSmallFont;
                using var nameClr = new SolidBrush(Color.White);

                var textRect = new RectangleF(innerX, rect.Y + 2, innerW, innerH);
                g.DrawString(name, nameFont, nameClr, textRect,
                    new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter
                    });
            }
        }

        // ──── 8. 현재 시간선 (주황 보색) ────
        if (todayDate >= _weekStart && todayDate <= _weekEnd)
        {
            var now = DateTime.Now;
            var nowMinutes = now.Hour * 60 + now.Minute;
            if (nowMinutes < 600) nowMinutes += 1440;

            var startMinutes = 10 * 60;
            var nowY = HeaderHeight + (nowMinutes - startMinutes) / 30.0f * cellH;

            if (nowY > HeaderHeight && nowY < Height)
            {
                // 주황 시간선 (보색)
                using var timePen = new Pen(ColorPalette.Accent, 2f);
                g.DrawLine(timePen, TimeColWidth, nowY, Width, nowY);

                // 좌측 원형
                var dotSize = 8;
                using var dotBrush = new SolidBrush(ColorPalette.Accent);
                g.FillEllipse(dotBrush, TimeColWidth - dotSize / 2, nowY - dotSize / 2, dotSize, dotSize);

                // 시각 라벨 (주황 배경)
                var nowLabel = now.ToString("HH:mm");
                var nowFont = _nowLabelFont;
                using var nowBgBrush = new SolidBrush(ColorPalette.Accent);
                using var nowFgBrush = new SolidBrush(Color.White);
                var labelSize = g.MeasureString(nowLabel, nowFont);
                var labelRect = new RectangleF(2, nowY - labelSize.Height / 2, labelSize.Width + 6, labelSize.Height + 2);
                using var labelPath = RoundedCard.CreateRoundedPath(Rectangle.Round(labelRect), 3);
                g.FillPath(nowBgBrush, labelPath);
                g.DrawString(nowLabel, nowFont, nowFgBrush, labelRect.X + 3, labelRect.Y + 1);
            }
        }
    }

    private static GraphicsPath CreateLeftRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
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
        var cellW = (Width - TimeColWidth) / Math.Max(days, 1);
        var cellH = Math.Max(18, (Height - HeaderHeight) / TimeHelper.TimeSlots.Length);

        if (pt.X < TimeColWidth || pt.Y < HeaderHeight) return (-1, -1);

        var dayIdx = (pt.X - TimeColWidth) / cellW;
        var slotIdx = (pt.Y - HeaderHeight) / cellH;

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
