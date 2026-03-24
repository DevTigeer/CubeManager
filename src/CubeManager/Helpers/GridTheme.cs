using System.Drawing;
using System.Drawing.Drawing2D;

namespace CubeManager.Helpers;

/// <summary>
/// 2025 DataGridView 테마 — #2D3047 기반.
/// 선택행: 배경 변경 없이 테두리만 표시 (테마 보색 #F18A3D).
/// </summary>
public static class GridTheme
{
    public static void ApplyTheme(DataGridView grid)
    {
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

        // 데이터 행 — 선택 시에도 배경/글씨 동일 (테두리로 구분)
        grid.RowTemplate.Height = 44;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.TableBg,
            ForeColor = ColorPalette.TableText,
            Font = DesignTokens.FontBody,
            Padding = new Padding(12, 4, 12, 4),
            SelectionBackColor = ColorPalette.TableBg,      // 선택해도 배경 그대로
            SelectionForeColor = ColorPalette.TableText      // 선택해도 글씨 그대로
        };

        // 교차행
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.RowAlt,
            ForeColor = ColorPalette.TableText,
            SelectionBackColor = ColorPalette.RowAlt,        // 선택해도 배경 그대로
            SelectionForeColor = ColorPalette.TableText
        };

        // 선택행: 좌측 주황 바 + 테두리 (RowPostPaint)
        grid.RowPostPaint += (_, e) =>
        {
            if (e.RowIndex < 0 || !grid.Rows[e.RowIndex].Selected) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = e.RowBounds;

            // 좌측 주황 악센트 바 (3px)
            using var barBrush = new SolidBrush(ColorPalette.Accent);
            g.FillRectangle(barBrush, bounds.X, bounds.Y + 4, 3, bounds.Height - 8);

            // 행 전체 테두리 (연한 주황, 1px)
            using var borderPen = new Pen(Color.FromArgb(120, ColorPalette.Accent), 1.5f);
            var borderRect = new Rectangle(bounds.X + 1, bounds.Y, bounds.Width - 3, bounds.Height - 1);
            g.DrawRectangle(borderPen, borderRect);
        };
    }

    /// <summary>금액 컬럼 — Bold, 오른쪽 정렬</summary>
    public static DataGridViewCellStyle AmountStyle => new()
    {
        Alignment = DataGridViewContentAlignment.MiddleRight,
        Format = "N0",
        Font = new Font("Segoe UI", 10f, FontStyle.Bold)
    };

    public static DataGridViewCellStyle CenterStyle => new()
    {
        Alignment = DataGridViewContentAlignment.MiddleCenter
    };

    public static void MarkManualEdit(DataGridViewCell cell)
    {
        cell.Style.BackColor = ColorPalette.ManualEdit;
    }
}
