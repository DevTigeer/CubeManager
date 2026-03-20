using System.Drawing;
using CubeManager.Controls;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class ReservationSalesTab : UserControl
{
    private readonly ISalesService _salesService;
    private readonly IReservationScraperService _scraperService;
    private readonly DateTimePicker _dtpDate;
    private readonly DataGridView _gridMain;       // 예약+결제 통합
    private readonly DataGridView _gridExpense;    // 지출 내역
    private readonly CheckBox _chkAutoRefresh;
    private readonly Label _lblLastFetch;
    private readonly System.Windows.Forms.Timer _autoRefreshTimer;
    private readonly SummaryCardRow _summaryCards;

    // 우측 요약 라벨들
    private readonly Label _lblSumCard, _lblSumCash, _lblSumTransfer;
    private readonly Label _lblSumTotal, _lblSumExpense, _lblSumCashBalance, _lblSumBalanceDetail;

    private string _currentDate;
    private List<Reservation> _reservations = [];
    private List<SaleItem> _existingSaleItems = [];
    private int _sortState; // 0=원래, 1=오름차순, 2=내림차순

    public ReservationSalesTab(ISalesService salesService, IReservationScraperService scraperService)
    {
        _salesService = salesService;
        _scraperService = scraperService;
        _currentDate = DateTime.Today.ToString("yyyy-MM-dd");
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Padding = new Padding(15);

        // ========== 1. 상단 툴바 ==========
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
            Size = new Size(150, 32),
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
            _ = LoadAllAsync();
        };
        topPanel.Controls.Add(_dtpDate);

        var btnFetch = CreateBtn("웹 조회", ColorPalette.Info);
        btnFetch.Click += BtnFetchWeb_Click;
        topPanel.Controls.Add(btnFetch);

        _chkAutoRefresh = new CheckBox
        {
            Text = "자동(2분)", Checked = true,
            Size = new Size(100, 28), Margin = new Padding(5, 4, 0, 0),
            Font = new Font("맑은 고딕", 9f)
        };
        _chkAutoRefresh.CheckedChanged += (_, _) =>
        {
            if (_autoRefreshTimer != null)
                _autoRefreshTimer.Enabled = _chkAutoRefresh.Checked;
        };
        topPanel.Controls.Add(_chkAutoRefresh);

        var btnSort = CreateBtn("시간순 ↑", ColorPalette.TextSecondary);
        btnSort.Size = new Size(90, 32);
        btnSort.Click += (_, _) => ToggleSort(btnSort);
        topPanel.Controls.Add(btnSort);

        var btnAddWalkin = CreateBtn("+ 워크인", ColorPalette.Primary);
        btnAddWalkin.Click += (_, _) => AddWalkinReservation();
        topPanel.Controls.Add(btnAddWalkin);

        var btnDelete = CreateBtn("매출 삭제", ColorPalette.Danger);
        btnDelete.Click += (_, _) => DeleteRevenueItem();
        topPanel.Controls.Add(btnDelete);

        _lblLastFetch = new Label
        {
            Text = "", Size = new Size(180, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ColorPalette.TextTertiary,
            Font = new Font("맑은 고딕", 9f),
            Margin = new Padding(10, 4, 0, 0)
        };
        topPanel.Controls.Add(_lblLastFetch);

        // ========== 2. Summary Cards ==========
        _summaryCards = new SummaryCardRow();
        _summaryCards.AddCard("오늘 예약", "0건", ColorPalette.AccentBlue.Main, ColorPalette.AccentBlue.Light);
        _summaryCards.AddCard("총 매출", "₩0", ColorPalette.AccentGreen.Main, ColorPalette.AccentGreen.Light);
        _summaryCards.AddCard("카드 매출", "₩0", ColorPalette.AccentBlue.Main, ColorPalette.AccentBlue.Light);
        _summaryCards.AddCard("현금 잔액", "₩0", ColorPalette.AccentOrange.Main, ColorPalette.AccentOrange.Light);

        // ========== 3. 통합 그리드 (예약+결제) ==========
        _gridMain = new DataGridView { Dock = DockStyle.Fill };
        GridTheme.ApplyTheme(_gridMain);
        _gridMain.AllowUserToAddRows = false;
        _gridMain.ReadOnly = false;
        _gridMain.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "ResId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "시간", FillWeight = 10, ReadOnly = true },
            new DataGridViewTextBoxColumn { Name = "Theme", HeaderText = "테마", FillWeight = 12, ReadOnly = true },
            new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "예약자", FillWeight = 10, ReadOnly = true },
            new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "연락처", FillWeight = 13, ReadOnly = true },
            new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "인원", FillWeight = 6, ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "상태", FillWeight = 8, ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "CardAmt", HeaderText = "카드금액", FillWeight = 13,
                DefaultCellStyle = GridTheme.AmountStyle },
            new DataGridViewTextBoxColumn { Name = "CashAmt", HeaderText = "현금금액", FillWeight = 13,
                DefaultCellStyle = GridTheme.AmountStyle },
            new DataGridViewTextBoxColumn { Name = "TransferAmt", HeaderText = "계좌금액", FillWeight = 13,
                DefaultCellStyle = GridTheme.AmountStyle }
        );
        _gridMain.CellEndEdit += GridMain_CellEndEdit;
        _gridMain.CellMouseClick += GridMain_CellMouseClick;

        var lblMainHeader = new Label
        {
            Text = "예약 & 결제 현황", Dock = DockStyle.Top, Height = 25,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary, Padding = new Padding(0, 5, 0, 0)
        };

        // ========== 4. 하단 패널 (지출 + 요약) ==========
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom, Height = 200,
            BackColor = ColorPalette.Background,
            Padding = new Padding(0)
        };

        // 4-1. 지출 그리드 (좌측 60%)
        var expensePanel = new Panel
        {
            Dock = DockStyle.Left, Width = 1, // Resize에서 설정
            Padding = new Padding(10)
        };

        var lblExpenseHeader = new Label
        {
            Text = "지출 내역", Dock = DockStyle.Top, Height = 25,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary
        };

        _gridExpense = new DataGridView { Dock = DockStyle.Fill };
        GridTheme.ApplyTheme(_gridExpense);
        _gridExpense.AllowUserToAddRows = false;
        _gridExpense.ReadOnly = true;
        _gridExpense.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "ExpId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "ExpNo", HeaderText = "#", Width = 35, FillWeight = 5 },
            new DataGridViewTextBoxColumn { Name = "ExpDesc", HeaderText = "항목", FillWeight = 40 },
            new DataGridViewTextBoxColumn { Name = "ExpAmt", HeaderText = "금액", FillWeight = 25,
                DefaultCellStyle = GridTheme.AmountStyle },
            new DataGridViewTextBoxColumn { Name = "ExpPay", HeaderText = "결제", FillWeight = 20 }
        );
        _gridExpense.KeyDown += GridExpense_KeyDown;

        var btnAddExpense = CreateBtn("+ 지출 추가", ColorPalette.Danger);
        btnAddExpense.Dock = DockStyle.Bottom;
        btnAddExpense.Height = 32;
        btnAddExpense.Width = 0; // Fill
        btnAddExpense.Click += (_, _) => AddItem("expense");

        expensePanel.Controls.Add(_gridExpense);
        expensePanel.Controls.Add(btnAddExpense);
        expensePanel.Controls.Add(lblExpenseHeader);

        // 4-2. 요약 패널 (우측 40%)
        var summaryRight = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15, 10, 15, 10),
            BackColor = ColorPalette.Surface
        };

        var sf = new Font("맑은 고딕", 11f);
        var bf = new Font("맑은 고딕", 12f, FontStyle.Bold);
        var y = 8;
        _lblSumCard = MakeSummaryLabel(summaryRight, "카드", ColorPalette.PaymentCard.Item2, sf, ref y);
        _lblSumCash = MakeSummaryLabel(summaryRight, "현금", ColorPalette.PaymentCash.Item2, sf, ref y);
        _lblSumTransfer = MakeSummaryLabel(summaryRight, "계좌", ColorPalette.PaymentTransfer.Item2, sf, ref y);
        y += 5;
        summaryRight.Controls.Add(new Label { Location = new Point(10, y), Size = new Size(220, 1), BackColor = ColorPalette.Border });
        y += 8;
        _lblSumTotal = MakeSummaryLabel(summaryRight, "총매출", ColorPalette.Text, bf, ref y);
        _lblSumExpense = MakeSummaryLabel(summaryRight, "총지출", ColorPalette.Danger, sf, ref y);
        y += 5;
        summaryRight.Controls.Add(new Label { Location = new Point(10, y), Size = new Size(220, 1), BackColor = ColorPalette.Border });
        y += 8;
        _lblSumCashBalance = MakeSummaryLabel(summaryRight, "현금잔액", ColorPalette.Primary, bf, ref y);
        _lblSumBalanceDetail = new Label
        {
            Location = new Point(15, y), Size = new Size(220, 20),
            Font = new Font("맑은 고딕", 8.5f), ForeColor = ColorPalette.TextTertiary
        };
        summaryRight.Controls.Add(_lblSumBalanceDetail);

        bottomPanel.Controls.Add(summaryRight);
        bottomPanel.Controls.Add(expensePanel);

        // 구분선
        var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ColorPalette.Border };

        // ========== 레이아웃 조립 (역순) ==========
        Controls.Add(_gridMain);
        Controls.Add(lblMainHeader);
        Controls.Add(divider);
        Controls.Add(bottomPanel);
        Controls.Add(_summaryCards);
        Controls.Add(topPanel);

        // 하단 패널 크기 조정
        bottomPanel.Resize += (_, _) =>
        {
            expensePanel.Width = (int)(bottomPanel.Width * 0.6);
        };

        // ========== 자동 갱신 타이머 ==========
        _autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 120_000, Enabled = true };
        _autoRefreshTimer.Tick += async (_, _) => await FetchWebSilently();

        // 초기 로드
        _ = LoadAllAsync();
    }

    // ===== 탭 활성/비활성 시 타이머 제어 =====
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (_autoRefreshTimer != null)
            _autoRefreshTimer.Enabled = Visible && _chkAutoRefresh.Checked;
    }

    // ===== 웹 조회 (수동) =====
    private async void BtnFetchWeb_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn) { btn.Enabled = false; btn.Text = "조회 중..."; }
        try
        {
            await FetchWebAndUpdate();
            ToastNotification.Show($"예약 {_reservations.Count}건 조회 완료.", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"조회 실패: {ex.Message}", ToastType.Error);
        }
        finally
        {
            if (sender is Button btn2) { btn2.Enabled = true; btn2.Text = "웹 조회"; }
        }
    }

    // ===== 웹 조회 (자동, 조용히) =====
    private async Task FetchWebSilently()
    {
        try
        {
            var prevCount = _reservations.Count;
            await FetchWebAndUpdate();
            if (_reservations.Count != prevCount && _reservations.Count > 0)
                ToastNotification.Show($"예약 {_reservations.Count}건 갱신됨.", ToastType.Info);
        }
        catch
        {
            // 자동 갱신 실패는 조용히 무시 (로그만)
        }
    }

    // ===== 웹 스크래핑 + 그리드 갱신 =====
    private async Task FetchWebAndUpdate()
    {
        _reservations = (await _scraperService.FetchReservationsAsync(_dtpDate.Value)).ToList();
        _existingSaleItems = (await _salesService.GetSaleItemsAsync(_currentDate))
            .Where(i => i.Category == "revenue").ToList();
        _lblLastFetch.Text = $"마지막 조회: {DateTime.Now:HH:mm:ss}";
        PopulateMainGrid();
    }

    // ===== 시간순 정렬 토글 =====
    private void ToggleSort(Button btn)
    {
        _sortState = (_sortState + 1) % 3;
        btn.Text = _sortState switch
        {
            0 => "시간순 ↑",
            1 => "시간순 ↓",
            _ => "원래순서"
        };
        PopulateMainGrid();
    }

    // ===== 통합 그리드 데이터 채우기 =====
    private void PopulateMainGrid()
    {
        _gridMain.Rows.Clear();

        var sorted = _sortState switch
        {
            1 => _reservations.OrderBy(r => r.TimeSlot).ToList(),
            2 => _reservations.OrderByDescending(r => r.TimeSlot).ToList(),
            _ => _reservations
        };

        foreach (var r in sorted)
        {
            var idx = _gridMain.Rows.Add();
            var row = _gridMain.Rows[idx];
            row.Cells["ResId"].Value = r.Id;
            row.Cells["Time"].Value = r.TimeSlot;
            row.Cells["Theme"].Value = r.ThemeName;
            row.Cells["Customer"].Value = r.CustomerName;
            row.Cells["Phone"].Value = r.CustomerPhone ?? "";
            row.Cells["Count"].Value = r.Headcount > 0 ? $"{r.Headcount}명" : "";

            // 상태 표시
            var isRemoved = r.Status == "removed";
            var isWalkin = r.Status == "walkin";
            row.Cells["Status"].Value = isRemoved ? "취소" : isWalkin ? "워크인" : "확정";
            if (isRemoved)
            {
                row.Cells["Status"].Style.BackColor = ColorPalette.PaymentExpense.Item1;
                row.Cells["Status"].Style.ForeColor = ColorPalette.PaymentExpense.Item2;
            }
            else if (isWalkin)
            {
                row.Cells["Status"].Style.BackColor = ColorPalette.PaymentTransfer.Item1;
                row.Cells["Status"].Style.ForeColor = ColorPalette.PaymentTransfer.Item2;
            }
            else
            {
                row.Cells["Status"].Style.BackColor = ColorPalette.PaymentCash.Item1;
                row.Cells["Status"].Style.ForeColor = ColorPalette.PaymentCash.Item2;
            }

            // 취소된 행 스타일
            if (isRemoved)
            {
                for (var c = 0; c < row.Cells.Count; c++)
                {
                    row.Cells[c].Style.ForeColor = ColorPalette.TextTertiary;
                    row.Cells[c].Style.Font = new Font("맑은 고딕", 10f, FontStyle.Strikeout);
                }
                // 결제 셀 비활성
                row.Cells["CardAmt"].ReadOnly = true;
                row.Cells["CashAmt"].ReadOnly = true;
                row.Cells["TransferAmt"].ReadOnly = true;
            }

            // 기존 결제 데이터 로드하여 금액 셀 채우기
            LoadExistingPayments(row, r);
        }

        UpdateSummaryCards();
    }

    // ===== 결제 금액 셀 편집 완료 =====
    private async void GridMain_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var colName = _gridMain.Columns[e.ColumnIndex].Name;
        if (colName is not ("CardAmt" or "CashAmt" or "TransferAmt")) return;

        var row = _gridMain.Rows[e.RowIndex];
        var cellValue = row.Cells[e.ColumnIndex].Value?.ToString()?.Replace(",", "").Replace("₩", "").Trim();

        if (!int.TryParse(cellValue, out var amount) || amount < 0)
        {
            row.Cells[e.ColumnIndex].Value = null;
            return;
        }

        var paymentType = colName switch
        {
            "CardAmt" => "card",
            "CashAmt" => "cash",
            "TransferAmt" => "transfer",
            _ => "card"
        };

        // 금액이 0이면 셀 초기화
        if (amount == 0)
        {
            row.Cells[e.ColumnIndex].Value = null;
            row.Cells[e.ColumnIndex].Style.BackColor = Color.White;
            return;
        }

        // 결제 태그 색상 적용
        var tagColor = paymentType switch
        {
            "card" => ColorPalette.PaymentCard,
            "cash" => ColorPalette.PaymentCash,
            "transfer" => ColorPalette.PaymentTransfer,
            _ => (Color.White, ColorPalette.Text)
        };
        row.Cells[e.ColumnIndex].Style.BackColor = tagColor.Item1;
        row.Cells[e.ColumnIndex].Style.ForeColor = tagColor.Item2;
        row.Cells[e.ColumnIndex].Value = amount.ToString("N0");

        // 테마명 + 예약자 조합으로 고유 설명 생성 (Upsert 키)
        var theme = row.Cells["Theme"].Value?.ToString() ?? "매출";
        var customer = row.Cells["Customer"].Value?.ToString() ?? "";
        var time = row.Cells["Time"].Value?.ToString() ?? "";
        var desc = $"{time} {theme} {customer}".Trim();

        try
        {
            await _salesService.UpsertSaleItemAsync(_currentDate, desc, amount, paymentType, "revenue");
            await LoadSummaryAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    // ===== 우클릭: 예약 상태 전환 =====
    private void GridMain_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;

        var row = _gridMain.Rows[e.RowIndex];
        var currentStatus = row.Cells["Status"].Value?.ToString();

        var menu = new ContextMenuStrip();

        if (currentStatus == "확정")
        {
            menu.Items.Add("취소(삭제)", null, (_, _) =>
            {
                SetReservationStatus(e.RowIndex, "removed");
            });
        }
        else
        {
            menu.Items.Add("복원", null, (_, _) =>
            {
                SetReservationStatus(e.RowIndex, "confirmed");
            });
        }

        menu.Show(_gridMain, _gridMain.PointToClient(Cursor.Position));
    }

    private void SetReservationStatus(int rowIndex, string status)
    {
        if (rowIndex < 0 || rowIndex >= _reservations.Count) return;

        _reservations[rowIndex].Status = status;
        PopulateMainGrid();
    }

    // ===== 전체 데이터 로드 =====
    private async Task LoadAllAsync()
    {
        await LoadExpenseGridAsync();
        await LoadSummaryAsync();
    }

    // ===== 지출 그리드 로드 =====
    private async Task LoadExpenseGridAsync()
    {
        try
        {
            var items = (await _salesService.GetSaleItemsAsync(_currentDate))
                .Where(i => i.Category == "expense").ToList();

            _gridExpense.Rows.Clear();
            var num = 1;
            foreach (var item in items)
            {
                var idx = _gridExpense.Rows.Add();
                var row = _gridExpense.Rows[idx];
                row.Cells["ExpId"].Value = item.Id;
                row.Cells["ExpNo"].Value = num++;
                row.Cells["ExpDesc"].Value = item.Description;
                row.Cells["ExpAmt"].Value = item.Amount.ToString("N0");
                row.Cells["ExpPay"].Value = item.PaymentType switch
                {
                    "card" => "카드", "cash" => "현금", "transfer" => "계좌", _ => item.PaymentType
                };

                var tagColor = item.PaymentType switch
                {
                    "card" => ColorPalette.PaymentCard,
                    "cash" => ColorPalette.PaymentCash,
                    "transfer" => ColorPalette.PaymentTransfer,
                    _ => (ColorPalette.Background, ColorPalette.Text)
                };
                row.Cells["ExpPay"].Style.BackColor = tagColor.Item1;
                row.Cells["ExpPay"].Style.ForeColor = tagColor.Item2;
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    // ===== 요약 데이터 로드 =====
    private async Task LoadSummaryAsync()
    {
        try
        {
            var daily = await _salesService.GetDailySalesAsync(_currentDate);
            var balance = await _salesService.GetCashBalanceAsync(_currentDate);

            var card = daily?.CardAmount ?? 0;
            var cash = daily?.CashAmount ?? 0;
            var transfer = daily?.TransferAmount ?? 0;
            var total = daily?.TotalRevenue ?? 0;

            // 지출 합계
            var expenseItems = (await _salesService.GetSaleItemsAsync(_currentDate))
                .Where(i => i.Category == "expense").ToList();
            var totalExpense = expenseItems.Sum(i => i.Amount);

            // 우측 요약
            _lblSumCard.Text = $"카드      ₩ {card:N0}";
            _lblSumCash.Text = $"현금      ₩ {cash:N0}";
            _lblSumTransfer.Text = $"계좌      ₩ {transfer:N0}";
            _lblSumTotal.Text = $"총매출    ₩ {total:N0}";
            _lblSumExpense.Text = $"총지출    ₩ {totalExpense:N0}";

            var closingBal = balance?.ClosingBalance ?? 0;
            _lblSumCashBalance.Text = $"현금잔액  ₩ {closingBal:N0}";
            _lblSumBalanceDetail.Text = balance != null
                ? $"(전일 {balance.OpeningBalance:N0} + {balance.CashIn:N0} - {balance.CashOut:N0})"
                : "";

            // Summary Cards 갱신
            UpdateSummaryCards();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private void UpdateSummaryCards()
    {
        _summaryCards.UpdateCard(0, $"{_reservations.Count(r => r.Status == "confirmed")}건");
    }

    // ===== 지출 추가 =====
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
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    // ===== 워크인 추가 =====
    private void AddWalkinReservation()
    {
        using var dlg = new WalkinDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        // 예약 목록에 워크인 추가 (메모리)
        _reservations.Add(new Reservation
        {
            ReservationDate = _currentDate,
            TimeSlot = dlg.TimeSlot,
            ThemeName = dlg.ThemeName,
            CustomerName = dlg.CustomerName,
            CustomerPhone = dlg.Phone,
            Headcount = dlg.Headcount,
            Status = "walkin"
        });

        PopulateMainGrid();
        ToastNotification.Show("워크인 추가 완료.", ToastType.Success);
    }

    // ===== 매출 삭제 =====
    private async void DeleteRevenueItem()
    {
        try
        {
            var items = (await _salesService.GetSaleItemsAsync(_currentDate))
                .Where(i => i.Category == "revenue").ToList();

            if (items.Count == 0)
            {
                ToastNotification.Show("삭제할 매출 항목이 없습니다.", ToastType.Warning);
                return;
            }

            using var dlg = new DeleteSaleItemDialog(items);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedItemId == null) return;

            await _salesService.DeleteSaleItemAsync(_currentDate, dlg.SelectedItemId.Value);
            ToastNotification.Show("매출 항목 삭제 완료.", ToastType.Success);
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    // ===== 지출 삭제 (Delete 키) =====
    private async void GridExpense_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Delete || _gridExpense.CurrentRow == null) return;
        var id = (int)_gridExpense.CurrentRow.Cells["ExpId"].Value;
        if (MessageBox.Show("이 지출을 삭제하시겠습니까?", "확인",
                MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        await _salesService.DeleteSaleItemAsync(_currentDate, id);
        await LoadAllAsync();
    }

    // ===== 기존 결제 데이터를 그리드 셀에 채우기 =====
    private void LoadExistingPayments(DataGridViewRow row, Reservation r)
    {
        var prefix = $"{r.TimeSlot} {r.ThemeName} {r.CustomerName}".Trim();

        foreach (var item in _existingSaleItems)
        {
            if (item.Description == null || !item.Description.StartsWith(prefix)) continue;

            var colName = item.PaymentType switch
            {
                "card" => "CardAmt",
                "cash" => "CashAmt",
                "transfer" => "TransferAmt",
                _ => null
            };
            if (colName == null) continue;

            row.Cells[colName].Value = item.Amount.ToString("N0");
            var tagColor = item.PaymentType switch
            {
                "card" => ColorPalette.PaymentCard,
                "cash" => ColorPalette.PaymentCash,
                "transfer" => ColorPalette.PaymentTransfer,
                _ => (Color.White, ColorPalette.Text)
            };
            row.Cells[colName].Style.BackColor = tagColor.Item1;
            row.Cells[colName].Style.ForeColor = tagColor.Item2;
        }
    }

    // ===== 헬퍼 메서드 =====
    private static Button CreateBtn(string text, Color color) => new()
    {
        Text = text, Size = new Size(85, 32),
        BackColor = color, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 9.5f),
        Margin = new Padding(5, 0, 0, 0)
    };

    private static Label MakeSummaryLabel(Panel parent, string prefix, Color color, Font font, ref int y)
    {
        var lbl = new Label
        {
            Location = new Point(15, y), Size = new Size(230, 22),
            Font = font, ForeColor = color, Text = $"{prefix}      ₩ 0"
        };
        parent.Controls.Add(lbl);
        y += 24;
        return lbl;
    }
}

/// <summary>워크인 손님 추가 다이얼로그</summary>
internal class WalkinDialog : Form
{
    public string ThemeName => _txtTheme.Text.Trim();
    public string TimeSlot => $"{_dtpTime.Value:HH:mm}";
    public string CustomerName => _txtName.Text.Trim();
    public string? Phone => string.IsNullOrWhiteSpace(_txtPhone.Text) ? null : _txtPhone.Text.Trim();
    public int Headcount => (int)_numCount.Value;

    private readonly TextBox _txtTheme, _txtName, _txtPhone;
    private readonly DateTimePicker _dtpTime;
    private readonly NumericUpDown _numCount;

    public WalkinDialog()
    {
        Text = "워크인 추가";
        Size = new Size(380, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false; MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);

        var y = 15;
        AddLabel("테마:", ref y);
        _txtTheme = new TextBox { Location = new Point(90, y - 2), Size = new Size(250, 25) };
        Controls.Add(_txtTheme);
        y += 35;

        AddLabel("시간:", ref y);
        _dtpTime = new DateTimePicker
        {
            Location = new Point(90, y - 2), Size = new Size(120, 25),
            Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm",
            ShowUpDown = true, Value = DateTime.Now
        };
        Controls.Add(_dtpTime);
        y += 35;

        AddLabel("예약자:", ref y);
        _txtName = new TextBox { Location = new Point(90, y - 2), Size = new Size(150, 25) };
        Controls.Add(_txtName);
        y += 35;

        AddLabel("연락처:", ref y);
        _txtPhone = new TextBox { Location = new Point(90, y - 2), Size = new Size(180, 25) };
        Controls.Add(_txtPhone);
        y += 35;

        AddLabel("인원:", ref y);
        _numCount = new NumericUpDown
        {
            Location = new Point(90, y - 2), Size = new Size(80, 25),
            Minimum = 1, Maximum = 50, Value = 2
        };
        Controls.Add(_numCount);
        y += 40;

        var btnOk = new Button
        {
            Text = "추가", Location = new Point(170, y), Size = new Size(80, 35),
            BackColor = ColorPalette.Primary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.None
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtTheme.Text))
            {
                MessageBox.Show("테마를 입력하세요.", "알림"); _txtTheme.Focus(); return;
            }
            DialogResult = DialogResult.OK;
        };
        Controls.Add(btnOk);
        Controls.Add(new Button { Text = "취소", Location = new Point(260, y), Size = new Size(80, 35), DialogResult = DialogResult.Cancel });
        AcceptButton = btnOk;
    }

    private void AddLabel(string text, ref int y)
    {
        Controls.Add(new Label { Text = text, Location = new Point(20, y), Size = new Size(65, 22) });
        y += 2;
    }
}

/// <summary>지출 추가 다이얼로그</summary>
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

/// <summary>매출 항목 삭제 다이얼로그 — 당일 매출 목록에서 선택 삭제</summary>
internal class DeleteSaleItemDialog : Form
{
    public int? SelectedItemId { get; private set; }
    private readonly DataGridView _grid;

    public DeleteSaleItemDialog(List<SaleItem> items)
    {
        Text = "매출 삭제";
        Size = new Size(500, 350);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false; MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);

        Controls.Add(new Label
        {
            Text = "삭제할 매출 항목을 선택하세요:",
            Dock = DockStyle.Top, Height = 30, Padding = new Padding(10, 8, 0, 0)
        });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true,
            AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White
        };

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "ItemId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "Desc", HeaderText = "항목", FillWeight = 45 },
            new DataGridViewTextBoxColumn { Name = "Amt", HeaderText = "금액", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "Pay", HeaderText = "결제", FillWeight = 15 },
            new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "등록시간", FillWeight = 15 }
        );

        foreach (var item in items)
        {
            var idx = _grid.Rows.Add();
            _grid.Rows[idx].Cells["ItemId"].Value = item.Id;
            _grid.Rows[idx].Cells["Desc"].Value = item.Description;
            _grid.Rows[idx].Cells["Amt"].Value = $"₩{item.Amount:N0}";
            _grid.Rows[idx].Cells["Pay"].Value = item.PaymentType switch
            {
                "card" => "카드", "cash" => "현금", "transfer" => "계좌", _ => item.PaymentType
            };
            _grid.Rows[idx].Cells["Time"].Value = item.CreatedAt.ToString("HH:mm");
        }

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 45,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10, 5, 10, 5)
        };
        var btnCancel = new Button { Text = "취소", Size = new Size(80, 32), DialogResult = DialogResult.Cancel };
        var btnDelete = new Button
        {
            Text = "삭제", Size = new Size(80, 32),
            BackColor = ColorPalette.Danger, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnDelete.FlatAppearance.BorderSize = 0;
        btnDelete.Click += (_, _) =>
        {
            if (_grid.CurrentRow == null) return;
            SelectedItemId = (int)_grid.CurrentRow.Cells["ItemId"].Value;
            DialogResult = DialogResult.OK;
        };

        btnPanel.Controls.AddRange([btnCancel, btnDelete]);
        Controls.Add(_grid);
        Controls.Add(btnPanel);
        CancelButton = btnCancel;
    }
}
