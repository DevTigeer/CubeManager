using System.Drawing;
using System.Drawing.Drawing2D;

namespace CubeManager.Helpers;

/// <summary>
/// 2025 DataGridView 테마 — #2D3047 기반.
/// 선택행: 연한 밝은 주황 배경 + 좌측 바 + 테두리.
/// </summary>
public static class GridTheme
{
    // 선택행 배경: 보색(#F18A3D) 아주 연한 tint
    private static readonly Color SelectRowBg = Color.FromArgb(255, 250, 235, 220);  // 연한 피치
    private static readonly Color SelectRowAltBg = Color.FromArgb(255, 245, 228, 210); // 교차행용

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

        // 데이터 행
        grid.RowTemplate.Height = 44;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.TableBg,
            ForeColor = ColorPalette.TableText,
            Font = DesignTokens.FontBody,
            Padding = new Padding(12, 4, 12, 4),
            SelectionBackColor = SelectRowBg,           // 연한 피치
            SelectionForeColor = ColorPalette.TableText  // 글씨색 유지
        };

        // 교차행
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.RowAlt,
            ForeColor = ColorPalette.TableText,
            SelectionBackColor = SelectRowAltBg,        // 교차행 선택 시 약간 더 진한 피치
            SelectionForeColor = ColorPalette.TableText
        };

        // 선택 변경 시 이전 행 다시 그리기 (잔상 방지)
        var prevSelectedRow = -1;
        grid.SelectionChanged += (_, _) =>
        {
            // 이전 선택행 강제 repaint (테두리 잔상 제거)
            if (prevSelectedRow >= 0 && prevSelectedRow < grid.RowCount)
                grid.InvalidateRow(prevSelectedRow);

            prevSelectedRow = grid.CurrentRow?.Index ?? -1;
        };

        // 선택행: 좌측 주황 바 + 테두리
        grid.RowPostPaint += (_, e) =>
        {
            if (e.RowIndex < 0 || !grid.Rows[e.RowIndex].Selected) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = e.RowBounds;

            // 좌측 주황 악센트 바 (3px)
            using var barBrush = new SolidBrush(ColorPalette.Accent);
            g.FillRectangle(barBrush, bounds.X, bounds.Y + 4, 3, bounds.Height - 8);

            // 테두리 (연한 주황)
            using var borderPen = new Pen(Color.FromArgb(150, ColorPalette.Accent), 1.5f);
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
