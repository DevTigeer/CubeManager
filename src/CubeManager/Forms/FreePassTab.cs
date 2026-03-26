using System.Drawing;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

/// <summary>
/// 무료이용권 관리 탭.
/// 월최고기록/장치보상/기타 사유로 발급, A2000부터 순차 번호.
/// </summary>
public class FreePassTab : UserControl
{
    private readonly IFreePassRepository _repo;
    private readonly DataGridView _grid;
    private readonly DateTimePicker _dtpMonth;
    private readonly CheckBox _chkAll;

    // 입력 폼
    private readonly TextBox _txtName;
    private readonly NumericUpDown _numCount;
    private readonly TextBox _txtPhone;
    private readonly ComboBox _cmbReason;
    private readonly TextBox _txtNote;

    public FreePassTab(IFreePassRepository repo)
    {
        _repo = repo;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(15);

        // ========== 1. 상단 헤더 ==========
        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 45,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 5)
        };

        topBar.Controls.Add(new Label
        {
            Text = "무료이용권 관리",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Size = new Size(180, 32),
            TextAlign = ContentAlignment.MiddleLeft
        });

        _dtpMonth = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy년 MM월",
            ShowUpDown = true,
            Size = new Size(140, 28),
            Value = DateTime.Today,
            Margin = new Padding(10, 2, 0, 0)
        };
        _dtpMonth.ValueChanged += (_, _) => _ = LoadAsync();
        topBar.Controls.Add(_dtpMonth);

        _chkAll = new CheckBox
        {
            Text = "전체", Size = new Size(60, 28),
            Margin = new Padding(5, 4, 0, 0),
            Font = new Font("맑은 고딕", 9f)
        };
        _chkAll.CheckedChanged += (_, _) => _ = LoadAsync();
        topBar.Controls.Add(_chkAll);

        // ========== 2. 발급 입력 폼 ==========
        var inputBox = new GroupBox
        {
            Text = "무료이용권 발급",
            Dock = DockStyle.Top, Height = 85,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Padding = new Padding(10, 5, 10, 5)
        };

        var inputFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 0)
        };

        var nf = new Font("맑은 고딕", 10f);

        inputFlow.Controls.Add(MakeLabel("이름:", 50, nf));
        _txtName = new TextBox { Size = new Size(90, 25), Font = nf };
        inputFlow.Controls.Add(_txtName);

        inputFlow.Controls.Add(MakeLabel("인원:", 50, nf, 6));
        _numCount = new NumericUpDown { Minimum = 1, Maximum = 20, Value = 2, Size = new Size(55, 25), Font = nf };
        inputFlow.Controls.Add(_numCount);

        inputFlow.Controls.Add(MakeLabel("전화:", 50, nf, 6));
        _txtPhone = new TextBox { Size = new Size(120, 25), Font = nf, PlaceholderText = "010-0000-0000" };
        inputFlow.Controls.Add(_txtPhone);

        inputFlow.Controls.Add(MakeLabel("사유:", 50, nf, 6));
        _cmbReason = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(100, 25), Font = nf,
            Items = { "월최고기록", "장치보상", "기타" }
        };
        _cmbReason.SelectedIndex = 0;
        inputFlow.Controls.Add(_cmbReason);

        inputFlow.Controls.Add(MakeLabel("비고:", 50, nf, 6));
        _txtNote = new TextBox { Size = new Size(120, 25), Font = nf };
        inputFlow.Controls.Add(_txtNote);

        var btnIssue = ButtonFactory.CreatePrimary("+ 발급", 80);
        btnIssue.Margin = new Padding(10, 0, 0, 0);
        btnIssue.Click += BtnIssue_Click;
        inputFlow.Controls.Add(btnIssue);

        // Enter키로 발급
        void HandleEnterKey(object? s, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BtnIssue_Click(btnIssue, EventArgs.Empty); }
        }
        _txtName.KeyDown += HandleEnterKey;
        _txtPhone.KeyDown += HandleEnterKey;
        _txtNote.KeyDown += HandleEnterKey;

        inputBox.Controls.Add(inputFlow);

        // ========== 3. 그리드 ==========
        _grid = new DataGridView { Dock = DockStyle.Fill };
        GridTheme.ApplyTheme(_grid);
        _grid.AllowUserToAddRows = false;
        _grid.ReadOnly = true;
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "PassId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "PassNo", HeaderText = "무료번호", FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("맑은 고딕", 10f, FontStyle.Bold) } },
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "이름", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "인원", FillWeight = 6,
                DefaultCellStyle = GridTheme.CenterStyle },
            new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "전화번호", FillWeight = 14 },
            new DataGridViewTextBoxColumn { Name = "Reason", HeaderText = "사유", FillWeight = 9,
                DefaultCellStyle = GridTheme.CenterStyle },
            new DataGridViewTextBoxColumn { Name = "IssuedDate", HeaderText = "발급일", FillWeight = 10,
                DefaultCellStyle = GridTheme.CenterStyle },
            new DataGridViewTextBoxColumn { Name = "UsedDate", HeaderText = "사용일", FillWeight = 10,
                DefaultCellStyle = GridTheme.CenterStyle },
            new DataGridViewButtonColumn { Name = "UseBtn", HeaderText = "관리", FillWeight = 10,
                FlatStyle = FlatStyle.Flat }
        );

        _grid.CellContentClick += Grid_CellContentClick;
        _grid.KeyDown += Grid_KeyDown;

        // ========== 레이아웃 조립 ==========
        Controls.Add(_grid);
        Controls.Add(inputBox);
        Controls.Add(topBar);

        _ = LoadAsync();
    }

    // ========== 발급 ==========
    private async void BtnIssue_Click(object? sender, EventArgs e)
    {
        var name = _txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ToastNotification.Show("이름을 입력하세요.", ToastType.Warning);
            _txtName.Focus();
            return;
        }

        try
        {
            var passNumber = await _repo.GetNextPassNumberAsync();
            var reasonCode = _cmbReason.SelectedIndex switch
            {
                0 => "record",
                1 => "device",
                _ => "other"
            };

            var pass = new FreePass
            {
                PassNumber = passNumber,
                CustomerName = name,
                Headcount = (int)_numCount.Value,
                Phone = string.IsNullOrWhiteSpace(_txtPhone.Text) ? null : _txtPhone.Text.Trim(),
                Reason = reasonCode,
                Note = string.IsNullOrWhiteSpace(_txtNote.Text) ? null : _txtNote.Text.Trim(),
                IssuedDate = DateTime.Today.ToString("yyyy-MM-dd")
            };

            await _repo.InsertAsync(pass);
            ToastNotification.Show($"무료이용권 {passNumber} 발급 완료.", ToastType.Success);

            // 입력 초기화
            _txtName.Clear();
            _numCount.Value = 2;
            _txtPhone.Clear();
            _cmbReason.SelectedIndex = 0;
            _txtNote.Clear();
            _txtName.Focus();

            await LoadAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"발급 실패: {ex.Message}", ToastType.Error);
        }
    }

    // ========== 사용 체크 ==========
    private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "UseBtn") return;

        var row = _grid.Rows[e.RowIndex];
        var isUsed = row.Cells["UsedDate"].Value?.ToString();
        if (!string.IsNullOrEmpty(isUsed)) return; // 이미 사용됨

        var id = (int)row.Cells["PassId"].Value;
        var passNo = row.Cells["PassNo"].Value?.ToString();

        try
        {
            await _repo.MarkUsedAsync(id);
            ToastNotification.Show($"{passNo} 사용 처리 완료.", ToastType.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"사용 처리 실패: {ex.Message}", ToastType.Error);
        }
    }

    // ========== 삭제 (Delete 키) ==========
    private async void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Delete || _grid.CurrentRow == null) return;

        var id = (int)_grid.CurrentRow.Cells["PassId"].Value;
        var passNo = _grid.CurrentRow.Cells["PassNo"].Value?.ToString();

        if (MessageBox.Show($"{passNo} 이용권을 삭제하시겠습니까?", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        await _repo.DeleteAsync(id);
        ToastNotification.Show($"{passNo} 삭제 완료.", ToastType.Success);
        await LoadAsync();
    }

    // ========== 데이터 로드 ==========
    private async Task LoadAsync()
    {
        try
        {
            var items = _chkAll.Checked
                ? await _repo.GetAllAsync()
                : await _repo.GetByMonthAsync(_dtpMonth.Value.ToString("yyyy-MM"));

            _grid.Rows.Clear();

            foreach (var p in items)
            {
                var idx = _grid.Rows.Add();
                var row = _grid.Rows[idx];

                row.Cells["PassId"].Value = p.Id;
                row.Cells["PassNo"].Value = p.PassNumber;
                row.Cells["Name"].Value = p.CustomerName;
                row.Cells["Count"].Value = $"{p.Headcount}명";
                row.Cells["Phone"].Value = p.Phone ?? "";
                row.Cells["IssuedDate"].Value = FormatDate(p.IssuedDate);
                row.Cells["UsedDate"].Value = p.IsUsed ? FormatDate(p.UsedDate) : "";

                // 사유 태그
                var (reasonText, reasonBg, reasonFg) = p.Reason switch
                {
                    "record" => ("최고기록", ColorPalette.PaymentCard.Bg, ColorPalette.PaymentCard.Fg),
                    "device" => ("장치보상", ColorPalette.AccentOrange.Light, ColorPalette.AccentOrange.Main),
                    _ => ("기타", ColorPalette.SubtleBg, ColorPalette.TextTertiary)
                };
                row.Cells["Reason"].Value = reasonText;
                row.Cells["Reason"].Style.BackColor = reasonBg;
                row.Cells["Reason"].Style.ForeColor = reasonFg;
                row.Cells["Reason"].Style.Font = new Font("맑은 고딕", 9f, FontStyle.Bold);

                // 사용 버튼
                if (p.IsUsed)
                {
                    row.Cells["UseBtn"].Value = "✓ 사용됨";
                    row.Cells["UseBtn"].Style.BackColor = ColorPalette.SuccessLight;
                    row.Cells["UseBtn"].Style.ForeColor = ColorPalette.Success;

                    // 사용된 행은 약간 흐리게
                    for (var c = 0; c < _grid.Columns.Count - 1; c++)
                        row.Cells[c].Style.ForeColor = ColorPalette.TextTertiary;
                }
                else
                {
                    row.Cells["UseBtn"].Value = "사용체크";
                    row.Cells["UseBtn"].Style.BackColor = ColorPalette.Primary;
                    row.Cells["UseBtn"].Style.ForeColor = ColorPalette.TextWhite;
                }
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    // ========== 헬퍼 ==========
    private static string FormatDate(string? date) =>
        DateTime.TryParse(date, out var d) ? d.ToString("MM/dd") : date ?? "";

    private static Label MakeLabel(string text, int width, Font font, int leftMargin = 0) => new()
    {
        Text = text,
        Size = new Size(width, 25),
        Font = font,
        TextAlign = ContentAlignment.MiddleRight,
        Margin = new Padding(leftMargin, 0, 0, 0)
    };
}
