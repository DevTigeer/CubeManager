using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Core.Helpers;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>주간 타임테이블 커스텀 GDI+ Panel — 2025 리디자인</summary>
public class TimeTablePanel : Panel
{
    private List<Schedule> _schedules = [];
    private DateTime _weekStart;
    private DateTime _weekEnd;
    private HashSet<string> _holidayDates = new();
    private const int HeaderHeight = 40;
    private const int TimeColWidth = 50;
    private const int CardGap = 3;       // 카드 간 간격
    private const int CardRadius = 6;    // 카드 라운드
    private const int AccentBarWidth = 4; // 좌측 컬러바 폭
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

    /// <summary>진한 악센트 색 (컬러바용)</summary>
    private static Color GetAccentColor(Color baseColor) => Color.FromArgb(
        Math.Max(baseColor.R - 80, 0),
        Math.Max(baseColor.G - 80, 0),
        Math.Max(baseColor.B - 80, 0));

    /// <summary>시간 단축 표시: "10:00" → "10", "10:30" → "10:30"</summary>
    private static string ShortTime(string time) =>
        time.EndsWith(":00") ? time[..^3] : time;

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

        // ──── Fonts ────
        using var headerDateFont = new Font("맑은 고딕", 9.5f, FontStyle.Bold);
        using var headerDayFont = new Font("맑은 고딕", 8f);
        using var timeAxisFont = new Font("맑은 고딕", 7.5f);
        using var cardNameFont = new Font("맑은 고딕", 8.5f, FontStyle.Bold);
        using var cardTimeFont = new Font("맑은 고딕", 7.5f);
        using var cardSmallFont = new Font("맑은 고딕", 7.5f, FontStyle.Bold);

        // ──── Brushes & Pens ────
        using var textBrush = new SolidBrush(ColorPalette.Text);
        using var subTextBrush = new SolidBrush(ColorPalette.TextTertiary);
        using var gridPen = new Pen(Color.FromArgb(40, ColorPalette.Border)) { Width = 0.5f };
        using var halfGridPen = new Pen(Color.FromArgb(20, ColorPalette.Border)) { DashStyle = DashStyle.Dot, Width = 0.5f };
        using var colDividerPen = new Pen(Color.FromArgb(50, ColorPalette.Border)) { Width = 0.5f };

        // ──── 1. 헤더 배경 ────
        using var headerBg = new SolidBrush(ColorPalette.Background);
        g.FillRectangle(headerBg, 0, 0, Width, HeaderHeight);

        // ──── 2. 오늘 열 강조 (헤더 포함) ────
        var todayDate = DateTime.Today;
        var todayDayIdx = -1;
        if (todayDate >= _weekStart && todayDate <= _weekEnd)
        {
            todayDayIdx = (int)(todayDate - _weekStart).TotalDays;
            var todayX = TimeColWidth + todayDayIdx * cellW;
            // 헤더 강조
            using var todayHeaderBg = new SolidBrush(Color.FromArgb(25, ColorPalette.Primary));
            g.FillRectangle(todayHeaderBg, todayX, 0, cellW, HeaderHeight);
            // 본문 강조
            using var todayBodyBg = new SolidBrush(Color.FromArgb(12, ColorPalette.Primary));
            g.FillRectangle(todayBodyBg, todayX, HeaderHeight, cellW, Height - HeaderHeight);
        }

        // ──── 3. 요일 헤더 렌더링 ────
        var dayNames = new[] { "일", "월", "화", "수", "목", "금", "토" };
        for (var d = 0; d < days; d++)
        {
            var date = _weekStart.AddDays(d);
            var x = TimeColWidth + d * cellW;
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isHoliday = _holidayDates.Contains(date.ToString("yyyy-MM-dd"));
            var isRedDay = isWeekend || isHoliday;
            var isToday = d == todayDayIdx;

            // 날짜 (상단): "3/9"
            var dateText = $"{date.Month}/{date.Day}";
            using var dateBrush = new SolidBrush(
                isToday ? ColorPalette.Primary :
                isRedDay ? ColorPalette.Danger : ColorPalette.Text);
            g.DrawString(dateText, headerDateFont, dateBrush,
                new RectangleF(x, 2, cellW, HeaderHeight / 2f),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            // 요일 (하단): "(월)"
            var dayText = dayNames[(int)date.DayOfWeek];
            using var dayBrush = new SolidBrush(
                isToday ? ColorPalette.Primary :
                isRedDay ? Color.FromArgb(180, ColorPalette.Danger) : ColorPalette.TextTertiary);
            g.DrawString(dayText, headerDayFont, dayBrush,
                new RectangleF(x, HeaderHeight / 2f, cellW, HeaderHeight / 2f),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            // 열 구분선
            if (d > 0)
                g.DrawLine(colDividerPen, x, 0, x, Height);
        }

        // 헤더 하단 구분선
        using var headerLine = new Pen(Color.FromArgb(60, ColorPalette.Border));
        g.DrawLine(headerLine, 0, HeaderHeight, Width, HeaderHeight);

        // ──── 4. 시간 라벨 + 격자 (연한 보조선) ────
        for (var r = 0; r < slots.Length; r++)
        {
            var y = HeaderHeight + r * cellH;
            // 정시=연한 실선, 30분=점선 (거의 안보이게)
            g.DrawLine(r % 2 == 0 ? gridPen : halfGridPen, TimeColWidth, y, Width, y);

            if (r % 2 == 0) // 정시만 라벨
            {
                g.DrawString(slots[r], timeAxisFont, subTextBrush,
                    new RectangleF(0, y - 1, TimeColWidth - 4, cellH + 2),
                    new StringFormat
                    {
                        Alignment = StringAlignment.Far,
                        LineAlignment = StringAlignment.Center
                    });
            }
        }

        // ──── 5. 스케줄 블록 렌더링 ────
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

                // 간격 포함 레이아웃
                var dayX = TimeColWidth + current.dayIdx * cellW;
                var totalW = cellW - CardGap * 2; // 좌우 마진
                var gapTotal = (overlapCount - 1) * CardGap;
                var blockW = (totalW - gapTotal) / overlapCount;
                var x = dayX + CardGap + overlapIndex * (blockW + CardGap);
                var y = HeaderHeight + current.startSlot * cellH + 2;
                var h = (current.endSlot - current.startSlot) * cellH - 4;

                var empColor = GetEmployeeColor(current.sched.EmployeeId);
                var accentColor = GetAccentColor(empColor);
                var rect = new Rectangle(x, y, Math.Max(blockW, 20), Math.Max(h, cellH));

                // ── 카드 배경: 흰색 + 미세한 테두리 ──
                using var cardPath = RoundedCard.CreateRoundedPath(rect, CardRadius);
                using var cardFill = new SolidBrush(ColorPalette.Surface);
                g.FillPath(cardFill, cardPath);

                // 아주 연한 employee 색상 워시
                using var colorWash = new SolidBrush(Color.FromArgb(25, empColor));
                g.FillPath(colorWash, cardPath);

                // 카드 테두리 (미세)
                using var cardBorderPen = new Pen(Color.FromArgb(50, empColor), 0.5f);
                g.DrawPath(cardBorderPen, cardPath);

                // ── 좌측 컬러바 (4px, 직원 식별용) ──
                var barRect = new Rectangle(rect.X, rect.Y, AccentBarWidth, rect.Height);
                using var barPath = CreateLeftRoundedPath(barRect, CardRadius);
                using var barBrush = new SolidBrush(accentColor);
                g.FillPath(barBrush, barPath);

                // ── 텍스트 영역 ──
                var name = current.sched.EmployeeName ?? $"ID:{current.sched.EmployeeId}";
                var innerX = rect.X + AccentBarWidth + 4;
                var innerW = rect.Width - AccentBarWidth - 8;
                var innerY = rect.Y + 4;

                if (blockW >= 65)
                {
                    // 넓은 블록: 이름 + 시간 2줄
                    using var nameClr = new SolidBrush(ColorPalette.Text);
                    g.DrawString(name, cardNameFont, nameClr,
                        new RectangleF(innerX, innerY, innerW, cardNameFont.Height + 2),
                        new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

                    if (rect.Height > 36)
                    {
                        var timeStr = $"{ShortTime(current.sched.StartTime)}-{ShortTime(current.sched.EndTime)}";
                        using var timeClr = new SolidBrush(ColorPalette.TextSecondary);
                        g.DrawString(timeStr, cardTimeFont, timeClr,
                            new RectangleF(innerX, innerY + cardNameFont.Height + 1, innerW, cardTimeFont.Height + 2),
                            new StringFormat { FormatFlags = StringFormatFlags.NoWrap });
                    }
                }
                else if (blockW >= 40)
                {
                    // 중간 블록: 축약 이름 + 시간
                    var shortName = name.Length > 4 ? name[..4] : name;
                    using var nameClr = new SolidBrush(ColorPalette.Text);
                    g.DrawString(shortName, cardSmallFont, nameClr,
                        new RectangleF(innerX, innerY, innerW, cardSmallFont.Height + 2),
                        new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

                    if (rect.Height > 32)
                    {
                        var timeStr = $"{ShortTime(current.sched.StartTime)}-{ShortTime(current.sched.EndTime)}";
                        using var timeClr = new SolidBrush(ColorPalette.TextSecondary);
                        g.DrawString(timeStr, cardTimeFont, timeClr,
                            new RectangleF(innerX, innerY + cardSmallFont.Height, innerW, cardTimeFont.Height + 2),
                            new StringFormat { FormatFlags = StringFormatFlags.NoWrap });
                    }
                }
                else
                {
                    // 좁은 블록: 이니셜 2자 (세로 중앙)
                    var initials = name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
                    using var nameClr = new SolidBrush(accentColor);
                    g.DrawString(initials, cardSmallFont, nameClr,
                        new RectangleF(innerX, rect.Y, innerW, rect.Height),
                        new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.NoWrap
                        });
                }
            }
        }

        // ──── 6. 현재 시간 표시선 ────
        if (todayDate >= _weekStart && todayDate <= _weekEnd)
        {
            var now = DateTime.Now;
            var nowMinutes = now.Hour * 60 + now.Minute;
            if (nowMinutes < 600) nowMinutes += 1440; // 자정 보정

            var startMinutes = 10 * 60;
            var nowY = HeaderHeight + (nowMinutes - startMinutes) / 30.0f * cellH;

            if (nowY > HeaderHeight && nowY < Height)
            {
                using var timePen = new Pen(ColorPalette.Danger, 1.5f);
                g.DrawLine(timePen, TimeColWidth, nowY, Width, nowY);

                // 좌측 원형 인디케이터
                var dotSize = 8;
                g.FillEllipse(Brushes.Red, TimeColWidth - dotSize / 2, nowY - dotSize / 2, dotSize, dotSize);

                // 현재 시각 라벨
                var nowLabel = now.ToString("HH:mm");
                using var nowFont = new Font("맑은 고딕", 7f, FontStyle.Bold);
                using var nowBg = new SolidBrush(ColorPalette.Danger);
                using var nowFg = new SolidBrush(Color.White);
                var labelSize = g.MeasureString(nowLabel, nowFont);
                var labelRect = new RectangleF(2, nowY - labelSize.Height / 2, labelSize.Width + 4, labelSize.Height);
                using var labelPath = RoundedCard.CreateRoundedPath(
                    Rectangle.Round(labelRect), 3);
                g.FillPath(nowBg, labelPath);
                g.DrawString(nowLabel, nowFont, nowFg, labelRect.X + 2, labelRect.Y);
            }
        }
    }

    /// <summary>좌측만 라운드된 경로 생성 (컬러바용)</summary>
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
