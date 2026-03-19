using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// DataGridView 공통 테마. 모든 탭에서 ApplyTheme()으로 일관된 스타일 적용.
/// </summary>
public static class GridTheme
{
    /// <summary>디자인 시스템 기반 DataGridView 테마 일괄 적용</summary>
    public static void ApplyTheme(DataGridView grid)
    {
        // 기본 설정
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = ColorPalette.Surface;
        grid.GridColor = ColorPalette.Divider;
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

        // 헤더 스타일
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.HeaderBg,
            ForeColor = ColorPalette.TextSecondary,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(8, 0, 8, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        // 데이터 행 스타일
        grid.RowTemplate.Height = 40;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.Surface,
            ForeColor = ColorPalette.Text,
            Font = new Font("맑은 고딕", 10f),
            Padding = new Padding(8, 4, 8, 4),
            SelectionBackColor = ColorPalette.SelectedBg,
            SelectionForeColor = ColorPalette.Text
        };

        // 교차 행 배경
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.RowAlt
        };
    }

    /// <summary>금액 컬럼용 오른쪽 정렬 스타일</summary>
    public static DataGridViewCellStyle AmountStyle => new()
    {
        Alignment = DataGridViewContentAlignment.MiddleRight,
        Format = "N0"
    };

    /// <summary>가운데 정렬 스타일</summary>
    public static DataGridViewCellStyle CenterStyle => new()
    {
        Alignment = DataGridViewContentAlignment.MiddleCenter
    };

    /// <summary>수기 수정 셀 배경</summary>
    public static void MarkManualEdit(DataGridViewCell cell)
    {
        cell.Style.BackColor = ColorPalette.ManualEdit;
    }
}
