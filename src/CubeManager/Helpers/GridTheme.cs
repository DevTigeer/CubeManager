using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// 2025 Dark Tone DataGridView 테마.
/// 어두운 배경 + 밝은 텍스트 + 뉴모피즘 선택 바.
/// </summary>
public static class GridTheme
{
    public static void ApplyTheme(DataGridView grid)
    {
        // 기본
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

        // 헤더 — 가장 어두운 배경 + 밝은 텍스트
        grid.ColumnHeadersHeight = 40;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.HeaderBg,
            ForeColor = ColorPalette.TextSecondary,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(12, 0, 12, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        // 데이터 행 — 어두운 배경 + 밝은 텍스트
        grid.RowTemplate.Height = 44;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.Surface,
            ForeColor = ColorPalette.Text,
            Font = new Font("Segoe UI", 10f),
            Padding = new Padding(12, 4, 12, 4),
            SelectionBackColor = ColorPalette.SelectedBg,
            SelectionForeColor = Color.White
        };

        // 교차행 (미세하게 다른 어두운 톤)
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.RowAlt,
            ForeColor = ColorPalette.Text,
            SelectionBackColor = ColorPalette.SelectedBg,
            SelectionForeColor = Color.White
        };

        // 선택행 좌측 파란 바 (액센트 포인트)
        grid.RowPostPaint += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Selected)
            {
                using var brush = new SolidBrush(ColorPalette.Primary);
                e.Graphics.FillRectangle(brush,
                    e.RowBounds.X, e.RowBounds.Y + 6, 3, e.RowBounds.Height - 12);
            }
        };
    }

    /// <summary>금액 컬럼 — 오른쪽 정렬, Segoe UI</summary>
    public static DataGridViewCellStyle AmountStyle => new()
    {
        Alignment = DataGridViewContentAlignment.MiddleRight,
        Format = "N0",
        Font = new Font("Segoe UI", 10f)
    };

    /// <summary>가운데 정렬</summary>
    public static DataGridViewCellStyle CenterStyle => new()
    {
        Alignment = DataGridViewContentAlignment.MiddleCenter
    };

    /// <summary>수기 편집 표시</summary>
    public static void MarkManualEdit(DataGridViewCell cell)
    {
        cell.Style.BackColor = ColorPalette.ManualEdit;
    }
}
