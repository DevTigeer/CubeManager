using System.Drawing;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class ReservationSalesTab : UserControl
{
    private readonly ISalesService _salesService;
    private readonly DateTimePicker _dtpDate;
    private readonly DataGridView _gridItems;
    private readonly Label _lblCard, _lblCash, _lblTransfer, _lblTotal, _lblCashBalance;
    private string _currentDate;

    public ReservationSalesTab(ISalesService salesService)
    {
        _salesService = salesService;
        _currentDate = DateTime.Today.ToString("yyyy-MM-dd");
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Padding = new Padding(15);

        // Header + Date picker
        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 45,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 5)
        };

        topPanel.Controls.Add(new Label
        {
            Text = "예약/매출 관리",
            Font = new Font("맑은 고딕", 14f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Size = new Size(160, 32),
            TextAlign = ContentAlignment.MiddleLeft
        });

        _dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Size = new Size(130, 28),
            Value = DateTime.Today
        };
        _dtpDate.ValueChanged += (_, _) =>
        {
            _currentDate = _dtpDate.Value.ToString("yyyy-MM-dd");
            _ = LoadDataAsync();
        };
        topPanel.Controls.Add(_dtpDate);

        var btnAddRevenue = CreateBtn("+ 매출", ColorPalette.Primary);
        btnAddRevenue.Click += (_, _) => AddItem("revenue");

        var btnAddExpense = CreateBtn("+ 지출", ColorPalette.Danger);
        btnAddExpense.Click += (_, _) => AddItem("expense");

        topPanel.Controls.Add(btnAddRevenue);
        topPanel.Controls.Add(btnAddExpense);

        // Sale items grid
        _gridItems = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false, ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false, BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle, GridColor = ColorPalette.Border,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("맑은 고딕", 10f) },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                BackColor = ColorPalette.Background
            }
        };
        _gridItems.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", Visible = false },
            new DataGridViewTextBoxColumn { HeaderText = "#", Width = 35, FillWeight = 5 },
            new DataGridViewTextBoxColumn { HeaderText = "항목", FillWeight = 35 },
            new DataGridViewTextBoxColumn { HeaderText = "금액", FillWeight = 20,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { HeaderText = "결제", FillWeight = 15 },
            new DataGridViewTextBoxColumn { HeaderText = "구분", FillWeight = 10 });
        _gridItems.KeyDown += GridItems_KeyDown;

        // Summary panel
        var summaryPanel = new Panel
        {
            Dock = DockStyle.Bottom, Height = 100,
            BackColor = ColorPalette.Background,
            Padding = new Padding(15, 10, 15, 10)
        };

        var summaryFont = new Font("맑은 고딕", 11f);
        var boldFont = new Font("맑은 고딕", 12f, FontStyle.Bold);

        _lblCard = CreateSummaryLabel(summaryFont, 0);
        _lblCash = CreateSummaryLabel(summaryFont, 25);
        _lblTransfer = CreateSummaryLabel(summaryFont, 50);
        _lblTotal = new Label
        {
            Location = new Point(15, 75), Size = new Size(400, 22),
            Font = boldFont, ForeColor = ColorPalette.Text
        };
        _lblCashBalance = new Label
        {
            Location = new Point(430, 75), Size = new Size(300, 22),
            Font = boldFont, ForeColor = ColorPalette.Primary
        };

        summaryPanel.Controls.AddRange([_lblCard, _lblCash, _lblTransfer, _lblTotal, _lblCashBalance]);

        Controls.Add(_gridItems);
        Controls.Add(summaryPanel);
        Controls.Add(topPanel);

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var items = (await _salesService.GetSaleItemsAsync(_currentDate)).ToList();
            _gridItems.Rows.Clear();

            var num = 1;
            foreach (var item in items)
            {
                var idx = _gridItems.Rows.Add();
                var row = _gridItems.Rows[idx];
                row.Cells[0].Value = item.Id;
                row.Cells[1].Value = num++;
                row.Cells[2].Value = item.Description;
                row.Cells[3].Value = (item.Category == "expense" ? "-" : "") + item.Amount.ToString("N0");
                row.Cells[4].Value = item.PaymentType switch
                {
                    "card" => "카드", "cash" => "현금", "transfer" => "계좌", _ => item.PaymentType
                };
                row.Cells[5].Value = item.Category == "revenue" ? "매출" : "지출";

                var tagColor = item.PaymentType switch
                {
                    "card" => ColorPalette.PaymentCard,
                    "cash" => ColorPalette.PaymentCash,
                    "transfer" => ColorPalette.PaymentTransfer,
                    _ => (ColorPalette.Background, ColorPalette.Text)
                };
                if (item.Category == "expense") tagColor = ColorPalette.PaymentExpense;
                row.Cells[4].Style.BackColor = tagColor.Item1;
                row.Cells[4].Style.ForeColor = tagColor.Item2;
            }

            // Summary
            var daily = await _salesService.GetDailySalesAsync(_currentDate);
            _lblCard.Text = $"카드: {daily?.CardAmount ?? 0:N0}원";
            _lblCash.Text = $"현금: {daily?.CashAmount ?? 0:N0}원";
            _lblTransfer.Text = $"계좌: {daily?.TransferAmount ?? 0:N0}원";
            _lblTotal.Text = $"총 매출: {daily?.TotalRevenue ?? 0:N0}원";

            var balance = await _salesService.GetCashBalanceAsync(_currentDate);
            _lblCashBalance.Text = balance != null
                ? $"현금 잔액: {balance.ClosingBalance:N0}원 (전일 {balance.OpeningBalance:N0} + {balance.CashIn:N0} - {balance.CashOut:N0})"
                : "현금 잔액: -";
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private async void AddItem(string category)
    {
        using var dlg = new SaleItemDialog(category);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _salesService.AddSaleItemAsync(_currentDate,
                dlg.ItemDescription, dlg.Amount, dlg.PaymentType, category);
            ToastNotification.Show(
                category == "revenue" ? "매출 추가 완료." : "지출 추가 완료.",
                ToastType.Success);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private async void GridItems_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Delete || _gridItems.CurrentRow == null) return;
        var id = (int)_gridItems.CurrentRow.Cells[0].Value;
        if (MessageBox.Show("이 항목을 삭제하시겠습니까?", "확인",
                MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        await _salesService.DeleteSaleItemAsync(_currentDate, id);
        await LoadDataAsync();
    }

    private static Button CreateBtn(string text, Color color) => new()
    {
        Text = text, Size = new Size(80, 32),
        BackColor = color, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 10f),
        Margin = new Padding(10, 0, 0, 0)
    };

    private static Label CreateSummaryLabel(Font font, int yOff) => new()
    {
        Location = new Point(15 + yOff / 25 * 200, 10 + (yOff % 25 == 0 ? 0 : 0)),
        Size = new Size(180, 22), Font = font
    };
}

internal class SaleItemDialog : Form
{
    public string ItemDescription => _txtDesc.Text.Trim();
    public int Amount => (int)_numAmount.Value;
    public string PaymentType => _cmbPayment.Text switch
    {
        "카드" => "card", "현금" => "cash", "계좌" => "transfer", _ => "card"
    };

    private readonly TextBox _txtDesc;
    private readonly NumericUpDown _numAmount;
    private readonly ComboBox _cmbPayment;

    public SaleItemDialog(string category)
    {
        Text = category == "revenue" ? "매출 추가" : "지출 추가";
        Size = new Size(360, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false; MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);

        var y = 15;
        Controls.Add(new Label { Text = "항목:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        _txtDesc = new TextBox { Location = new Point(90, y), Size = new Size(230, 25) };
        Controls.Add(_txtDesc);

        y += 38;
        Controls.Add(new Label { Text = "금액:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        _numAmount = new NumericUpDown
        {
            Location = new Point(90, y), Size = new Size(150, 25),
            Maximum = 100_000_000, Minimum = 1, Increment = 1000, ThousandsSeparator = true
        };
        Controls.Add(_numAmount);

        y += 38;
        Controls.Add(new Label { Text = "결제:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        _cmbPayment = new ComboBox
        {
            Location = new Point(90, y), Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Items = { "카드", "현금", "계좌" }
        };
        _cmbPayment.SelectedIndex = category == "expense" ? 1 : 0;
        Controls.Add(_cmbPayment);

        y += 45;
        var btnOk = new Button
        {
            Text = "추가", Location = new Point(150, y), Size = new Size(80, 35),
            BackColor = ColorPalette.Primary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;
        Controls.Add(btnOk);
        Controls.Add(new Button { Text = "취소", Location = new Point(240, y), Size = new Size(80, 35), DialogResult = DialogResult.Cancel });
        AcceptButton = btnOk;
    }
}
