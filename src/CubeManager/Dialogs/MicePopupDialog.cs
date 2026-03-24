using System.Drawing;
using CubeManager.Helpers;

namespace CubeManager.Dialogs;

/// <summary>
/// 미끼관리 팝업. "완료하였습니다" 정확히 입력해야 닫을 수 있음.
/// TopMost, 닫기(X) 버튼 비활성.
/// </summary>
public class MicePopupDialog : Form
{
    private readonly TextBox _txtConfirm;
    private readonly Button _btnConfirm;

    public MicePopupDialog(string title, string content)
    {
        Text = title;
        Size = new Size(450, 320);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        ControlBox = false; // X 버튼 비활성
        Font = new Font("맑은 고딕", 10f);
        BackColor = ColorPalette.Surface;

        // 제목
        var lblTitle = new Label
        {
            Text = $"📋 {title}",
            Font = new Font("맑은 고딕", 14f, FontStyle.Bold),
            ForeColor = ColorPalette.Primary,
            Dock = DockStyle.Top, Height = 45,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(15, 0, 0, 0)
        };

        // 내용
        var lblContent = new Label
        {
            Text = content,
            Font = new Font("맑은 고딕", 11f),
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 10, 20, 10),
            TextAlign = ContentAlignment.TopLeft
        };

        // 하단 패널
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom, Height = 90,
            Padding = new Padding(20, 10, 20, 15)
        };

        var lblHint = new Label
        {
            Text = "아래에 \"완료하였습니다\"를 정확히 입력하세요:",
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextSecondary,
            Dock = DockStyle.Top, Height = 22
        };

        _txtConfirm = new TextBox
        {
            Dock = DockStyle.Top, Height = 28,
            Font = new Font("맑은 고딕", 11f),
            PlaceholderText = "완료하였습니다"
        };
        _txtConfirm.TextChanged += (_, _) =>
        {
            _btnConfirm!.Enabled = _txtConfirm.Text.Trim() == "완료하였습니다";
            _btnConfirm.BackColor = _btnConfirm.Enabled ? ColorPalette.Primary : ColorPalette.Border;
        };

        _btnConfirm = ButtonFactory.CreatePrimary("확인");
        _btnConfirm.Dock = DockStyle.Bottom;
        _btnConfirm.Height = 34;
        _btnConfirm.Enabled = false;
        _btnConfirm.BackColor = ColorPalette.Border;
        _btnConfirm.Click += (_, _) => { DialogResult = DialogResult.OK; };

        bottomPanel.Controls.Add(_btnConfirm);
        bottomPanel.Controls.Add(_txtConfirm);
        bottomPanel.Controls.Add(lblHint);

        Controls.Add(lblContent);
        Controls.Add(bottomPanel);
        Controls.Add(lblTitle);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 사용자가 Alt+F4 등으로 닫으려 할 때 차단
        if (DialogResult != DialogResult.OK)
            e.Cancel = true;
    }
}
