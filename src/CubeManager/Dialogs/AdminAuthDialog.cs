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
        BackColor = ColorPalette.Surface;
        ForeColor = ColorPalette.Text;

        var lbl = new Label
        {
            Text = "비밀번호:",
            Location = new Point(20, 25),
            Size = new Size(70, 20),
            Font = DesignTokens.FontBody,
            ForeColor = ColorPalette.Text
        };

        _txtPassword = new TextBox
        {
            Location = new Point(100, 23),
            Size = new Size(210, 25),
            UseSystemPasswordChar = true,
            Font = DesignTokens.FontBody,
            BackColor = ColorPalette.Card,
            ForeColor = ColorPalette.Text
        };

        var btnOk = ButtonFactory.CreatePrimary("확인", 80);
        btnOk.Location = new Point(140, 70);
        btnOk.DialogResult = DialogResult.None;
        btnOk.Click += BtnOk_Click;

        var btnCancel = ButtonFactory.CreateGhost("취소", 80);
        btnCancel.Location = new Point(230, 70);
        btnCancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange([lbl, _txtPassword, btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    public static bool Authenticate(IConfigRepository configRepo, IWin32Window? owner = null)
    {
        // 캐시 무효화 — 매번 인증 필요
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
                DialogResult = DialogResult.OK;
            }
            else
            {
                ToastNotification.Show("비밀번호가 틀렸습니다.", ToastType.Error);
                _txtPassword.Clear();
                _txtPassword.Focus();
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"인증 오류: {ex.Message}", ToastType.Error);
        }
    }
}
