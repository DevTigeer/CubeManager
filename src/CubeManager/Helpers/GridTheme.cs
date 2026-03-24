using System.Drawing;
using System.Drawing.Drawing2D;

namespace CubeManager.Helpers;

/// <summary>
/// 2025 DataGridView 테마 — #2D3047 기반.
/// 선택행: 연한 피치 배경 + 주황 테두리/바.
/// RowPrePaint/PostPaint로 잔상 없이 처리.
/// </summary>
public static class GridTheme
{
    // 선택행 배경
    private static readonly Color SelectRowBg = Color.FromArgb(250, 235, 220);
    private static readonly Color SelectRowAltBg = Color.FromArgb(245, 228, 210);

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

        // 데이터 행 — Selection 색상은 RowPrePaint에서 직접 처리
        grid.RowTemplate.Height = 44;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.TableBg,
            ForeColor = ColorPalette.TableText,
            Font = DesignTokens.FontBody,
            Padding = new Padding(12, 4, 12, 4),
            SelectionBackColor = SelectRowBg,
            SelectionForeColor = ColorPalette.TableText
        };

        // 교차행
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.RowAlt,
            ForeColor = ColorPalette.TableText,
            SelectionBackColor = SelectRowAltBg,
            SelectionForeColor = ColorPalette.TableText
        };

        // 1) 헤더 CellPainting: 선택행이 바로 아래일 때 헤더 텍스트 대비 보장
        grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex != -1) return; // 헤더만
            if (e.ColumnIndex < 0) return;

            e.PaintBackground(e.ClipBounds, false);

            // 헤더 텍스트: 항상 어두운 배경 위 밝은 글씨
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

        // 2) 선택 변경 시 이전+현재 행 모두 repaint (잔상 완전 제거)
        var prevSelectedRow = -1;
        grid.SelectionChanged += (_, _) =>
        {
            if (prevSelectedRow >= 0 && prevSelectedRow < grid.RowCount)
            {
                grid.InvalidateRow(prevSelectedRow);
            }
            var newRow = grid.CurrentRow?.Index ?? -1;
            if (newRow >= 0 && newRow < grid.RowCount)
            {
                grid.InvalidateRow(newRow);
            }
            prevSelectedRow = newRow;
        };

        // 3) 선택행: 좌측 주황 바 + 테두리 (PostPaint)
        grid.RowPostPaint += (_, e) =>
        {
            if (e.RowIndex < 0 || !grid.Rows[e.RowIndex].Selected) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = e.RowBounds;

            // 좌측 주황 악센트 바
            using var barBrush = new SolidBrush(ColorPalette.Accent);
            g.FillRectangle(barBrush, bounds.X, bounds.Y + 4, 3, bounds.Height - 8);

            // 테두리
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
