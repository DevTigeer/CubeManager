using System.Drawing;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class SettingsTab : UserControl
{
    private readonly IEmployeeService _employeeService;
    private readonly IReservationScraperService _scraperService;
    private readonly IConfigRepository _configRepo;
    private readonly DataGridView _grid;

    public SettingsTab(IEmployeeService employeeService,
        IReservationScraperService scraperService, IConfigRepository configRepo)
    {
        _employeeService = employeeService;
        _scraperService = scraperService;
        _configRepo = configRepo;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(15);

        // Header
        var header = new Label
        {
            Text = "직원 관리",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Dock = DockStyle.Top,
            Height = 40
        };

        // Toolbar
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 45,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 5)
        };

        var btnAdd = CreateButton("+ 직원 추가", ColorPalette.Primary);
        btnAdd.Click += BtnAdd_Click;

        var btnDeactivate = CreateButton("비활성화", ColorPalette.Danger);
        btnDeactivate.Margin = new Padding(10, 0, 0, 0);
        btnDeactivate.Click += BtnDeactivate_Click;

        toolbar.Controls.AddRange([btnAdd, btnDeactivate]);

        // DataGridView
        _grid = new DataGridView { Dock = DockStyle.Fill };
        GridTheme.ApplyTheme(_grid);

        SetupColumns();
        _grid.CellDoubleClick += Grid_CellDoubleClick;
        _grid.CellContentClick += Grid_CellContentClick;

        // === 웹 연동 설정 패널 (하단) ===
        var webPanel = new GroupBox
        {
            Text = "웹 연동 설정 (cubeescape.co.kr)",
            Dock = DockStyle.Bottom,
            Height = 130,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(10)
        };

        var lblUrl = new Label { Text = "URL:", Location = new Point(15, 28), Size = new Size(40, 22), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var txtUrl = new TextBox { Name = "txtUrl", Location = new Point(90, 26), Size = new Size(300, 25), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var lblId = new Label { Text = "아이디:", Location = new Point(15, 58), Size = new Size(60, 22), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var txtId = new TextBox { Name = "txtWebId", Location = new Point(90, 56), Size = new Size(200, 25), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var lblPw = new Label { Text = "비밀번호:", Location = new Point(15, 88), Size = new Size(70, 22), Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };
        var txtPw = new TextBox { Name = "txtWebPw", Location = new Point(90, 86), Size = new Size(200, 25), UseSystemPasswordChar = true, Font = new Font("맑은 고딕", 10f, FontStyle.Regular) };

        var btnTest = new Button
        {
            Text = "연결 테스트", Location = new Point(310, 54), Size = new Size(100, 30),
            FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 9f, FontStyle.Regular)
        };
        btnTest.Click += async (_, _) =>
        {
            btnTest.Enabled = false;
            btnTest.Text = "테스트 중...";
            try
            {
                var ok = await _scraperService.TestConnectionAsync(txtId.Text, txtPw.Text);
                ToastNotification.Show(ok ? "연결 성공!" : "로그인 실패: ID/PW를 확인하세요.",
                    ok ? ToastType.Success : ToastType.Error);
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
            finally { btnTest.Enabled = true; btnTest.Text = "연결 테스트"; }
        };

        var btnSaveWeb = new Button
        {
            Text = "저장", Location = new Point(310, 84), Size = new Size(100, 30),
            BackColor = ColorPalette.Primary, ForeColor = ColorPalette.TextWhite,
            FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 9f, FontStyle.Regular)
        };
        btnSaveWeb.FlatAppearance.BorderSize = 0;
        btnSaveWeb.Click += async (_, _) =>
        {
            await _configRepo.SetAsync("web_base_url", txtUrl.Text.Trim());
            await _configRepo.SetAsync("web_login_id", CredentialHelper.Encrypt(txtId.Text));
            await _configRepo.SetAsync("web_login_pw", CredentialHelper.Encrypt(txtPw.Text));
            ToastNotification.Show("웹 연동 설정이 저장되었습니다.", ToastType.Success);
        };

        webPanel.Controls.AddRange([lblUrl, txtUrl, lblId, txtId, lblPw, txtPw, btnTest, btnSaveWeb]);

        Controls.Add(_grid);
        Controls.Add(webPanel);
        Controls.Add(toolbar);
        Controls.Add(header);

        _ = LoadDataAsync();
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

    private static Button CreateButton(string text, Color color)
    {
        var btn = ButtonFactory.CreatePrimary(text, 110);
        btn.BackColor = color;
        return btn;
    }

    private void SetupColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Id", Visible = false });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
            { Name = "IsActive", HeaderText = "활성", Width = 55, ReadOnly = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Name", HeaderText = "이름", FillWeight = 25, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "HourlyWage", HeaderText = "시급", FillWeight = 20, ReadOnly = true,
              DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Phone", HeaderText = "연락처", FillWeight = 30, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Status", HeaderText = "상태", Width = 70, ReadOnly = true,
              DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var employees = (await _employeeService.GetAllAsync()).ToList();
            _grid.Rows.Clear();

            foreach (var emp in employees)
            {
                var idx = _grid.Rows.Add();
                var row = _grid.Rows[idx];
                row.Cells["Id"].Value = emp.Id;
                row.Cells["IsActive"].Value = emp.IsActive;
                row.Cells["Name"].Value = emp.Name;
                row.Cells["HourlyWage"].Value = emp.HourlyWage.ToString("N0");
                row.Cells["Phone"].Value = emp.Phone ?? "";
                row.Cells["Status"].Value = emp.IsActive ? "활성" : "비활성";
                row.Cells["Status"].Style.ForeColor =
                    emp.IsActive ? ColorPalette.Success : ColorPalette.MissingRecord;
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"목록 로드 실패: {ex.Message}", ToastType.Error);
        }
    }

    private async void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var dlg = new EmployeeEditDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _employeeService.AddEmployeeAsync(dlg.EmpName, dlg.Wage, dlg.Phone);
            ToastNotification.Show("직원이 추가되었습니다.", ToastType.Success);
            await LoadDataAsync();
        }
        catch (ArgumentException ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Warning);
        }
    }

    private async void BtnDeactivate_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow == null) return;
        var id = (int)_grid.CurrentRow.Cells["Id"].Value;
        var name = _grid.CurrentRow.Cells["Name"].Value?.ToString();

        if (MessageBox.Show($"'{name}' 직원을 비활성화하시겠습니까?", "확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            await _employeeService.DeactivateAsync(id);
            ToastNotification.Show($"'{name}' 비활성화 완료.", ToastType.Success);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private async void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var id = (int)_grid.Rows[e.RowIndex].Cells["Id"].Value;
        var emp = await _employeeService.GetByIdAsync(id);
        if (emp == null) return;

        using var dlg = new EmployeeEditDialog(emp);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _employeeService.UpdateEmployeeAsync(id, dlg.EmpName, dlg.Wage, dlg.Phone);
            ToastNotification.Show("직원 정보가 수정되었습니다.", ToastType.Success);
            await LoadDataAsync();
        }
        catch (ArgumentException ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Warning);
        }
    }

    private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "IsActive") return;

        _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        var id = (int)_grid.Rows[e.RowIndex].Cells["Id"].Value;
        var isActive = (bool)_grid.Rows[e.RowIndex].Cells["IsActive"].Value;

        try
        {
            await _employeeService.ToggleActiveAsync(id, isActive);
            _grid.Rows[e.RowIndex].Cells["Status"].Value = isActive ? "활성" : "비활성";
            _grid.Rows[e.RowIndex].Cells["Status"].Style.ForeColor =
                isActive ? ColorPalette.Success : ColorPalette.MissingRecord;
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
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
        var btnOk = new Button
        {
            Text = existing == null ? "추가" : "수정",
            Location = new Point(150, y), Size = new Size(80, 35),
            BackColor = ColorPalette.Primary, ForeColor = ColorPalette.TextWhite,
            FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;
        var btnCancel = new Button
        {
            Text = "취소", Location = new Point(240, y), Size = new Size(80, 35),
            DialogResult = DialogResult.Cancel
        };
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
