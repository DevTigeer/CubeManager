using System.Drawing;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Helpers;

namespace CubeManager.Dialogs;

public class AdminAuthDialog : Form
{
    private readonly IConfigRepository _configRepo;
    private readonly TextBox _txtPassword;

    public AdminAuthDialog(IConfigRepository configRepo)
    {
        _configRepo = configRepo;

        Text = "관리자 인증";
        Size = new Size(340, 160);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var lbl = new Label { Text = "비밀번호:", Location = new Point(20, 25), Size = new Size(70, 20) };
        _txtPassword = new TextBox
        {
            Location = new Point(100, 23),
            Size = new Size(210, 25),
            UseSystemPasswordChar = true
        };

        var btnOk = new Button
        {
            Text = "확인",
            Location = new Point(140, 70),
            Size = new Size(80, 35),
            DialogResult = DialogResult.None
        };
        btnOk.Click += BtnOk_Click;

        var btnCancel = new Button
        {
            Text = "취소",
            Location = new Point(230, 70),
            Size = new Size(80, 35),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([lbl, _txtPassword, btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    /// <summary>
    /// 관리자 인증을 요청한다. 캐시 유효 시 바로 true 반환.
    /// </summary>
    public static bool Authenticate(IConfigRepository configRepo, IWin32Window? owner = null)
    {
        if (AdminAuthCache.IsValid())
            return true;

        using var dlg = new AdminAuthDialog(configRepo);
        return dlg.ShowDialog(owner) == DialogResult.OK;
    }

    private async void BtnOk_Click(object? sender, EventArgs e)
    {
        try
        {
            var hash = await _configRepo.GetAsync("admin_password_hash");
            if (hash != null && BCrypt.Net.BCrypt.Verify(_txtPassword.Text, hash))
            {
                AdminAuthCache.SetAuthenticated();
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("비밀번호가 틀렸습니다.", "인증 실패",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPassword.Clear();
                _txtPassword.Focus();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"인증 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
