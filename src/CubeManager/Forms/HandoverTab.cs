using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Controls;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

/// <summary>
/// 인수인계 탭 — 2025 리디자인.
/// 좌: 카드 목록 (날짜, 제목, 다음근무자 체크) | 우: 상세보기 (내용 + 댓글)
/// </summary>
public class HandoverTab : UserControl
{
    private readonly IHandoverRepository _repo;

    // 좌측: 카드 목록
    private readonly Panel _listPanel;
    private readonly TextBox _txtSearch;
    private readonly Label _lblPageInfo;
    private int _page = 1;
    private const int PageSize = 10;

    // 우측: 상세보기
    private readonly Panel _detailPanel;
    private readonly Label _lblDetailTitle;
    private readonly Label _lblDetailMeta;
    private readonly CheckBox _chkNextWorker;
    private readonly Label _lblDetailContent;
    private readonly Panel _commentListPanel;
    private readonly Panel _emptyDetail;

    private Handover? _selected;

    public HandoverTab(IHandoverRepository repo)
    {
        _repo = repo;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Background;
        Padding = new Padding(12);

        // ═══ 상단 헤더 ═══
        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 50,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 5)
        };

        topBar.Controls.Add(new Label
        {
            Text = "인수인계", Size = new Size(100, 32),
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var btnNew = ButtonFactory.CreatePrimary("+ 새 글 작성", 110);
        btnNew.Margin = new Padding(10, 0, 0, 0);
        btnNew.Click += BtnNew_Click;

        _txtSearch = new TextBox
        {
            PlaceholderText = "🔍 제목/내용 검색...",
            Size = new Size(200, 28),
            Font = new Font("맑은 고딕", 10f),
            Margin = new Padding(20, 2, 0, 0)
        };
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _page = 1; _ = LoadAsync(); }
        };

        topBar.Controls.AddRange([btnNew, _txtSearch]);

        // ═══ Split: 좌측 목록 | 우측 상세 ═══
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 380,
            SplitterWidth = 6,
            BackColor = ColorPalette.Border
        };
        split.Panel1.BackColor = ColorPalette.Background;
        split.Panel2.BackColor = ColorPalette.Surface;

        // ── 좌측: 카드 목록 ──
        _listPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ColorPalette.Background,
            Padding = new Padding(4, 4, 4, 0)
        };

        var navBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 35,
            Padding = new Padding(0, 5, 0, 0),
            BackColor = ColorPalette.Background
        };
        var btnPrev = ButtonFactory.CreateGhost("◀", 36);
        btnPrev.Click += (_, _) => { if (_page > 1) { _page--; _ = LoadAsync(); } };
        _lblPageInfo = new Label
        {
            Size = new Size(80, 28), TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("맑은 고딕", 9f), ForeColor = ColorPalette.TextSecondary
        };
        var btnNextPage = ButtonFactory.CreateGhost("▶", 36);
        btnNextPage.Click += (_, _) => { _page++; _ = LoadAsync(); };
        navBar.Controls.AddRange([btnPrev, _lblPageInfo, btnNextPage]);

        split.Panel1.Controls.Add(_listPanel);
        split.Panel1.Controls.Add(navBar);

        // ── 우측: 상세보기 ──
        _detailPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 16),
            AutoScroll = true,
            Visible = false
        };

        _lblDetailTitle = new Label
        {
            Dock = DockStyle.Top, Height = 32,
            Font = new Font("맑은 고딕", 14f, FontStyle.Bold),
            ForeColor = ColorPalette.Text
        };

        _lblDetailMeta = new Label
        {
            Dock = DockStyle.Top, Height = 24,
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextTertiary,
            Padding = new Padding(0, 4, 0, 0)
        };

        _chkNextWorker = new CheckBox
        {
            Text = "다음 근무자 확인 완료",
            Dock = DockStyle.Top, Height = 30,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            ForeColor = ColorPalette.Success,
            Padding = new Padding(0, 6, 0, 0)
        };
        _chkNextWorker.CheckedChanged += ChkNextWorker_Changed;

        var separator1 = new Panel
        {
            Dock = DockStyle.Top, Height = 1,
            BackColor = ColorPalette.Border,
            Margin = new Padding(0, 8, 0, 8)
        };

        _lblDetailContent = new Label
        {
            Dock = DockStyle.Top, AutoSize = true,
            MaximumSize = new Size(500, 0),
            Font = new Font("맑은 고딕", 11f),
            ForeColor = ColorPalette.Text,
            Padding = new Padding(0, 8, 0, 0)
        };

        var lblCommentHeader = new Label
        {
            Text = "💬 댓글",
            Dock = DockStyle.Top, Height = 30,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary,
            Padding = new Padding(0, 12, 0, 0)
        };

        _commentListPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 200,
            AutoScroll = true,
            Padding = new Padding(0, 4, 0, 0)
        };

        var commentInputPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 38,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 6, 0, 0)
        };

        var txtCommentAuthor = new TextBox
        {
            PlaceholderText = "작성자",
            Size = new Size(80, 28), Font = new Font("맑은 고딕", 9f)
        };
        var txtCommentContent = new TextBox
        {
            PlaceholderText = "댓글 입력...",
            Size = new Size(200, 28), Font = new Font("맑은 고딕", 9f)
        };
        var btnAddComment = ButtonFactory.CreatePrimary("등록", 55);
        btnAddComment.Height = 28;
        btnAddComment.Click += async (_, _) =>
        {
            if (_selected == null || string.IsNullOrWhiteSpace(txtCommentAuthor.Text)
                || string.IsNullOrWhiteSpace(txtCommentContent.Text)) return;
            await _repo.InsertCommentAsync(_selected.Id, txtCommentAuthor.Text.Trim(), txtCommentContent.Text.Trim());
            txtCommentContent.Clear();
            ToastNotification.Show("댓글 등록 완료.", ToastType.Success);
            await LoadDetailAsync(_selected);
        };
        // Enter키로 댓글 등록
        txtCommentContent.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; btnAddComment.PerformClick(); }
        };

        commentInputPanel.Controls.AddRange([txtCommentAuthor, txtCommentContent, btnAddComment]);

        // Dock.Top 순서 (역순 추가 아님, 위→아래)
        _detailPanel.Controls.Add(commentInputPanel);
        _detailPanel.Controls.Add(_commentListPanel);
        _detailPanel.Controls.Add(lblCommentHeader);
        _detailPanel.Controls.Add(_lblDetailContent);
        _detailPanel.Controls.Add(separator1);
        _detailPanel.Controls.Add(_chkNextWorker);
        _detailPanel.Controls.Add(_lblDetailMeta);
        _detailPanel.Controls.Add(_lblDetailTitle);

        // 빈 상태 (상세 미선택)
        _emptyDetail = new Panel { Dock = DockStyle.Fill };
        _emptyDetail.Controls.Add(new Label
        {
            Text = "📋 인수인계를 선택하세요",
            Font = new Font("맑은 고딕", 12f),
            ForeColor = ColorPalette.TextTertiary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        });

        split.Panel2.Controls.Add(_detailPanel);
        split.Panel2.Controls.Add(_emptyDetail);

        Controls.Add(split);
        Controls.Add(topBar);
        _ = LoadAsync();
    }

    // ═══════ 데이터 로드 ═══════

    private async Task LoadAsync()
    {
        var keyword = _txtSearch.Text.Trim();
        var (items, totalCount) = await _repo.GetPagedAsync(_page, PageSize,
            string.IsNullOrEmpty(keyword) ? null : keyword);

        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        _lblPageInfo.Text = $"{_page}/{totalPages}";

        _listPanel.Controls.Clear();

        if (!items.Any())
        {
            _listPanel.Controls.Add(new Label
            {
                Text = _page == 1 ? "📋 아직 인수인계가 없습니다.\n'새 글 작성'으로 시작하세요."
                    : "더 이상 글이 없습니다.",
                Font = new Font("맑은 고딕", 11f),
                ForeColor = ColorPalette.TextTertiary,
                Size = new Size(350, 60),
                Location = new Point(15, 20)
            });
            return;
        }

        var y = 2;
        foreach (var h in items)
        {
            var card = CreateCard(h);
            card.Location = new Point(2, y);
            card.Width = _listPanel.Width - 28;
            _listPanel.Controls.Add(card);
            y += card.Height + 6;
        }
    }

    /// <summary>카드 생성: 날짜 + 제목 + 다음근무자체크 아이콘</summary>
    private Panel CreateCard(Handover h)
    {
        var card = new Panel
        {
            Size = new Size(360, 68),
            BackColor = ColorPalette.Surface,
            Cursor = Cursors.Hand,
            Padding = new Padding(12, 8, 12, 8)
        };
        // 라운드 테두리 적용
        card.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedCard.CreateRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            using var borderPen = new Pen(
                _selected?.Id == h.Id ? ColorPalette.Primary : Color.FromArgb(40, ColorPalette.Border), 1);
            pe.Graphics.DrawPath(borderPen, path);

            // 선택된 카드 좌측 액센트
            if (_selected?.Id == h.Id)
            {
                using var accentBrush = new SolidBrush(ColorPalette.Primary);
                pe.Graphics.FillRectangle(accentBrush, 0, 8, 3, card.Height - 16);
            }
        };

        // 날짜
        var lblDate = new Label
        {
            Text = h.CreatedAt.ToString("MM/dd HH:mm"),
            Font = new Font("맑은 고딕", 8.5f),
            ForeColor = ColorPalette.TextTertiary,
            Location = new Point(12, 6), Size = new Size(100, 16)
        };

        // 체크 아이콘
        var lblCheck = new Label
        {
            Text = h.IsNextWorkerChecked ? "✅" : "",
            Font = new Font("Segoe UI Emoji", 10f),
            Location = new Point(card.Width - 35, 6), Size = new Size(25, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        // 제목
        var title = string.IsNullOrEmpty(h.Title) ? "(제목 없음)" : h.Title;
        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Location = new Point(12, 24), Size = new Size(300, 20),
            AutoEllipsis = true
        };

        // 작성자
        var lblAuthor = new Label
        {
            Text = h.AuthorName,
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextSecondary,
            Location = new Point(12, 46), Size = new Size(200, 16)
        };

        card.Controls.AddRange([lblDate, lblCheck, lblTitle, lblAuthor]);

        // 클릭 → 상세보기
        void OnClick(object? s, EventArgs e) { _ = SelectCard(h); }
        card.Click += OnClick;
        foreach (Control c in card.Controls) c.Click += OnClick;

        // 호버 효과
        void OnEnter(object? s, EventArgs e) { card.BackColor = ColorPalette.CardHover; }
        void OnLeave(object? s, EventArgs e) { card.BackColor = ColorPalette.Surface; }
        card.MouseEnter += OnEnter;
        card.MouseLeave += OnLeave;
        foreach (Control c in card.Controls) { c.MouseEnter += OnEnter; c.MouseLeave += OnLeave; }

        return card;
    }

    // ═══════ 상세보기 ═══════

    private async Task SelectCard(Handover h)
    {
        _selected = h;
        await LoadDetailAsync(h);
        _emptyDetail.Visible = false;
        _detailPanel.Visible = true;

        // 카드 목록 repaint (선택 표시)
        foreach (Control c in _listPanel.Controls) c.Invalidate();
    }

    private async Task LoadDetailAsync(Handover h)
    {
        _lblDetailTitle.Text = string.IsNullOrEmpty(h.Title) ? "(제목 없음)" : h.Title;
        _lblDetailMeta.Text = $"{h.AuthorName}  ·  {h.CreatedAt:yyyy-MM-dd HH:mm}";
        _chkNextWorker.Checked = h.IsNextWorkerChecked;
        _lblDetailContent.Text = h.Content;

        // 댓글 로드
        var comments = (await _repo.GetCommentsAsync(h.Id)).ToList();
        _commentListPanel.Controls.Clear();
        var cy = 0;
        foreach (var c in comments)
        {
            var commentCard = new Panel
            {
                Location = new Point(0, cy),
                Size = new Size(_commentListPanel.Width - 20, 50),
                BackColor = ColorPalette.SubtleBg,
                Padding = new Padding(10, 6, 10, 6)
            };
            commentCard.Controls.Add(new Label
            {
                Text = $"{c.AuthorName}  ·  {c.CreatedAt:MM/dd HH:mm}",
                Font = new Font("맑은 고딕", 8f),
                ForeColor = ColorPalette.TextTertiary,
                Dock = DockStyle.Top, Height = 16
            });
            commentCard.Controls.Add(new Label
            {
                Text = c.Content,
                Font = new Font("맑은 고딕", 9.5f),
                ForeColor = ColorPalette.Text,
                Location = new Point(10, 22),
                Size = new Size(commentCard.Width - 30, 24),
                AutoEllipsis = true
            });
            _commentListPanel.Controls.Add(commentCard);
            cy += 56;
        }
        _commentListPanel.Height = Math.Max(cy + 10, 60);
    }

    private async void ChkNextWorker_Changed(object? sender, EventArgs e)
    {
        if (_selected == null) return;
        await _repo.UpdateNextWorkerCheckAsync(_selected.Id, _chkNextWorker.Checked);
        _selected.IsNextWorkerChecked = _chkNextWorker.Checked;
        // 카드 목록 repaint
        foreach (Control c in _listPanel.Controls) c.Invalidate();
    }

    // ═══════ 새 글 작성 ═══════

    private async void BtnNew_Click(object? sender, EventArgs e)
    {
        using var dlg = new HandoverWriteDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        await _repo.InsertHandoverAsync(dlg.AuthorName, dlg.ContentText, dlg.TitleText);
        ToastNotification.Show("인수인계가 등록되었습니다.", ToastType.Success);
        _page = 1;
        await LoadAsync();
    }

    public async Task RefreshAsync() => await LoadAsync();
}

/// <summary>인수인계 작성 다이얼로그</summary>
internal class HandoverWriteDialog : Form
{
    private readonly TextBox _txtAuthor;
    private readonly TextBox _txtTitle;
    private readonly TextBox _txtContent;

    public string AuthorName => _txtAuthor.Text.Trim();
    public string TitleText => _txtTitle.Text.Trim();
    public string ContentText => _txtContent.Text.Trim();

    public HandoverWriteDialog()
    {
        Text = "인수인계 작성";
        Size = new Size(440, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);
        BackColor = ColorPalette.Surface;

        var y = 15;
        Controls.Add(new Label { Text = "작성자:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        _txtAuthor = new TextBox { Location = new Point(90, y), Size = new Size(310, 25) };
        Controls.Add(_txtAuthor);

        y += 35;
        Controls.Add(new Label { Text = "제목:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        _txtTitle = new TextBox { Location = new Point(90, y), Size = new Size(310, 25), PlaceholderText = "인수인계 제목" };
        Controls.Add(_txtTitle);

        y += 35;
        Controls.Add(new Label { Text = "내용:", Location = new Point(20, y + 2), Size = new Size(60, 22) });
        _txtContent = new TextBox
        {
            Location = new Point(90, y), Size = new Size(310, 160),
            Multiline = true, ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "인수인계 내용을 입력하세요..."
        };
        Controls.Add(_txtContent);

        y += 170;
        var btnOk = ButtonFactory.CreatePrimary("등록", 80);
        btnOk.Location = new Point(230, y);
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtAuthor.Text))
            {
                ToastNotification.Show("작성자를 입력하세요.", ToastType.Warning);
                _txtAuthor.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtContent.Text))
            {
                ToastNotification.Show("내용을 입력하세요.", ToastType.Warning);
                _txtContent.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
        };

        var btnCancel = new Button
        {
            Text = "취소", Location = new Point(320, y), Size = new Size(80, 34),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        _txtAuthor.TabIndex = 0;
        _txtTitle.TabIndex = 1;
        _txtContent.TabIndex = 2;
        btnOk.TabIndex = 3;
    }
}
