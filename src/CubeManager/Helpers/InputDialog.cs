using System.Drawing;

namespace CubeManager.Helpers;

public static class InputDialog
{
    public static string? Show(string prompt, string title = "입력")
    {
        using var form = new Form
        {
            Text = title, Size = new Size(350, 150),
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false, MinimizeBox = false,
            Font = new Font("맑은 고딕", 10f)
        };

        var lbl = new Label { Text = prompt, Location = new Point(15, 12), Size = new Size(300, 20) };
        var txt = new TextBox { Location = new Point(15, 38), Size = new Size(300, 25) };
        var btnOk = new Button { Text = "확인", Location = new Point(150, 72), Size = new Size(75, 30), DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "취소", Location = new Point(235, 72), Size = new Size(75, 30), DialogResult = DialogResult.Cancel };

        form.Controls.AddRange([lbl, txt, btnOk, btnCancel]);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        return form.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text)
            ? txt.Text.Trim()
            : null;
    }
}
