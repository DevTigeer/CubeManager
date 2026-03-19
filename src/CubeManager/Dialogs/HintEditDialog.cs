using System.Drawing;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Dialogs;

/// <summary>힌트 추가/수정 다이얼로그. 힌트코드는 자동 생성(수정 가능).</summary>
internal class HintEditDialog : Form
{
    private readonly NumericUpDown _numCode;
    private readonly TextBox _txtQuestion;
    private readonly TextBox _txtHint1;
    private readonly TextBox _txtHint2;
    private readonly TextBox _txtAnswer;

    public int HintCode => (int)_numCode.Value;
    public string Question => _txtQuestion.Text.Trim();
    public string Hint1Text => _txtHint1.Text.Trim();
    public string? Hint2Text => string.IsNullOrWhiteSpace(_txtHint2.Text) ? null : _txtHint2.Text.Trim();
    public string Answer => _txtAnswer.Text.Trim();

    public HintEditDialog(ThemeHint? existing = null)
    {
        Text = existing == null ? "힌트 추가" : "힌트 수정";
        Size = new Size(480, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);

        var y = 15;
        AddField("힌트코드:", _numCode = new NumericUpDown
        {
            Minimum = 1000, Maximum = 9999,
            Value = existing?.HintCode ?? Random.Shared.Next(1000, 10000),
            Size = new Size(120, 25)
        }, ref y);

        AddField("문제:", _txtQuestion = new TextBox { Size = new Size(320, 25) }, ref y);
        AddField("힌트 1:", _txtHint1 = new TextBox { Size = new Size(320, 25) }, ref y);
        AddField("힌트 2:", _txtHint2 = new TextBox { Size = new Size(320, 25) }, ref y);
        AddField("정답:", _txtAnswer = new TextBox { Size = new Size(320, 25) }, ref y);

        y += 10;
        var btnOk = ButtonFactory.CreatePrimary(existing == null ? "추가" : "수정");
        btnOk.Location = new Point(280, y);
        btnOk.Size = new Size(80, 35);
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtQuestion.Text))
            {
                MessageBox.Show("문제를 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtQuestion.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtHint1.Text))
            {
                MessageBox.Show("힌트 1을 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtHint1.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtAnswer.Text))
            {
                MessageBox.Show("정답을 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtAnswer.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
        };

        var btnCancel = new Button { Text = "취소", Location = new Point(370, y), Size = new Size(80, 35), DialogResult = DialogResult.Cancel };
        Controls.AddRange([btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        if (existing != null)
        {
            _txtQuestion.Text = existing.Question;
            _txtHint1.Text = existing.Hint1;
            _txtHint2.Text = existing.Hint2 ?? "";
            _txtAnswer.Text = existing.Answer;
        }
    }

    private void AddField(string label, Control control, ref int y)
    {
        Controls.Add(new Label { Text = label, Location = new Point(20, y + 2), Size = new Size(80, 22) });
        control.Location = new Point(105, y);
        Controls.Add(control);
        y += 38;
    }
}
