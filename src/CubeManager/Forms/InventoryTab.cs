using System.Drawing;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class InventoryTab : UserControl
{
    private readonly IInventoryRepository _repo;
    private readonly DataGridView _grid;
    private readonly Button _btnEdit;
    private readonly Button _btnDelete;
    private readonly Button _btnPlus;
    private readonly Button _btnMinus;

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

        _btnEdit = ButtonFactory.CreateSecondary("수정", 70);
        _btnEdit.Margin = new Padding(6, 0, 0, 0);
        _btnEdit.Click += async (_, _) => await EditSelectedAsync();
        topBar.Controls.Add(_btnEdit);

        _btnPlus = ButtonFactory.CreateGhost("＋1", 50);
        _btnPlus.Margin = new Padding(6, 0, 0, 0);
        _btnPlus.Click += async (_, _) => await AdjustQtyAsync(1);
        topBar.Controls.Add(_btnPlus);

        _btnMinus = ButtonFactory.CreateGhost("－1", 50);
        _btnMinus.Margin = new Padding(2, 0, 0, 0);
        _btnMinus.Click += async (_, _) => await AdjustQtyAsync(-1);
        topBar.Controls.Add(_btnMinus);

        _btnDelete = ButtonFactory.CreateDanger("삭제", 70);
        _btnDelete.Margin = new Padding(6, 0, 0, 0);
        _btnDelete.Click += async (_, _) => await DeleteSelectedAsync();
        topBar.Controls.Add(_btnDelete);

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
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].HeaderText == "현재수량") return;
            await EditSelectedAsync();
        };
        _grid.SelectionChanged += (_, _) => UpdateButtonStates();
        _grid.MouseDown += Grid_MouseDown;
        _grid.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Delete) await DeleteSelectedAsync();
        };

        _grid.ContextMenuStrip = BuildContextMenu();

        Controls.Add(_grid);
        Controls.Add(topBar);
        UpdateButtonStates();
        _ = LoadAsync();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("수정", null, async (_, _) => await EditSelectedAsync());
        menu.Items.Add("＋1", null, async (_, _) => await AdjustQtyAsync(1));
        menu.Items.Add("－1", null, async (_, _) => await AdjustQtyAsync(-1));
        menu.Items.Add(new ToolStripSeparator());
        var del = new ToolStripMenuItem("삭제") { ForeColor = ColorPalette.Danger };
        del.Click += async (_, _) => await DeleteSelectedAsync();
        menu.Items.Add(del);
        menu.Opening += (_, e) => { if (_grid.CurrentRow == null) e.Cancel = true; };
        return menu;
    }

    private void Grid_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        var hit = _grid.HitTest(e.X, e.Y);
        if (hit.RowIndex < 0) return;
        _grid.ClearSelection();
        _grid.Rows[hit.RowIndex].Selected = true;
        _grid.CurrentCell = _grid.Rows[hit.RowIndex].Cells[1];
    }

    private void UpdateButtonStates()
    {
        var hasRow = _grid.CurrentRow != null;
        _btnEdit.Enabled = hasRow;
        _btnDelete.Enabled = hasRow;
        _btnPlus.Enabled = hasRow;
        _btnMinus.Enabled = hasRow;
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

        UpdateButtonStates();

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

    private async Task EditSelectedAsync()
    {
        var row = _grid.CurrentRow;
        if (row == null) return;

        var existing = new InventoryItem
        {
            Id = (int)row.Cells["Id"].Value,
            ItemName = row.Cells[1].Value?.ToString() ?? "",
            RequiredQty = int.TryParse(row.Cells[2].Value?.ToString(), out var rq) ? rq : 0,
            CurrentQty = int.TryParse(row.Cells[3].Value?.ToString(), out var cq) ? cq : 0,
            Category = row.Cells[5].Value?.ToString(),
            Note = row.Cells[6].Value?.ToString()
        };

        using var dlg = new InventoryEditDialog(existing);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var updated = new InventoryItem
        {
            Id = existing.Id,
            ItemName = dlg.ItemName,
            RequiredQty = dlg.RequiredQty,
            CurrentQty = dlg.CurrentQty,
            Category = dlg.ItemCategory,
            Note = dlg.ItemNote
        };

        await _repo.UpdateAsync(updated);
        ToastNotification.Show($"'{updated.ItemName}' 수정 완료.", ToastType.Success);
        await LoadAsync();
    }

    private async Task AdjustQtyAsync(int delta)
    {
        var row = _grid.CurrentRow;
        if (row == null) return;
        var id = (int)row.Cells["Id"].Value;
        if (!int.TryParse(row.Cells[3].Value?.ToString(), out var cur)) return;
        var newQty = Math.Max(0, cur + delta);
        if (newQty == cur) return;

        await _repo.UpdateQuantityAsync(id, newQty);
        row.Cells[3].Value = newQty;
        var reqQty = int.TryParse(row.Cells[2].Value?.ToString(), out var rq) ? rq : 0;
        UpdateShortageCell(row, new InventoryItem { RequiredQty = reqQty, CurrentQty = newQty });
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

    private async Task DeleteSelectedAsync()
    {
        var row = _grid.CurrentRow;
        if (row == null) return;
        var id = (int)row.Cells["Id"].Value;
        var itemName = row.Cells[1].Value?.ToString() ?? "물품";
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

    public InventoryEditDialog(InventoryItem? existing = null)
    {
        var isEdit = existing != null;
        Text = isEdit ? "물품 수정" : "물품 추가";
        Size = new Size(360, 280);
        FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterParent;
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

        if (existing != null)
        {
            _txtName.Text = existing.ItemName;
            _numReq.Value = Math.Clamp(existing.RequiredQty, 0, 9999);
            _numCur.Value = Math.Clamp(existing.CurrentQty, 0, 9999);
            _cmbCat.Text = existing.Category ?? "";
            _txtNote.Text = existing.Note ?? "";
        }

        var btnOk = ButtonFactory.CreatePrimary(isEdit ? "저장" : "추가", 80);
        btnOk.Location = new Point(150, y);
        btnOk.DialogResult = DialogResult.OK;
        Controls.Add(btnOk);

        var btnCancel = ButtonFactory.CreateGhost("취소", 80);
        btnCancel.Location = new Point(240, y);
        btnCancel.DialogResult = DialogResult.Cancel;
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

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
