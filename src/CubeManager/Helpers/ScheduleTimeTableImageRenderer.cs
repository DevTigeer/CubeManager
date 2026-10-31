using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using CubeManager.Controls;
using CubeManager.Core.Helpers;
using CubeManager.Telegram.Imaging;

namespace CubeManager.Helpers;

/// <summary>
/// 텔레그램 스케줄 이미지 — 스케줄탭 화면(상단 날짜 + TimeTablePanel + 직원별 주간 요약)을 PNG로 렌더링.
/// 컨트롤 핸들 없이 TimeTablePanel.RenderTo 로 Bitmap 에 직접 그리므로 봇 스레드에서 호출 가능.
/// </summary>
public sealed class ScheduleTimeTableImageRenderer : IScheduleTimeTableRenderer
{
    private const int ImageWidth = 1400;     // 요일 칸 ~189px — 겹치는 이름도 잘리지 않도록 넓게
    private const int Pad = 12;              // 스케줄탭 Padding
    private const int TopBarHeight = 50;     // 스케줄탭 상단 헤더
    private const int TableHeaderHeight = 42; // TimeTablePanel.HeaderHeight
    private const int CellHeight = 22;
    private const int ChipWidth = 180;
    private const int ChipHeight = 36;
    private const int ChipGap = 8;
    private const int SummaryTopPadding = 6;
    private const float Scale = 2f;          // 고해상도 (2800px) — 텔레그램엔 압축 없는 파일로 전송

    public IReadOnlyList<Stream> RenderWeeks(IReadOnlyList<ScheduleWeekImage> weeks)
    {
        // 패널 하나를 재사용 → 직원 색상이 모든 주차 이미지에서 동일
        using var panel = new TimeTablePanel();
        var result = new List<Stream>(weeks.Count);
        foreach (var week in weeks)
            result.Add(RenderWeek(panel, week));
        return result;
    }

    private static Stream RenderWeek(TimeTablePanel panel, ScheduleWeekImage week)
    {
        var empGroups = week.Schedules
            .GroupBy(s => new { s.EmployeeId, s.EmployeeName })
            .OrderBy(g => g.Key.EmployeeName)
            .ToList();

        var contentWidth = ImageWidth - Pad * 2;
        var chipsPerRow = Math.Max(1, (contentWidth + ChipGap) / (ChipWidth + ChipGap));
        var chipRows = Math.Max(1, (empGroups.Count + chipsPerRow - 1) / chipsPerRow);

        var tableHeight = TableHeaderHeight + TimeHelper.TimeSlots.Length * CellHeight;
        var summaryHeight = SummaryTopPadding + chipRows * ChipHeight;
        var imageHeight = Pad + TopBarHeight + tableHeight + summaryHeight + Pad;

        using var bmp = new Bitmap((int)(ImageWidth * Scale), (int)(imageHeight * Scale), PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.ScaleTransform(Scale, Scale);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(ColorPalette.Surface);

        // ── 상단 헤더: 날짜 범위 + 주차 (스케줄탭 _lblDateRange / _lblWeekSub) ──
        using (var dateFont = new Font("맑은 고딕", 13f, FontStyle.Bold))
        using (var subFont = new Font("맑은 고딕", 8.5f))
        using (var textBrush = new SolidBrush(ColorPalette.Text))
        using (var subBrush = new SolidBrush(ColorPalette.TextTertiary))
        {
            g.DrawString($"{week.WeekStart:yyyy.MM.dd} - {week.WeekEnd:MM.dd}", dateFont, textBrush, Pad, Pad + 4);
            g.DrawString(week.Label, subFont, subBrush, Pad + 2, Pad + 30);

            var stamp = $"조회 {DateTime.Now:yyyy-MM-dd HH:mm}";
            var stampSize = g.MeasureString(stamp, subFont);
            g.DrawString(stamp, subFont, subBrush, ImageWidth - Pad - stampSize.Width, Pad + 30);
        }

        // ── 타임테이블 ──
        var tableTop = Pad + TopBarHeight;
        panel.SetData(week.Schedules, week.WeekStart, week.WeekEnd, week.HolidayDates);
        var state = g.Save();
        g.TranslateTransform(Pad, tableTop);
        g.SetClip(new Rectangle(0, 0, contentWidth, tableHeight));
        panel.RenderTo(g, contentWidth, tableHeight);
        g.Restore(state);

        // ── 하단: 직원별 주간 요약 칩 (스케줄탭 RenderWeeklySummary) ──
        using (var chipFont = new Font("맑은 고딕", 8.5f))
        using (var chipBrush = new SolidBrush(ColorPalette.Text))
        {
            var summaryTop = tableTop + tableHeight + SummaryTopPadding;
            var chipFormat = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
            for (var i = 0; i < empGroups.Count; i++)
            {
                var grp = empGroups[i];
                var x = Pad + (i % chipsPerRow) * (ChipWidth + ChipGap);
                var y = summaryTop + (i / chipsPerRow) * ChipHeight;

                var name = grp.Key.EmployeeName ?? $"ID:{grp.Key.EmployeeId}";
                var totalHours = grp.Sum(s => TimeHelper.CalcHours(s.StartTime, s.EndTime));
                var hoursStr = totalHours % 1 == 0 ? $"{(int)totalHours}h" : $"{totalHours:F1}h";

                using var dotBrush = new SolidBrush(ColorPalette.GetEmployeeColor(
                    grp.Key.EmployeeId % ColorPalette.EmployeeColors.Length));
                g.FillEllipse(dotBrush, x + 4, y + 13, 9, 9);

                g.DrawString($"{name}  {hoursStr}", chipFont, chipBrush,
                    new RectangleF(x + 18, y, ChipWidth - 22, ChipHeight), chipFormat);
            }
        }

        var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        return ms;
    }
}
