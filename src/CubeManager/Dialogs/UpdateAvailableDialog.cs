using System.Diagnostics;
using System.Drawing;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Helpers;
using Serilog;

namespace CubeManager.Dialogs;

public class UpdateAvailableDialog : Form
{
    private readonly IUpdateCheckService _updateService;
    private readonly AppUpdateInfo _updateInfo;
    private readonly Button _btnUpdate;
    private readonly Button _btnLater;
    private readonly ProgressBar _progressBar;
    private readonly Label _lblStatus;

    public UpdateAvailableDialog(IUpdateCheckService updateService, AppUpdateCheckResult result)
    {
        _updateService = updateService;
        _updateInfo = result.Latest ?? throw new ArgumentException("업데이트 정보가 없습니다.", nameof(result));

        Text = "업데이트";
        Size = new Size(460, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorPalette.Surface;
        ForeColor = ColorPalette.Text;
        Font = DesignTokens.FontBody;

        var title = new Label
        {
            Text = $"새 버전 {_updateInfo.Version}이 있습니다.",
            Font = new Font("맑은 고딕", 14f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Location = new Point(24, 22),
            Size = new Size(400, 30)
        };

        var current = new Label
        {
            Text = $"현재 버전: {result.CurrentVersion}",
            ForeColor = ColorPalette.TextSecondary,
            Location = new Point(24, 58),
            Size = new Size(400, 24)
        };

        var notes = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(_updateInfo.Notes)
                ? "릴리스 노트가 없습니다."
                : _updateInfo.Notes,
            Location = new Point(24, 90),
            Size = new Size(398, 82),
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.Card,
            ForeColor = ColorPalette.Text,
            ScrollBars = ScrollBars.Vertical
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(24, 184),
            Size = new Size(398, 18),
            Visible = false
        };

        _lblStatus = new Label
        {
            Text = "",
            ForeColor = ColorPalette.TextSecondary,
            Location = new Point(24, 207),
            Size = new Size(398, 22)
        };

        _btnLater = ButtonFactory.CreateGhost("나중에", 90);
        _btnLater.Location = new Point(226, 230);
        _btnLater.Enabled = !_updateInfo.Mandatory;
        _btnLater.Click += (_, _) => Close();

        _btnUpdate = ButtonFactory.CreatePrimary("업데이트", 100);
        _btnUpdate.Location = new Point(322, 230);
        _btnUpdate.Click += async (_, _) => await DownloadAndRunInstallerAsync();

        Controls.AddRange([title, current, notes, _progressBar, _lblStatus, _btnLater, _btnUpdate]);
    }

    private async Task DownloadAndRunInstallerAsync()
    {
        try
        {
            _btnUpdate.Enabled = false;
            _btnLater.Enabled = false;
            _progressBar.Visible = true;
            _lblStatus.Text = "설치파일 다운로드 중...";

            var progress = new Progress<int>(value => _progressBar.Value = Math.Clamp(value, 0, 100));
            var installerPath = await _updateService.DownloadInstallerAsync(_updateInfo, progress);

            _lblStatus.Text = "설치 프로그램을 실행합니다.";
            Log.Information("업데이트 설치파일 실행: {Path}", installerPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                UseShellExecute = true
            });

            Application.Exit();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "업데이트 다운로드/실행 실패");
            _lblStatus.Text = "업데이트에 실패했습니다.";
            ToastNotification.Show(ex.Message, ToastType.Error);
            _btnUpdate.Enabled = true;
            _btnLater.Enabled = true;
        }
    }
}
