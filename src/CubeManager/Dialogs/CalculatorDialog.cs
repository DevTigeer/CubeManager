using System.Data;
using System.Drawing;
using CubeManager.Helpers;

namespace CubeManager.Dialogs;

/// <summary>간이 수식 계산기. 10000*4 → 40,000 표시.</summary>
public class CalculatorDialog : Form
{
    private readonly TextBox _txtExpr;
    private readonly Label _lblResult;

    public CalculatorDialog()
    {
        Text = "간이 계산기";
        Size = new Size(340, 190);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 11f);
        BackColor = ColorPalette.Surface;
        KeyPreview = true;

        var lblTitle = new Label
        {
            Text = "수식을 입력하세요 (예: 10000*4+5000)",
            Location = new Point(15, 12),
            Size = new Size(300, 22),
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextSecondary
        };

        _txtExpr = new TextBox
        {
            Location = new Point(15, 38),
            Size = new Size(295, 30),
            Font = new Font("Consolas", 14f),
            PlaceholderText = "10000*4"
        };
        _txtExpr.TextChanged += (_, _) => Calculate();

        _lblResult = new Label
        {
            Location = new Point(15, 78),
            Size = new Size(295, 35),
            Font = new Font("맑은 고딕", 18f, FontStyle.Bold),
            ForeColor = ColorPalette.Primary,
            TextAlign = ContentAlignment.MiddleRight,
            Text = "0"
        };

        var btnCopy = ButtonFactory.CreatePrimary("복사", 70);
        btnCopy.Location = new Point(155, 118);
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(_lblResult.Text.Replace(",", "").Replace("원", "").Trim());
            ToastNotification.Show("결과가 클립보드에 복사되었습니다.", ToastType.Success);
        };

        var btnClose = ButtonFactory.CreateGhost("닫기", 70);
        btnClose.Location = new Point(235, 118);
        btnClose.Click += (_, _) => Close();

        Controls.AddRange([lblTitle, _txtExpr, _lblResult, btnCopy, btnClose]);
        CancelButton = btnClose;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Clipboard.SetText(_lblResult.Text.Replace(",", "").Replace("원", "").Trim());
                ToastNotification.Show("결과 복사됨!", ToastType.Success);
            }
        };
    }

    private void Calculate()
    {
        var expr = _txtExpr.Text.Trim();
        if (string.IsNullOrEmpty(expr))
        {
            _lblResult.Text = "0";
            _lblResult.ForeColor = ColorPalette.Primary;
            return;
        }

        try
        {
            // DataTable.Compute로 수식 계산 (+, -, *, / 지원)
            var result = new DataTable().Compute(expr, null);
            var value = Convert.ToDecimal(result);
            _lblResult.Text = $"{value:N0}";
            _lblResult.ForeColor = ColorPalette.Primary;
        }
        catch
        {
            _lblResult.Text = "?";
            _lblResult.ForeColor = ColorPalette.TextTertiary;
        }
    }
}
