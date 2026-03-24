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

        // 데이터 행 — 선택 시 피치색 배경 (WinForms 내장 처리, 격자선 유지)
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

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.RowAlt,
            ForeColor = ColorPalette.TableText,
            SelectionBackColor = SelectRowBg,
            SelectionForeColor = ColorPalette.TableText
        };

        // 선택행: 좌측 주황 바 + 테두리 (PostPaint만, PrePaint 제거)
        grid.RowPostPaint += (_, e) =>
        {
            if (e.RowIndex < 0 || !grid.Rows[e.RowIndex].Selected) return;
            var g = e.Graphics;
            var bounds = e.RowBounds;

            // 좌측 주황 악센트 바
            using var barBrush = new SolidBrush(ColorPalette.Accent);
            g.FillRectangle(barBrush, bounds.X, bounds.Y + 4, 3, bounds.Height - 8);

            // 테두리 (격자선 위에 그려짐, 격자선 자체는 보존)
            using var borderPen = new Pen(Color.FromArgb(150, ColorPalette.Accent), 1.5f);
            g.DrawRectangle(borderPen, bounds.X + 1, bounds.Y, bounds.Width - 3, bounds.Height - 1);
        };
    }

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
