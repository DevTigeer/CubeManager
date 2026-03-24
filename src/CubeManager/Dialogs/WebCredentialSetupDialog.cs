using System.Drawing;
using CubeManager.Core.Interfaces.Services;

namespace CubeManager.Dialogs;

/// <summary>
/// 최초 실행 시 cubeescape.co.kr 웹 관리자 자격증명을 입력받는 다이얼로그.
/// [연결 테스트]로 사전 검증 후 [저장], 또는 [건너뛰기]로 나중에 설정 가능.
/// </summary>
public class WebCredentialSetupDialog : Form
{
    private readonly IReservationScraperService? _scraperService;
    private readonly TextBox _txtId;
    private readonly TextBox _txtPw;
    private readonly Label _lblStatus;
    private readonly Button _btnTest;
    private readonly Button _btnSave;

    /// <summary>입력된 아이디 (평문)</summary>
    public string WebId => _txtId.Text.Trim();

    /// <summary>입력된 비밀번호 (평문)</summary>
    public string WebPw => _txtPw.Text;

    public WebCredentialSetupDialog(IReservationScraperService? scraperService = null)
    {
        _scraperService = scraperService;

        Text = "웹 연동 설정";
        Size = new Size(420, 280);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);

        // 안내 문구
        var lblGuide = new Label
        {
            Text = "예약 데이터를 조회하려면\ncubeescape.co.kr 관리자 계정이 필요합니다.",
            Location = new Point(20, 15),
            Size = new Size(370, 40),
            Font = new Font("맑은 고딕", 10f)
        };

        // 아이디
        var lblId = new Label { Text = "아이디:", Location = new Point(20, 70), Size = new Size(70, 22) };
        _txtId = new TextBox
        {
            Location = new Point(100, 68),
            Size = new Size(280, 25)
        };

        // 비밀번호
        var lblPw = new Label { Text = "비밀번호:", Location = new Point(20, 105), Size = new Size(70, 22) };
        _txtPw = new TextBox
        {
            Location = new Point(100, 103),
            Size = new Size(280, 25),
            UseSystemPasswordChar = true
        };

        // 상태 라벨
        _lblStatus = new Label
        {
            Location = new Point(20, 145),
            Size = new Size(370, 22),
            ForeColor = Color.Gray,
            Text = ""
        };

        // 연결 테스트 버튼
        _btnTest = new Button
        {
            Text = "연결 테스트",
            Location = new Point(20, 180),
            Size = new Size(110, 35),
            FlatStyle = FlatStyle.Flat
        };
        _btnTest.Click += BtnTest_Click;

        // 저장 버튼
        _btnSave = new Button
        {
            Text = "저장",
            Location = new Point(200, 180),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(25, 118, 210),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.None
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += BtnSave_Click;

        // 건너뛰기 버튼
        var btnSkip = new Button
        {
            Text = "건너뛰기",
            Location = new Point(300, 180),
            Size = new Size(90, 35),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([lblGuide, lblId, _txtId, lblPw, _txtPw,
            _lblStatus, _btnTest, _btnSave, btnSkip]);
        AcceptButton = _btnSave;
        CancelButton = btnSkip;
    }

    private async void BtnTest_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtId.Text) || string.IsNullOrWhiteSpace(_txtPw.Text))
        {
            SetStatus("아이디와 비밀번호를 입력하세요.", Color.OrangeRed);
            return;
        }

        if (_scraperService == null)
        {
            SetStatus("스크래핑 서비스를 사용할 수 없습니다.", Color.OrangeRed);
            return;
        }

        _btnTest.Enabled = false;
        _btnTest.Text = "테스트 중...";
        SetStatus("연결 확인 중...", Color.Gray);

        try
        {
            var ok = await _scraperService.TestConnectionAsync(_txtId.Text.Trim(), _txtPw.Text);
            if (ok)
            {
                SetStatus("✓ 연결 성공! 저장 버튼을 눌러주세요.", Color.FromArgb(76, 175, 80));
            }
            else
            {
                SetStatus("✗ 로그인 실패: ID/PW를 확인하세요.", Color.FromArgb(244, 67, 54));
            }
        }
        catch (Exception ex)
        {
            SetStatus($"✗ 연결 오류: {ex.Message}", Color.FromArgb(244, 67, 54));
        }
        finally
        {
            _btnTest.Enabled = true;
            _btnTest.Text = "연결 테스트";
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtId.Text))
        {
            SetStatus("아이디를 입력하세요.", Color.OrangeRed);
            _txtId.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtPw.Text))
        {
            SetStatus("비밀번호를 입력하세요.", Color.OrangeRed);
            _txtPw.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
    }

    private void SetStatus(string text, Color color)
    {
        _lblStatus.Text = text;
        _lblStatus.ForeColor = color;
    }
}
