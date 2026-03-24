using System.Drawing;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class SettingsTab : UserControl
{
    private readonly IReservationScraperService _scraperService;
    private readonly IConfigRepository _configRepo;

    public SettingsTab(IReservationScraperService scraperService, IConfigRepository configRepo)
    {
        _scraperService = scraperService;
        _configRepo = configRepo;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(15);

        // Header
        var header = new Label
        {
            Text = "설정",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Top,
            Height = 40
        };

        // === 웹 연동 설정 패널 ===
        var webPanel = new GroupBox
        {
            Text = "웹 연동 설정 (cubeescape.co.kr)",
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(10)
        };

        var lblUrl = new Label { Text = "URL:", Location = new Point(15, 28), Size = new Size(40, 22), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var txtUrl = new TextBox { Name = "txtUrl", Location = new Point(90, 26), Size = new Size(300, 25), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var lblId = new Label { Text = "아이디:", Location = new Point(15, 58), Size = new Size(60, 22), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var txtId = new TextBox { Name = "txtWebId", Location = new Point(90, 56), Size = new Size(200, 25), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var lblPw = new Label { Text = "비밀번호:", Location = new Point(15, 88), Size = new Size(70, 22), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var txtPw = new TextBox { Name = "txtWebPw", Location = new Point(90, 86), Size = new Size(200, 25), UseSystemPasswordChar = true, Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };

        var btnTest = ButtonFactory.CreateSecondary("연결 테스트", 100);
        btnTest.Location = new Point(310, 54);
        btnTest.Height = 30;
        btnTest.Click += async (_, _) =>
        {
            await ButtonFactory.RunWithLoadingAsync(btnTest, "테스트 중...", async () =>
            {
                var ok = await _scraperService.TestConnectionAsync(txtId.Text, txtPw.Text);
                ToastNotification.Show(ok ? "연결 성공!" : "로그인 실패: ID/PW를 확인하세요.",
                    ok ? ToastType.Success : ToastType.Error);
            });
        };

        var btnSaveWeb = ButtonFactory.CreatePrimary("저장", 100);
        btnSaveWeb.Location = new Point(310, 84);
        btnSaveWeb.Height = 30;
        btnSaveWeb.Click += async (_, _) =>
        {
            await _configRepo.SetAsync("web_base_url", txtUrl.Text.Trim());
            await _configRepo.SetAsync("web_login_id", CredentialHelper.Encrypt(txtId.Text));
            await _configRepo.SetAsync("web_login_pw", CredentialHelper.Encrypt(txtPw.Text));
            ToastNotification.Show("웹 연동 설정이 저장되었습니다.", ToastType.Success);
        };

        webPanel.Controls.AddRange([lblUrl, txtUrl, lblId, txtId, lblPw, txtPw, btnTest, btnSaveWeb]);

        Controls.Add(webPanel);
        Controls.Add(header);

        _ = LoadWebSettingsAsync(txtUrl, txtId);
    }

    private async Task LoadWebSettingsAsync(TextBox txtUrl, TextBox txtId)
    {
        var url = await _configRepo.GetAsync("web_base_url");
        var encId = await _configRepo.GetAsync("web_login_id");
        txtUrl.Text = url ?? "http://www.cubeescape.co.kr";
        txtId.Text = string.IsNullOrEmpty(encId) ? "" : CredentialHelper.Decrypt(encId);
        // PW는 표시하지 않음 (보안)
    }
}

/// <summary>직원 추가/수정 다이얼로그</summary>
internal class EmployeeEditDialog : Form
{
    private readonly TextBox _txtName;
    private readonly NumericUpDown _numWage;
    private readonly TextBox _txtPhone;

    public string EmpName => _txtName.Text.Trim();
    public int Wage => (int)_numWage.Value;
    public string? Phone => string.IsNullOrWhiteSpace(_txtPhone.Text) ? null : _txtPhone.Text.Trim();

    public EmployeeEditDialog(Employee? existing = null)
    {
        Text = existing == null ? "직원 추가" : "직원 수정";
        Size = new Size(360, 230);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);

        var y = 15;
        AddField("이름:", _txtName = new TextBox(), ref y);
        AddField("시급:", _numWage = new NumericUpDown
        {
            Maximum = 1_000_000, Minimum = 0, Increment = 100,
            ThousandsSeparator = true, Size = new Size(150, 25)
        }, ref y);
        AddField("연락처:", _txtPhone = new TextBox(), ref y);

        y += 10;
        var btnOk = ButtonFactory.CreatePrimary(existing == null ? "추가" : "수정", 80);
        btnOk.Location = new Point(150, y);
        btnOk.DialogResult = DialogResult.OK;

        var btnCancel = ButtonFactory.CreateGhost("취소", 80);
        btnCancel.Location = new Point(240, y);
        btnCancel.DialogResult = DialogResult.Cancel;
        Controls.AddRange([btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        if (existing != null)
        {
            _txtName.Text = existing.Name;
            _numWage.Value = existing.HourlyWage;
            _txtPhone.Text = existing.Phone ?? "";
        }
    }

    private void AddField(string label, Control control, ref int y)
    {
        Controls.Add(new Label { Text = label, Location = new Point(20, y + 2), Size = new Size(70, 22) });
        control.Location = new Point(95, y);
        if (control.Size.Width < 200) control.Size = new Size(230, 25);
        Controls.Add(control);
        y += 38;
    }
}
