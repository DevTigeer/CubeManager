using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// 2025 DataGridView 테마 — #2D3047 기반.
/// 헤더: 주색 어두운 배경 + 밝은 텍스트
/// 데이터: 밝은 #F0F0F0 배경 + 어두운 텍스트 (배경과 강한 대비)
/// 모든 폰트 Bold.
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

        // 헤더 — 어두운 배경 + 밝은 텍스트 (Bold)
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

        // 데이터 행 — 밝은 배경 + 어두운 텍스트 (대비 강조, Bold)
        grid.RowTemplate.Height = 44;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.TableBg,
            ForeColor = ColorPalette.TableText,
            Font = DesignTokens.FontBody,
            Padding = new Padding(12, 4, 12, 4),
            SelectionBackColor = ColorPalette.SelectedBg,
            SelectionForeColor = Color.White
        };

        // 교차행 (밝은 회색 변형)
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.RowAlt,
            ForeColor = ColorPalette.TableText,
            SelectionBackColor = ColorPalette.SelectedBg,
            SelectionForeColor = Color.White
        };

        // 선택행 좌측 주황 바 (보색 포인트)
        grid.RowPostPaint += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Selected)
            {
                using var brush = new SolidBrush(ColorPalette.Accent);
                e.Graphics.FillRectangle(brush,
                    e.RowBounds.X, e.RowBounds.Y + 6, 3, e.RowBounds.Height - 12);
            }
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
