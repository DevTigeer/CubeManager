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
    private readonly TimeTablePainter _painter = new();
    private const int HeaderHeight = TimeTablePainter.HeaderHeight;
    private const int TimeColWidth = TimeTablePainter.TimeColWidth;

    public event EventHandler<ScheduleBlockClickEventArgs>? BlockClicked;
    public event EventHandler<ScheduleBlockClickEventArgs>? BlockEditRequested;
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
        _painter.SetData(_schedules, weekStart, weekEnd, holidayDates);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        _painter.Render(e.Graphics, Width, Height);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Right) return;

        var (dayIdx, slotIdx) = HitTest(e.Location);
        if (dayIdx < 0 || slotIdx < 0) return;

        var date = _weekStart.AddDays(dayIdx);
        var hit = _schedules.FirstOrDefault(s =>
            s.WorkDate == date.ToString("yyyy-MM-dd") &&
            Array.IndexOf(TimeHelper.TimeSlots, s.StartTime) <= slotIdx &&
            Array.IndexOf(TimeHelper.TimeSlots, s.EndTime) > slotIdx);

        if (hit == null) return;

        var menu = new ContextMenuStrip();
        menu.BackColor = ColorPalette.Card;
        menu.ForeColor = ColorPalette.Text;
        menu.Font = DesignTokens.FontBody;

        var editItem = new ToolStripMenuItem("직원 변경");
        editItem.Click += (_, _) => BlockEditRequested?.Invoke(this, new ScheduleBlockClickEventArgs(hit));
        menu.Items.Add(editItem);

        var deleteItem = new ToolStripMenuItem("삭제");
        deleteItem.ForeColor = ColorPalette.Danger;
        deleteItem.Click += (_, _) => BlockClicked?.Invoke(this, new ScheduleBlockClickEventArgs(hit));
        menu.Items.Add(deleteItem);

        menu.Show(this, e.Location);
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
