using System.Drawing;

namespace CubeManager.Dialogs;

public class AdminPasswordSetupDialog : Form
{
    private readonly TextBox _txtPassword;
    private readonly TextBox _txtConfirm;

    public string PasswordHash { get; private set; } = string.Empty;

    public AdminPasswordSetupDialog()
    {
        Text = "관리자 비밀번호 설정";
        Size = new Size(380, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblTitle = new Label
        {
            Text = "최초 실행: 관리자 비밀번호를 설정하세요.",
            Location = new Point(20, 15),
            Size = new Size(330, 20),
            Font = new Font("맑은 고딕", 10f)
        };

        var lblPw = new Label { Text = "비밀번호:", Location = new Point(20, 50), Size = new Size(80, 20) };
        _txtPassword = new TextBox
        {
            Location = new Point(110, 48),
            Size = new Size(230, 25),
            UseSystemPasswordChar = true
        };

        var lblConfirm = new Label { Text = "비밀번호 확인:", Location = new Point(20, 85), Size = new Size(90, 20) };
        _txtConfirm = new TextBox
        {
            Location = new Point(110, 83),
            Size = new Size(230, 25),
            UseSystemPasswordChar = true
        };

        var btnOk = new Button
        {
            Text = "설정",
            Location = new Point(170, 130),
            Size = new Size(80, 35),
            DialogResult = DialogResult.None
        };
        btnOk.Click += BtnOk_Click;

        var btnCancel = new Button
        {
            Text = "종료",
            Location = new Point(260, 130),
            Size = new Size(80, 35),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([lblTitle, lblPw, _txtPassword, lblConfirm, _txtConfirm, btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtPassword.Text))
        {
            MessageBox.Show("비밀번호를 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPassword.Focus();
            return;
        }

        if (_txtPassword.Text != _txtConfirm.Text)
        {
            MessageBox.Show("비밀번호가 일치하지 않습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtConfirm.Clear();
            _txtConfirm.Focus();
            return;
        }

        PasswordHash = BCrypt.Net.BCrypt.HashPassword(_txtPassword.Text, workFactor: 12);
        DialogResult = DialogResult.OK;
    }
}
