using System.Drawing;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class InventoryTab : UserControl
{
    private readonly IInventoryRepository _repo;
    private readonly DataGridView _grid;

    public InventoryTab(IInventoryRepository repo)
    {
        _repo = repo;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(15);

        var topBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 5) };
        topBar.Controls.Add(new Label
        {
            Text = "물품 관리", Size = new Size(120, 32),
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
        });

        var btnAdd = ButtonFactory.CreatePrimary("+ 물품 추가", 110);
        btnAdd.Margin = new Padding(10, 0, 0, 0);
        btnAdd.Click += BtnAdd_Click;
        topBar.Controls.Add(btnAdd);

        _grid = new DataGridView { Dock = DockStyle.Fill };
        GridTheme.ApplyTheme(_grid);

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", Visible = false },
            new DataGridViewTextBoxColumn { HeaderText = "물품명", FillWeight = 25, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "보유기준", FillWeight = 12, ReadOnly = true,
                DefaultCellStyle = new() { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "현재수량", FillWeight = 12,
                DefaultCellStyle = new() { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "부족", FillWeight = 10, ReadOnly = true,
                DefaultCellStyle = new() { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { HeaderText = "카테고리", FillWeight = 15, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "비고", FillWeight = 20, ReadOnly = true });

        _grid.CellEndEdit += Grid_CellEndEdit;
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete && _grid.CurrentRow != null)
                _ = DeleteItemAsync((int)_grid.CurrentRow.Cells["Id"].Value);
        };

        Controls.Add(_grid);
        Controls.Add(topBar);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = (await _repo.GetAllAsync()).ToList();
        _grid.Rows.Clear();
        foreach (var item in items)
        {
            var idx = _grid.Rows.Add(item.Id, item.ItemName, item.RequiredQty,
                item.CurrentQty, null, item.Category, item.Note);
            UpdateShortageCell(_grid.Rows[idx], item);
        }

        if (items.Count == 0)
            ToastNotification.Show("📦 등록된 물품이 없습니다. '+ 물품 추가'로 시작하세요.", ToastType.Info);
    }

    private static void UpdateShortageCell(DataGridViewRow row, InventoryItem item)
    {
        var shortage = item.ShortageQty;
        row.Cells[4].Value = shortage <= 0 ? "✓ 0" : $"▼ {shortage}";
        row.Cells[4].Style.ForeColor = shortage > 0 ? ColorPalette.Danger : ColorPalette.Success;
    }

    private async void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var dlg = new InventoryEditDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        await _repo.InsertAsync(new InventoryItem
        {
            ItemName = dlg.ItemName, RequiredQty = dlg.RequiredQty,
            CurrentQty = dlg.CurrentQty, Category = dlg.ItemCategory, Note = dlg.ItemNote
        });
        ToastNotification.Show("물품 추가 완료.", ToastType.Success);
        await LoadAsync();
    }

    private async void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].HeaderText != "현재수량") return;
        var id = (int)_grid.Rows[e.RowIndex].Cells["Id"].Value;
        if (int.TryParse(_grid.Rows[e.RowIndex].Cells[3].Value?.ToString(), out var qty))
        {
            await _repo.UpdateQuantityAsync(id, qty);
            var reqQty = int.Parse(_grid.Rows[e.RowIndex].Cells[2].Value?.ToString() ?? "0");
            UpdateShortageCell(_grid.Rows[e.RowIndex], new InventoryItem { RequiredQty = reqQty, CurrentQty = qty });
        }
    }

    private async Task DeleteItemAsync(int id)
    {
        var itemName = _grid.CurrentRow?.Cells[1]?.Value?.ToString() ?? "물품";
        if (MessageBox.Show($"'{itemName}'을(를) 삭제하시겠습니까?", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        await _repo.DeleteAsync(id);
        ToastNotification.Show($"'{itemName}' 삭제 완료.", ToastType.Success);
        await LoadAsync();
    }
}

internal class InventoryEditDialog : Form
{
    private readonly TextBox _txtName, _txtNote;
    private readonly NumericUpDown _numReq, _numCur;
    private readonly ComboBox _cmbCat;

    public string ItemName => _txtName.Text.Trim();
    public int RequiredQty => (int)_numReq.Value;
    public int CurrentQty => (int)_numCur.Value;
    public string? ItemCategory => _cmbCat.Text;
    public string? ItemNote => _txtNote.Text.Trim();

    public InventoryEditDialog()
    {
        Text = "물품 추가"; Size = new Size(360, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false; MinimizeBox = false; Font = new Font("맑은 고딕", 10f);

        var y = 15;
        AddField("물품명:", _txtName = new TextBox(), ref y);
        AddField("보유기준:", _numReq = new NumericUpDown { Maximum = 9999, Size = new Size(100, 25) }, ref y);
        AddField("현재수량:", _numCur = new NumericUpDown { Maximum = 9999, Size = new Size(100, 25) }, ref y);
        AddField("카테고리:", _cmbCat = new ComboBox
        {
            Size = new Size(160, 25), DropDownStyle = ComboBoxStyle.DropDown,
            Items = { "사무용품", "음료", "청소용품", "장비부품", "기타" }
        }, ref y);
        AddField("비고:", _txtNote = new TextBox(), ref y);

        var btnOk = ButtonFactory.CreatePrimary("추가", 80);
        btnOk.Location = new Point(150, y);
        btnOk.DialogResult = DialogResult.OK;
        Controls.Add(btnOk);

        var btnCancel = ButtonFactory.CreateGhost("취소", 80);
        btnCancel.Location = new Point(240, y);
        btnCancel.DialogResult = DialogResult.Cancel;
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        // Tab 순서
        _txtName.TabIndex = 0;
        _numReq.TabIndex = 1;
        _numCur.TabIndex = 2;
        _cmbCat.TabIndex = 3;
        _txtNote.TabIndex = 4;
        btnOk.TabIndex = 5;
    }

    private void AddField(string label, Control ctrl, ref int y)
    {
        Controls.Add(new Label { Text = label, Location = new Point(20, y + 2), Size = new Size(70, 22) });
        ctrl.Location = new Point(95, y);
        if (ctrl.Width < 200 && ctrl is not NumericUpDown) ctrl.Size = new Size(230, 25);
        Controls.Add(ctrl);
        y += 35;
    }
}
