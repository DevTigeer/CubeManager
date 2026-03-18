using System.Drawing;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Dialogs;

public class EmployeePickerDialog : Form
{
    private readonly ListBox _listBox;

    public Employee? SelectedEmployee { get; private set; }

    public EmployeePickerDialog(IEnumerable<Employee> employees)
    {
        Text = "직원 선택";
        Size = new Size(300, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);

        _listBox = new ListBox
        {
            Location = new Point(15, 15),
            Size = new Size(255, 240),
            Font = new Font("맑은 고딕", 12f),
            ItemHeight = 30
        };

        foreach (var emp in employees)
            _listBox.Items.Add(emp);

        _listBox.DisplayMember = "Name";
        _listBox.DoubleClick += (_, _) => SelectAndClose();

        var btnOk = new Button
        {
            Text = "선택",
            Location = new Point(100, 270),
            Size = new Size(80, 35),
            BackColor = ColorPalette.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (_, _) => SelectAndClose();

        var btnCancel = new Button
        {
            Text = "취소",
            Location = new Point(190, 270),
            Size = new Size(80, 35),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([_listBox, btnOk, btnCancel]);
        CancelButton = btnCancel;
    }

    private void SelectAndClose()
    {
        if (_listBox.SelectedItem is Employee emp)
        {
            SelectedEmployee = emp;
            DialogResult = DialogResult.OK;
        }
    }
}
