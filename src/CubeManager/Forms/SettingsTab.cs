using System.Drawing;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Helpers;
using CubeManager.Telegram;

namespace CubeManager.Forms;

public class SettingsTab : UserControl
{
    private readonly IReservationScraperService _scraperService;
    private readonly IConfigRepository _configRepo;
    private readonly IUpdateCheckService _updateService;
    private readonly ITelegramBotConfigService _telegramConfig;
    private readonly ITelegramBotWorker _telegramWorker;
    private bool _isLoadingUpdateSettings;

    public SettingsTab(
        IReservationScraperService scraperService,
        IConfigRepository configRepo,
        IUpdateCheckService updateService,
        ITelegramBotConfigService telegramConfig,
        ITelegramBotWorker telegramWorker)
    {
        _scraperService = scraperService;
        _configRepo = configRepo;
        _updateService = updateService;
        _telegramConfig = telegramConfig;
        _telegramWorker = telegramWorker;
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

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ColorPalette.Surface
        };

        // === 웹 연동 설정 패널 ===
        var webPanel = new GroupBox
        {
            Text = "웹 연동 설정 (cubeescape.co.kr)",
            Dock = DockStyle.Top,
            Height = 145,
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

        // === 업데이트 설정 패널 ===
        var updatePanel = new GroupBox
        {
            Text = "프로그램 업데이트",
            Dock = DockStyle.Top,
            Height = 138,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(10)
        };

        var lblVersion = new Label
        {
            Text = $"현재 버전: {AppVersionHelper.CurrentVersion}",
            Location = new Point(15, 30),
            Size = new Size(220, 24),
            Font = new Font("맑은 고딕", 10f, FontStyle.Regular),
            ForeColor = ColorPalette.Text
        };

        var chkAutoUpdate = new CheckBox
        {
            Text = "시작 시 업데이트 확인",
            Location = new Point(15, 62),
            Size = new Size(220, 24),
            Font = new Font("맑은 고딕", 10f, FontStyle.Regular),
            ForeColor = ColorPalette.Text,
            Checked = true
        };

        var lblLastCheck = new Label
        {
            Text = "마지막 확인: -",
            Location = new Point(15, 94),
            Size = new Size(280, 24),
            Font = new Font("맑은 고딕", 9.5f, FontStyle.Regular),
            ForeColor = ColorPalette.TextSecondary
        };

        var btnCheckUpdate = ButtonFactory.CreateSecondary("업데이트 확인", 120);
        btnCheckUpdate.Location = new Point(300, 28);
        btnCheckUpdate.Height = 30;
        btnCheckUpdate.Click += async (_, _) =>
        {
            await ButtonFactory.RunWithLoadingAsync(btnCheckUpdate, "확인 중...", async () =>
            {
                await UpdateCoordinator.CheckAndPromptAsync(this, _updateService, showUpToDateToast: true);
                await LoadUpdateSettingsAsync(chkAutoUpdate, lblLastCheck);
            });
        };

        chkAutoUpdate.CheckedChanged += async (_, _) =>
        {
            if (_isLoadingUpdateSettings)
                return;

            await _configRepo.SetAsync("update_check_enabled", chkAutoUpdate.Checked ? "1" : "0");
            ToastNotification.Show("업데이트 설정이 저장되었습니다.", ToastType.Success);
        };

        updatePanel.Controls.AddRange([lblVersion, chkAutoUpdate, lblLastCheck, btnCheckUpdate]);

        // === 텔레그램 봇 설정 패널 ===
        var telegramPanel = new GroupBox
        {
            Text = "텔레그램 봇 설정",
            Dock = DockStyle.Top,
            Height = 200,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(10)
        };

        var lblToken = new Label { Text = "봇 토큰:", Location = new Point(15, 28), Size = new Size(80, 22), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var txtBotToken = new TextBox { Name = "txtBotToken", Location = new Point(110, 26), Size = new Size(280, 25), UseSystemPasswordChar = true, Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };

        var lblChatIds = new Label { Text = "허용 chat_id:", Location = new Point(15, 58), Size = new Size(90, 22), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var txtChatIds = new TextBox { Name = "txtChatIds", Location = new Point(110, 56), Size = new Size(280, 25), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };

        var lblHint = new Label
        {
            Text = "콤마 구분. 단톡방은 음수 (예: -1001234567890)",
            Location = new Point(110, 84),
            Size = new Size(300, 18),
            Font = new Font("맑은 고딕", 8.5f, FontStyle.Regular),
            ForeColor = ColorPalette.TextSecondary
        };

        var chkEnableBot = new CheckBox
        {
            Text = "봇 활성화",
            Location = new Point(15, 110),
            Size = new Size(120, 24),
            Font = new Font("맑은 고딕", 10f, FontStyle.Regular),
            ForeColor = ColorPalette.Text
        };

        var btnTestBot = ButtonFactory.CreateSecondary("테스트 발송", 110);
        btnTestBot.Location = new Point(15, 145);
        btnTestBot.Height = 32;
        btnTestBot.Click += async (_, _) =>
        {
            var token = txtBotToken.Text.Trim();
            var ids = TelegramBotOptions.ParseChatIds(txtChatIds.Text);
            if (string.IsNullOrEmpty(token) || ids.Count == 0)
            {
                ToastNotification.Show("토큰과 chat_id를 입력하세요.", ToastType.Warning);
                return;
            }
            await ButtonFactory.RunWithLoadingAsync(btnTestBot, "발송 중...", async () =>
            {
                var result = await _telegramWorker.SendTestMessageAsync(token, ids[0]);
                if (result.Success)
                    ToastNotification.Show($"테스트 발송 성공 (chat: {ids[0]})", ToastType.Success);
                else
                    ToastNotification.Show($"발송 실패: {result.Error}", ToastType.Error);
            });
        };

        var btnSaveBot = ButtonFactory.CreatePrimary("저장 + 재시작", 140);
        btnSaveBot.Location = new Point(135, 145);
        btnSaveBot.Height = 32;
        btnSaveBot.Click += async (_, _) =>
        {
            var token = txtBotToken.Text.Trim();
            var ids = TelegramBotOptions.ParseChatIds(txtChatIds.Text);
            await _telegramConfig.SaveAsync(token, ids, chkEnableBot.Checked);
            await _telegramWorker.RestartAsync();
            var status = chkEnableBot.Checked && !string.IsNullOrEmpty(token) && ids.Count > 0
                ? "저장 완료, 봇 재시작됨"
                : "저장 완료 (봇 비활성)";
            ToastNotification.Show(status, ToastType.Success);
        };

        telegramPanel.Controls.AddRange([lblToken, txtBotToken, lblChatIds, txtChatIds, lblHint, chkEnableBot, btnTestBot, btnSaveBot]);

        // Top 도킹은 Add 역순으로 쌓임. 맨 아래에 telegramPanel을 두려면 가장 먼저 Add.
        content.Controls.Add(telegramPanel);
        content.Controls.Add(updatePanel);
        content.Controls.Add(webPanel);

        Controls.Add(content);
        Controls.Add(header);

        _ = LoadWebSettingsAsync(txtUrl, txtId);
        _ = LoadUpdateSettingsAsync(chkAutoUpdate, lblLastCheck);
        _ = LoadTelegramSettingsAsync(txtBotToken, txtChatIds, chkEnableBot);
    }

    private async Task LoadTelegramSettingsAsync(TextBox txtToken, TextBox txtChatIds, CheckBox chkEnable)
    {
        var opts = await _telegramConfig.LoadAsync();
        txtToken.Text = opts.Token;
        txtChatIds.Text = TelegramBotOptions.FormatChatIds(opts.AllowedChatIds);
        chkEnable.Checked = opts.Enabled;
    }

    private async Task LoadWebSettingsAsync(TextBox txtUrl, TextBox txtId)
    {
        var url = await _configRepo.GetAsync("web_base_url");
        var encId = await _configRepo.GetAsync("web_login_id");
        txtUrl.Text = url ?? "http://www.cubeescape.co.kr";
        txtId.Text = string.IsNullOrEmpty(encId) ? "" : CredentialHelper.Decrypt(encId);
        // PW는 표시하지 않음 (보안)
    }

    private async Task LoadUpdateSettingsAsync(CheckBox chkAutoUpdate, Label lblLastCheck)
    {
        var enabled = await _configRepo.GetIntAsync("update_check_enabled", 1);
        var lastCheck = await _configRepo.GetAsync("update_last_check_at");

        _isLoadingUpdateSettings = true;
        chkAutoUpdate.Checked = enabled == 1;
        _isLoadingUpdateSettings = false;

        lblLastCheck.Text = string.IsNullOrWhiteSpace(lastCheck)
            ? "마지막 확인: -"
            : $"마지막 확인: {lastCheck}";
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
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorPalette.Surface;
        ForeColor = ColorPalette.Text;
        Font = DesignTokens.FontBody;

        var y = 15;
        _txtName = new TextBox { ImeMode = ImeMode.Hangul };
        AddField("이름:", _txtName, ref y);
        AddField("시급:", _numWage = new NumericUpDown
        {
            Maximum = 1_000_000, Minimum = 0, Increment = 100,
            ThousandsSeparator = true, Size = new Size(150, 25)
        }, ref y);
        _txtPhone = new TextBox { ImeMode = ImeMode.Alpha };
        AddField("연락처:", _txtPhone, ref y);

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
        Controls.Add(new Label
        {
            Text = label, Location = new Point(20, y + 2), Size = new Size(70, 22),
            ForeColor = ColorPalette.Text, Font = DesignTokens.FontBody
        });
        control.Location = new Point(95, y);
        if (control.Size.Width < 200) control.Size = new Size(230, 25);
        control.BackColor = ColorPalette.Card;
        control.ForeColor = ColorPalette.Text;
        control.Font = DesignTokens.FontBody;
        Controls.Add(control);
        y += 38;
    }
}
