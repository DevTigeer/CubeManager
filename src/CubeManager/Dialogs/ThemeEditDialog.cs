using System.Drawing;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Dialogs;

/// <summary>테마 추가/수정 다이얼로그</summary>
internal class ThemeEditDialog : Form
{
    private readonly TextBox _txtName;
    private readonly TextBox _txtDesc;

    public string ThemeName => _txtName.Text.Trim();
    public string? Description => string.IsNullOrWhiteSpace(_txtDesc.Text) ? null : _txtDesc.Text.Trim();

    public ThemeEditDialog(Theme? existing = null)
    {
        Text = existing == null ? "테마 추가" : "테마 수정";
        Size = new Size(400, 200);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);

        var y = 15;
        Controls.Add(new Label { Text = "테마 이름:", Location = new Point(20, y + 2), Size = new Size(80, 22) });
        _txtName = new TextBox { Location = new Point(105, y), Size = new Size(260, 25) };
        Controls.Add(_txtName);
        y += 38;

        Controls.Add(new Label { Text = "설명:", Location = new Point(20, y + 2), Size = new Size(80, 22) });
        _txtDesc = new TextBox { Location = new Point(105, y), Size = new Size(260, 25) };
        Controls.Add(_txtDesc);
        y += 48;

        var btnOk = ButtonFactory.CreatePrimary(existing == null ? "추가" : "수정");
        btnOk.Location = new Point(190, y);
        btnOk.Size = new Size(80, 35);
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("테마 이름을 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
        };

        var btnCancel = new Button { Text = "취소", Location = new Point(280, y), Size = new Size(80, 35), DialogResult = DialogResult.Cancel };
        Controls.AddRange([btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        if (existing != null)
        {
            _txtName.Text = existing.ThemeName;
            _txtDesc.Text = existing.Description ?? "";
        }
    }
}
