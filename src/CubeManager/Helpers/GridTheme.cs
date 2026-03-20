using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// DataGridView 공통 테마.
/// 2025 업데이트: 가독성 향상, 행 간격 최적화, 선택 행 인디케이터.
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

        // 헤더 스타일 — 더 진한 배경, 약간 작은 폰트로 데이터와 구분
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.HeaderBg,
            ForeColor = ColorPalette.TextSecondary,
            Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        // 데이터 행 스타일 — 넉넉한 높이로 가독성 확보
        grid.RowTemplate.Height = 42;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.Surface,
            ForeColor = ColorPalette.Text,
            Font = new Font("맑은 고딕", 10f),
            Padding = new Padding(10, 4, 10, 4),
            SelectionBackColor = ColorPalette.SelectedBg,
            SelectionForeColor = ColorPalette.Text
        };

        // 교차 행 배경 (줄무늬)
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.RowAlt
        };

        // 선택 행 좌측 2px Primary 바 (Fluent Design)
        grid.RowPostPaint += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Selected)
            {
                using var brush = new SolidBrush(ColorPalette.Primary);
                e.Graphics.FillRectangle(brush,
                    e.RowBounds.X, e.RowBounds.Y + 4, 3, e.RowBounds.Height - 8);
            }
        };
    }

    /// <summary>금액 컬럼용 오른쪽 정렬 스타일</summary>
    public static DataGridViewCellStyle AmountStyle => new()
    {
        Alignment = DataGridViewContentAlignment.MiddleRight,
        Format = "N0",
        Font = new Font("맑은 고딕", 10f)
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
