using System.Drawing;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class ReservationSalesTab : UserControl
{
    private static readonly Font StrikeoutFont = new("맑은 고딕", 10f, FontStyle.Strikeout);

    private readonly ISalesService _salesService;
    private readonly IReservationScraperService _scraperService;
    private readonly IReservationRepository _reservationRepo;
    private readonly DateTimePicker _dtpDate;
    private readonly DataGridView _gridMain;       // 예약+결제 통합
    private readonly DataGridView _gridExpense;    // 지출 내역
    private readonly CheckBox _chkAutoRefresh;
    private readonly Label _lblLastFetch;
    private readonly System.Windows.Forms.Timer _autoRefreshTimer;

    // 우측 결제 요약 라벨들
    private readonly Label _lblSumCard, _lblSumCash, _lblSumTransfer;
    private readonly Label _lblSumTotal, _lblSumExpense, _lblSumCashBalance, _lblSumBalanceDetail;

    // 중앙 통계 라벨들
    private readonly Label _lblStatTotal, _lblStatRemoved, _lblStatNoshow, _lblStatConfirmed;
    private readonly Label _lblStatHeadcount, _lblStatAvgPrice, _lblStatYesterdayCash;

    private string _currentDate;
    private List<Reservation> _reservations = [];
    private List<SaleItem> _existingSaleItems = [];
    // _sortState 제거 — 항상 시간순, 현재 시간으로 스크롤

    public ReservationSalesTab(ISalesService salesService, IReservationScraperService scraperService,
        IReservationRepository reservationRepo)
    {
        _salesService = salesService;
        _scraperService = scraperService;
        _reservationRepo = reservationRepo;
        _currentDate = DateTime.Today.ToString("yyyy-MM-dd");
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
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
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
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

        var btnRecalc = CreateBtn("계산 반영", ColorPalette.AccentGreen.Main);
        btnRecalc.Click += async (_, _) =>
        {
            await _salesService.RecalculateTotalsAsync(_currentDate);
            await LoadSummaryAsync();
            ToastNotification.Show("계산 반영 완료.", ToastType.Success);
        };
        topPanel.Controls.Add(btnRecalc);

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

        var btnDeleteRow = CreateBtn("강제삭제", ColorPalette.Danger);
        btnDeleteRow.Click += (_, _) => DeleteSelectedReservation();
        topPanel.Controls.Add(btnDeleteRow);

        // ========== 2. 통합 그리드 (예약+결제) ==========
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

        // ========== 3. 하단 패널 (지출 + 통계 + 결제 요약) ==========
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom, Height = 210,
            BackColor = ColorPalette.Background,
            Padding = new Padding(0)
        };

        // 3-1. 지출 그리드 (좌측 40%)
        var expensePanel = new Panel
        {
            Dock = DockStyle.Left, Width = 1, // Resize에서 설정
            Padding = new Padding(10)
        };

        var lblExpenseHeader = new Label
        {
            Text = "지출 내역", Dock = DockStyle.Top, Height = 25,
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
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
        btnAddExpense.Width = 0;
        btnAddExpense.Click += (_, _) => AddItem("expense");

        expensePanel.Controls.Add(_gridExpense);
        expensePanel.Controls.Add(btnAddExpense);
        expensePanel.Controls.Add(lblExpenseHeader);

        // 3-2. 오늘의 통계 (중앙 30%)
        var statsPanel = new Panel
        {
            Dock = DockStyle.Left, Width = 1, // Resize에서 설정
            Padding = new Padding(15, 10, 15, 10),
            BackColor = ColorPalette.Surface
        };

        // 좌측 구분선
        var statsLeftBorder = new Panel
        {
            Dock = DockStyle.Left, Width = 1,
            BackColor = ColorPalette.Border
        };

        var lblStatsHeader = new Label
        {
            Text = "오늘의 통계", Dock = DockStyle.Top, Height = 25,
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary
        };

        var statsContent = new Panel { Dock = DockStyle.Fill };
        var ssf = new Font("맑은 고딕", 10.5f);
        var sy = 4;
        _lblStatTotal = MakeStatLabel(statsContent, "오늘 예약", ColorPalette.Text, ssf, ref sy);
        _lblStatRemoved = MakeStatLabel(statsContent, "취소", ColorPalette.Danger, ssf, ref sy);
        _lblStatNoshow = MakeStatLabel(statsContent, "노쇼", ColorPalette.AccentOrange.Main, ssf, ref sy);
        _lblStatConfirmed = MakeStatLabel(statsContent, "확정", ColorPalette.AccentGreen.Main, ssf, ref sy);
        sy += 3;
        statsContent.Controls.Add(new Label { Location = new Point(5, sy), Size = new Size(180, 1), BackColor = ColorPalette.Border });
        sy += 6;
        _lblStatHeadcount = MakeStatLabel(statsContent, "총 인원", ColorPalette.Text, ssf, ref sy);
        _lblStatAvgPrice = MakeStatLabel(statsContent, "객단가", ColorPalette.Primary, ssf, ref sy);
        _lblStatYesterdayCash = MakeStatLabel(statsContent, "어제 잔돈", ColorPalette.AccentOrange.Main, ssf, ref sy);

        statsPanel.Controls.Add(statsContent);
        statsPanel.Controls.Add(lblStatsHeader);
        statsPanel.Controls.Add(statsLeftBorder);

        // 3-3. 결제 요약 패널 (우측 30%)
        var summaryRight = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15, 10, 15, 10),
            BackColor = ColorPalette.Surface
        };

        // 좌측 구분선
        var summaryLeftBorder = new Panel
        {
            Dock = DockStyle.Left, Width = 1,
            BackColor = ColorPalette.Border
        };

        var lblSummaryHeader = new Label
        {
            Text = "결제 요약", Dock = DockStyle.Top, Height = 25,
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary
        };

        var summaryContent = new Panel { Dock = DockStyle.Fill };
        var sf = new Font("맑은 고딕", 10.5f);
        var bf = new Font("맑은 고딕", 11f, FontStyle.Bold);
        var ry = 4;
        _lblSumCard = MakeStatLabel(summaryContent, "카드", ColorPalette.PaymentCard.Item2, sf, ref ry);
        _lblSumCash = MakeStatLabel(summaryContent, "현금", ColorPalette.PaymentCash.Item2, sf, ref ry);
        _lblSumTransfer = MakeStatLabel(summaryContent, "계좌", ColorPalette.PaymentTransfer.Item2, sf, ref ry);
        ry += 3;
        summaryContent.Controls.Add(new Label { Location = new Point(5, ry), Size = new Size(180, 1), BackColor = ColorPalette.Border });
        ry += 6;
        _lblSumTotal = MakeStatLabel(summaryContent, "총매출", ColorPalette.Text, bf, ref ry);
        _lblSumExpense = MakeStatLabel(summaryContent, "총지출", ColorPalette.Danger, sf, ref ry);
        ry += 3;
        summaryContent.Controls.Add(new Label { Location = new Point(5, ry), Size = new Size(180, 1), BackColor = ColorPalette.Border });
        ry += 6;
        _lblSumCashBalance = MakeStatLabel(summaryContent, "현금잔액", ColorPalette.Primary, bf, ref ry);
        _lblSumBalanceDetail = new Label
        {
            Location = new Point(10, ry), Size = new Size(200, 18),
            Font = new Font("맑은 고딕", 8.5f), ForeColor = ColorPalette.TextTertiary
        };
        summaryContent.Controls.Add(_lblSumBalanceDetail);

        summaryRight.Controls.Add(summaryContent);
        summaryRight.Controls.Add(lblSummaryHeader);
        summaryRight.Controls.Add(summaryLeftBorder);

        // 하단 패널에 역순 추가 (Dock 순서)
        bottomPanel.Controls.Add(summaryRight);    // Fill (마지막 = 나머지)
        bottomPanel.Controls.Add(statsPanel);      // Left (중앙)
        bottomPanel.Controls.Add(expensePanel);    // Left (좌측)

        // 구분선
        var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ColorPalette.Border };

        // ========== 레이아웃 조립 (역순) ==========
        Controls.Add(_gridMain);
        Controls.Add(divider);
        Controls.Add(bottomPanel);
        Controls.Add(topPanel);

        // 하단 패널 크기 조정 (3분할)
        bottomPanel.Resize += (_, _) =>
        {
            expensePanel.Width = (int)(bottomPanel.Width * 0.40);
            statsPanel.Width = (int)(bottomPanel.Width * 0.30);
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

    // ===== 웹 스크래핑 + DB 저장 + 그리드 갱신 =====
    private async Task FetchWebAndUpdate()
    {
        // 1. 웹에서 스크래핑
        var scraped = (await _scraperService.FetchReservationsAsync(_dtpDate.Value)).ToList();

        // 2. DB에 Upsert (기존 예약 상태는 보존됨)
        foreach (var r in scraped)
            await _reservationRepo.UpsertAsync(r);

        // 3. 웹에서 사라진 예약 감지 → 자동으로 "삭제" 상태 전환
        var dbReservations = (await _reservationRepo.GetByDateAsync(_currentDate)).ToList();
        foreach (var dbRes in dbReservations)
        {
            // 이미 사용자가 수동으로 변경한 상태(removed, walkin)는 건드리지 않음
            if (dbRes.Status != "confirmed") continue;

            // 웹 조회 결과에 없으면 → 웹에서 삭제된 것
            var stillExists = scraped.Any(s =>
                s.TimeSlot == dbRes.TimeSlot &&
                s.ThemeName == dbRes.ThemeName &&
                s.CustomerName == dbRes.CustomerName);

            if (!stillExists)
                await _reservationRepo.UpdateStatusAsync(dbRes.Id, "removed");
        }

        // 4. DB에서 읽기 (최종 상태 반영)
        await LoadReservationsFromDb();

        _lblLastFetch.Text = $"마지막 조회: {DateTime.Now:HH:mm:ss}";
    }

    // ===== DB에서 예약 + 매출 로드 =====
    private async Task LoadReservationsFromDb()
    {
        _reservations = (await _reservationRepo.GetByDateAsync(_currentDate)).ToList();
        _existingSaleItems = (await _salesService.GetSaleItemsAsync(_currentDate))
            .Where(i => i.Category == "revenue").ToList();
        PopulateMainGrid();
    }

    // ===== 통합 그리드 데이터 채우기 =====
    private void PopulateMainGrid()
    {
        _gridMain.Rows.Clear();

        // 항상 시간 오름차순 정렬
        var sorted = _reservations.OrderBy(r => r.TimeSlot).ToList();

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
            var isNoshow = r.Status == "noshow";
            var isWalkin = r.Status == "walkin";
            row.Cells["Status"].Value = isRemoved ? "취소" : isNoshow ? "노쇼" : isWalkin ? "워크인" : "확정";
            if (isRemoved)
            {
                row.Cells["Status"].Style.BackColor = ColorPalette.PaymentExpense.Item1;
                row.Cells["Status"].Style.ForeColor = ColorPalette.PaymentExpense.Item2;
            }
            else if (isNoshow)
            {
                row.Cells["Status"].Style.BackColor = ColorPalette.AccentOrange.Light;
                row.Cells["Status"].Style.ForeColor = ColorPalette.AccentOrange.Main;
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

            // 취소/노쇼 행 스타일
            if (isRemoved || isNoshow)
            {
                for (var c = 0; c < row.Cells.Count; c++)
                {
                    row.Cells[c].Style.ForeColor = ColorPalette.TextTertiary;
                    row.Cells[c].Style.Font = StrikeoutFont;
                }
                // 결제 셀 비활성
                row.Cells["CardAmt"].ReadOnly = true;
                row.Cells["CashAmt"].ReadOnly = true;
                row.Cells["TransferAmt"].ReadOnly = true;
            }

            // 기존 결제 데이터 로드하여 금액 셀 채우기
            LoadExistingPayments(row, r);
        }

        // 현재 시간에 가장 가까운 예약 행으로 스크롤
        ScrollToCurrentTime();
    }

    private void ScrollToCurrentTime()
    {
        if (_gridMain.Rows.Count == 0) return;

        var now = DateTime.Now.ToString("HH:mm");
        var targetRow = -1;

        for (var i = 0; i < _gridMain.Rows.Count; i++)
        {
            var timeSlot = _gridMain.Rows[i].Cells["Time"].Value?.ToString() ?? "";
            if (string.Compare(timeSlot, now, StringComparison.Ordinal) >= 0)
            {
                targetRow = i;
                break;
            }
        }

        // 모든 예약이 이미 지났으면 마지막 행으로
        if (targetRow < 0)
            targetRow = _gridMain.Rows.Count - 1;

        _gridMain.FirstDisplayedScrollingRowIndex = targetRow;
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
            row.Cells[e.ColumnIndex].Style.BackColor = ColorPalette.Surface;
            return;
        }

        // 결제 태그 색상 적용
        var tagColor = paymentType switch
        {
            "card" => ColorPalette.PaymentCard,
            "cash" => ColorPalette.PaymentCash,
            "transfer" => ColorPalette.PaymentTransfer,
            _ => (ColorPalette.Surface, ColorPalette.Text)
        };
        row.Cells[e.ColumnIndex].Style.BackColor = tagColor.Item1;
        row.Cells[e.ColumnIndex].Style.ForeColor = tagColor.Item2;
        row.Cells[e.ColumnIndex].Value = amount.ToString("N0");

        // 예약 ID 기반으로 고유 설명 생성
        var resId = row.Cells["ResId"].Value;
        var theme = row.Cells["Theme"].Value?.ToString() ?? "매출";
        var customer = row.Cells["Customer"].Value?.ToString() ?? "";
        var time = row.Cells["Time"].Value?.ToString() ?? "";
        var desc = resId != null && (int)resId > 0
            ? $"[R{resId}] {time} {theme} {customer}".Trim()
            : $"{time} {theme} {customer}".Trim();

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

        if (currentStatus is "확정" or "워크인")
        {
            menu.Items.Add("취소(삭제)", null, (_, _) =>
            {
                SetReservationStatus(e.RowIndex, "removed");
            });
            menu.Items.Add("노쇼", null, (_, _) =>
            {
                SetReservationStatus(e.RowIndex, "noshow");
            });
        }
        else
        {
            menu.Items.Add("복원", null, (_, _) =>
            {
                SetReservationStatus(e.RowIndex, "confirmed");
            });
        }

        // 모든 상태에서 완전 삭제 가능
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("🗑 완전 삭제", null, (_, _) =>
        {
            DeleteReservationPermanently(e.RowIndex);
        });

        menu.Show(_gridMain, _gridMain.PointToClient(Cursor.Position));
    }

    private async void SetReservationStatus(int rowIndex, string status)
    {
        if (rowIndex < 0 || rowIndex >= _reservations.Count) return;

        var reservation = _reservations[rowIndex];
        reservation.Status = status;

        // DB에 상태 저장 (Id가 있으면 DB 레코드)
        if (reservation.Id > 0)
            await _reservationRepo.UpdateStatusAsync(reservation.Id, status);

        PopulateMainGrid();
    }

    // ===== 예약 완전 삭제 (DB에서 제거) =====
    private async void DeleteReservationPermanently(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _reservations.Count) return;

        var reservation = _reservations[rowIndex];
        var desc = $"{reservation.TimeSlot} {reservation.ThemeName} {reservation.CustomerName}".Trim();

        if (MessageBox.Show($"'{desc}' 예약을 완전히 삭제합니까?\n(표와 DB에서 영구 삭제됩니다)",
                "완전 삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            if (reservation.Id > 0)
                await _reservationRepo.DeleteAsync(reservation.Id);

            _reservations.RemoveAt(rowIndex);
            PopulateMainGrid();
            await LoadSummaryAsync();
            ToastNotification.Show("예약이 완전 삭제되었습니다.", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"삭제 실패: {ex.Message}", ToastType.Error);
        }
    }

    // ===== 선택된 행 삭제 (상단 버튼) =====
    private void DeleteSelectedReservation()
    {
        if (_gridMain.CurrentRow == null)
        {
            ToastNotification.Show("삭제할 행을 선택하세요.", ToastType.Warning);
            return;
        }
        DeleteReservationPermanently(_gridMain.CurrentRow.Index);
    }

    // ===== 전체 데이터 로드 =====
    private async Task LoadAllAsync()
    {
        // daily_sales 합계 + cash_balance 재계산 (이전 세션 데이터 정합성 보장)
        await _salesService.RecalculateTotalsAsync(_currentDate);
        await LoadReservationsFromDb();
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

    // ===== 요약 + 통계 데이터 로드 =====
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

            // ── 우측 결제 요약 ──
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

            // ── 중앙 오늘의 통계 ──
            var totalCount = _reservations.Count;
            var removedCount = _reservations.Count(r => r.Status == "removed");
            var noshowCount = _reservations.Count(r => r.Status == "noshow");
            var confirmedCount = _reservations.Count(r => r.Status == "confirmed");
            var activeReservations = _reservations
                .Where(r => r.Status is "confirmed" or "walkin").ToList();
            var headcount = activeReservations.Sum(r => r.Headcount);
            var avgPrice = activeReservations.Count > 0 ? total / activeReservations.Count : 0;

            _lblStatTotal.Text = $"오늘 예약       {totalCount}팀";
            _lblStatRemoved.Text = $"취소              {removedCount}팀";
            _lblStatNoshow.Text = $"노쇼              {noshowCount}팀";
            _lblStatConfirmed.Text = $"확정              {confirmedCount}팀";
            _lblStatHeadcount.Text = $"총 인원         {headcount}명";
            _lblStatAvgPrice.Text = $"객단가          ₩ {avgPrice:N0}";

            // 어제 잔돈: 전일 CashBalance
            var yesterday = DateTime.Parse(_currentDate).AddDays(-1).ToString("yyyy-MM-dd");
            var yesterdayBalance = await _salesService.GetCashBalanceAsync(yesterday);
            var yesterdayCash = yesterdayBalance?.ClosingBalance ?? 0;
            _lblStatYesterdayCash.Text = $"어제 잔돈     ₩ {yesterdayCash:N0}";
        }
        catch (Exception ex)
        {
            // 예외 시 기본값 표시 (— 상태 방지)
            _lblSumCard.Text = "카드      ₩ 0";
            _lblSumCash.Text = "현금      ₩ 0";
            _lblSumTransfer.Text = "계좌      ₩ 0";
            _lblSumTotal.Text = "총매출    ₩ 0";
            _lblSumExpense.Text = "총지출    ₩ 0";
            _lblSumCashBalance.Text = "현금잔액  ₩ 0";
            _lblSumBalanceDetail.Text = "";
            _lblStatTotal.Text = "오늘 예약       0팀";
            _lblStatRemoved.Text = "취소              0팀";
            _lblStatNoshow.Text = "노쇼              0팀";
            _lblStatConfirmed.Text = "확정              0팀";
            _lblStatHeadcount.Text = "총 인원         0명";
            _lblStatAvgPrice.Text = "객단가          ₩ 0";
            _lblStatYesterdayCash.Text = "어제 잔돈     ₩ 0";
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
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
    private async void AddWalkinReservation()
    {
        using var dlg = new WalkinDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        // DB에 워크인 예약 저장
        var walkin = new Reservation
        {
            ReservationDate = _currentDate,
            TimeSlot = dlg.TimeSlot,
            ThemeName = dlg.ThemeName,
            CustomerName = dlg.CustomerName,
            CustomerPhone = dlg.Phone,
            Headcount = dlg.Headcount,
            Status = "walkin",
            SyncedAt = DateTime.Now
        };
        await _reservationRepo.UpsertAsync(walkin);

        // DB에서 다시 로드 (ID 포함)
        await LoadReservationsFromDb();
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
        // ID 기반 또는 시간+테마+이름 기반으로 매칭
        var prefixById = r.Id > 0 ? $"[R{r.Id}]" : null;
        var prefixByName = $"{r.TimeSlot} {r.ThemeName} {r.CustomerName}".Trim();

        foreach (var item in _existingSaleItems)
        {
            if (item.Description == null) continue;
            var matched = (prefixById != null && item.Description.StartsWith(prefixById))
                          || item.Description.StartsWith(prefixByName);
            if (!matched) continue;

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
                _ => (ColorPalette.Surface, ColorPalette.Text)
            };
            row.Cells[colName].Style.BackColor = tagColor.Item1;
            row.Cells[colName].Style.ForeColor = tagColor.Item2;
        }
    }

    // ===== 헬퍼 메서드 =====
    private static Button CreateBtn(string text, Color color)
    {
        var btn = ButtonFactory.CreatePrimary(text, 90);
        btn.BackColor = color;
        btn.Font = new Font("맑은 고딕", 9.5f);
        btn.Margin = new Padding(5, 0, 0, 0);
        return btn;
    }

    private static Label MakeStatLabel(Panel parent, string prefix, Color color, Font font, ref int y)
    {
        var lbl = new Label
        {
            Location = new Point(10, y), Size = new Size(220, 20),
            Font = font, ForeColor = color, Text = $"{prefix}      0"
        };
        parent.Controls.Add(lbl);
        y += 22;
        return lbl;
    }
}

/// <summary>워크인 손님 추가 다이얼로그</summary>
internal class WalkinDialog : Form
{
    public string ThemeName => _txtTheme.Text.Trim();
    public string TimeSlot
    {
        get
        {
            var start = _dtpTime.Value;
            var end = start.AddHours(1);
            return $"{start:HH:mm}-{end:HH:mm}";
        }
    }
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
            BackColor = ColorPalette.Primary, ForeColor = ColorPalette.TextWhite,
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
            BackColor = ColorPalette.Primary, ForeColor = ColorPalette.TextWhite,
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
            BackgroundColor = ColorPalette.Surface
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
            BackColor = ColorPalette.Danger, ForeColor = ColorPalette.TextWhite,
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
