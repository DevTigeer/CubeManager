using System.Drawing;
using System.Drawing.Drawing2D;

namespace CubeManager.Helpers;

/// <summary>
/// 2025 DataGridView 테마 — #2D3047 기반.
/// 선택행: RowPrePaint/PostPaint에서 직접 배경+테두리 처리 (잔상 없음).
/// </summary>
public static class GridTheme
{
    private static readonly Color SelectRowBg = Color.FromArgb(250, 235, 220);

    // 캐싱된 스타일
    private static readonly DataGridViewCellStyle _amountStyle = new()
    {
        Alignment = DataGridViewContentAlignment.MiddleRight,
        Format = "N0",
        Font = new Font("Segoe UI", 10f, FontStyle.Bold)
    };

    public static void ApplyTheme(DataGridView grid)
    {
        // 이벤트 핸들러 중복 방지
        if (grid.Tag as string == "__gridThemed") return;
        grid.Tag = "__gridThemed";

        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = ColorPalette.Surface;
        grid.GridColor = ColorPalette.Border;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.DoubleBuffered(true); // 깜빡임 방지

        // 헤더
        grid.ColumnHeadersHeight = 40;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.HeaderBg,
            ForeColor = ColorPalette.TableHeaderText,
            Font = DesignTokens.FontTabMenu,
            Padding = new Padding(12, 0, 12, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        // 데이터 행 — SelectionBackColor를 일반과 동일하게 (자체 처리)
        grid.RowTemplate.Height = 44;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.TableBg,
            ForeColor = ColorPalette.TableText,
            Font = DesignTokens.FontBody,
            Padding = new Padding(12, 4, 12, 4),
            SelectionBackColor = ColorPalette.TableBg,
            SelectionForeColor = ColorPalette.TableText
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.RowAlt,
            ForeColor = ColorPalette.TableText,
            SelectionBackColor = ColorPalette.RowAlt,
            SelectionForeColor = ColorPalette.TableText
        };

        // 헤더 OwnerDraw (항상 밝은 텍스트 보장)
        grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex != -1 || e.ColumnIndex < 0) return;
            e.PaintBackground(e.ClipBounds, false);

            using var brush = new SolidBrush(ColorPalette.TableHeaderText);
            using var font = DesignTokens.FontTabMenu;
            var textRect = new Rectangle(
                e.CellBounds.X + 12, e.CellBounds.Y,
                e.CellBounds.Width - 24, e.CellBounds.Height);
            var sf = new StringFormat
            {
                Alignment = e.CellStyle?.Alignment switch
                {
                    DataGridViewContentAlignment.MiddleRight => StringAlignment.Far,
                    DataGridViewContentAlignment.MiddleCenter => StringAlignment.Center,
                    _ => StringAlignment.Near
                },
                LineAlignment = StringAlignment.Center
            };
            e.Graphics!.DrawString(e.FormattedValue?.ToString() ?? "", font, brush, textRect, sf);
            e.Handled = true;
        };

        // RowPrePaint: 선택행 배경을 직접 칠함 (잔상 근본 해결)
        grid.RowPrePaint += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];

            if (row.Selected)
            {
                // 선택행: 피치 배경으로 전체 행 덮기
                using var brush = new SolidBrush(SelectRowBg);
                e.Graphics.FillRectangle(brush, e.RowBounds);
                e.PaintParts &= ~DataGridViewPaintParts.Background; // 기본 배경 그리기 스킵
            }
        };

        // RowPostPaint: 선택행 테두리 + 악센트 바
        grid.RowPostPaint += (_, e) =>
        {
            if (e.RowIndex < 0 || !grid.Rows[e.RowIndex].Selected) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = e.RowBounds;

            // 좌측 주황 바
            using var barBrush = new SolidBrush(ColorPalette.Accent);
            g.FillRectangle(barBrush, bounds.X, bounds.Y + 4, 3, bounds.Height - 8);

            // 테두리
            using var borderPen = new Pen(Color.FromArgb(150, ColorPalette.Accent), 1.5f);
            g.DrawRectangle(borderPen, bounds.X + 1, bounds.Y, bounds.Width - 3, bounds.Height - 1);
        };

        // 선택 변경 시 이전 행 repaint
        var prevRow = -1;
        grid.SelectionChanged += (_, _) =>
        {
            if (prevRow >= 0 && prevRow < grid.RowCount)
                grid.InvalidateRow(prevRow);
            prevRow = grid.CurrentRow?.Index ?? -1;
        };
    }

    public static DataGridViewCellStyle AmountStyle => _amountStyle;

    public static DataGridViewCellStyle CenterStyle => new()
    {
        Alignment = DataGridViewContentAlignment.MiddleCenter
    };

    public static void MarkManualEdit(DataGridViewCell cell)
    {
        cell.Style.BackColor = ColorPalette.ManualEdit;
    }
}

/// <summary>DataGridView DoubleBuffered 확장</summary>
internal static class DataGridViewExtensions
{
    public static void DoubleBuffered(this DataGridView dgv, bool setting)
    {
        var type = dgv.GetType();
        var prop = type.GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        prop?.SetValue(dgv, setting);
    }
}
