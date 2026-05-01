using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Controls;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

/// <summary>
/// 인수인계 탭.
/// 좌측은 최신 글 목록, 우측은 본문과 댓글을 분리해 작은 창에서도 잘리지 않도록 구성한다.
/// </summary>
public class HandoverTab : UserControl
{
    private readonly IHandoverRepository _repo;

    private SplitContainer _split = null!;
    private Panel _listPanel = null!;
    private TextBox _txtSearch = null!;
    private Label _lblPageInfo = null!;
    private Panel _detailPanel = null!;
    private Panel _emptyDetail = null!;
    private Label _lblDetailTitle = null!;
    private Label _lblDetailMeta = null!;
    private CheckBox _chkNextWorker = null!;
    private TextBox _txtDetailContent = null!;
    private Panel _commentsShell = null!;
    private Panel _commentListPanel = null!;
    private TextBox _txtCommentAuthor = null!;
    private TextBox _txtCommentContent = null!;
    private Button _btnAddComment = null!;

    private Handover? _selected;
    private int _page = 1;
    private bool _isLoadingDetail;

    private const int PageSize = 10;
    private const int MinListWidth = 280;

    public HandoverTab(IHandoverRepository repo)
    {
        _repo = repo;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Background;
        Padding = new Padding(16);

        var topBar = CreateTopBar();

        // MinSize는 ctor에서 직접 설정하지 않는다. 기본 SplitContainer Width=150 상태에서
        // 큰 Panel1/Panel2MinSize를 박으면 내부적으로 SplitterDistance를 재조정하다 범위를
        // 벗어나 InvalidOperationException이 발생함. Width가 확보된 뒤 LayoutSplit에서 적용.
        _split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 8,
            BackColor = ColorPalette.Background
        };
        _split.Panel1.BackColor = ColorPalette.Background;
        _split.Panel2.BackColor = ColorPalette.Background;
        _split.HandleCreated += (_, _) => LayoutSplit();
        _split.Resize += (_, _) => LayoutSplit();

        _listPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ColorPalette.Background,
            Padding = new Padding(0, 2, 8, 2)
        };
        _listPanel.Resize += (_, _) => LayoutListCards();

        var navBar = CreateNavigationBar();
        _split.Panel1.Controls.Add(_listPanel);
        _split.Panel1.Controls.Add(navBar);

        _detailPanel = CreateDetailPanel();
        _emptyDetail = CreateEmptyDetailPanel();
        _split.Panel2.Controls.Add(_detailPanel);
        _split.Panel2.Controls.Add(_emptyDetail);

        Controls.Add(_split);
        Controls.Add(topBar);

        _ = LoadAsync();
    }

    private Control CreateTopBar()
    {
        var topBar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(0, 2, 0, 10),
            BackColor = ColorPalette.Background
        };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 2));

        var lblTitle = new Label
        {
            Text = "인수인계",
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnNew = ButtonFactory.CreatePrimary("+ 새 글 작성", 112);
        btnNew.Dock = DockStyle.Left;
        btnNew.Margin = new Padding(0, 7, 12, 0);
        btnNew.Click += BtnNew_Click;

        _txtSearch = new TextBox
        {
            PlaceholderText = "제목/내용/작성자 검색",
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 10f),
            Margin = new Padding(0, 8, 0, 8)
        };
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            _page = 1;
            _ = LoadAsync();
        };

        topBar.Controls.Add(lblTitle, 0, 0);
        topBar.Controls.Add(btnNew, 1, 0);
        topBar.Controls.Add(_txtSearch, 2, 0);
        return topBar;
    }

    private Control CreateNavigationBar()
    {
        var navBar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 8, 8, 0),
            BackColor = ColorPalette.Background
        };
        navBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        navBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        navBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));

        var btnPrev = ButtonFactory.CreateGhost("◀", 38);
        btnPrev.Dock = DockStyle.Fill;
        btnPrev.Click += (_, _) =>
        {
            if (_page <= 1) return;
            _page--;
            _ = LoadAsync();
        };

        _lblPageInfo = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary
        };

        var btnNext = ButtonFactory.CreateGhost("▶", 38);
        btnNext.Dock = DockStyle.Fill;
        btnNext.Click += (_, _) =>
        {
            _page++;
            _ = LoadAsync();
        };

        navBar.Controls.Add(btnPrev, 0, 0);
        navBar.Controls.Add(_lblPageInfo, 1, 0);
        navBar.Controls.Add(btnNext, 2, 0);
        return navBar;
    }

    private Panel CreateDetailPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            BackColor = ColorPalette.Surface,
            Visible = false
        };

        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 116,
            BackColor = ColorPalette.Surface
        };

        _lblDetailTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 38,
            Font = new Font("맑은 고딕", 15f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblDetailMeta = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextTertiary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var actionRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = ColorPalette.Surface
        };
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 4));

        _chkNextWorker = new CheckBox
        {
            Text = "다음 근무자 확인 완료",
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
            ForeColor = ColorPalette.Success,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _chkNextWorker.CheckedChanged += ChkNextWorker_Changed;

        var btnDelete = ButtonFactory.CreateDanger("삭제", 76);
        btnDelete.Dock = DockStyle.Fill;
        btnDelete.Click += BtnDelete_Click;

        actionRow.Controls.Add(_chkNextWorker, 0, 0);
        actionRow.Controls.Add(btnDelete, 1, 0);

        var divider = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = ColorPalette.Border
        };

        headerPanel.Controls.Add(divider);
        headerPanel.Controls.Add(actionRow);
        headerPanel.Controls.Add(_lblDetailMeta);
        headerPanel.Controls.Add(_lblDetailTitle);

        _commentsShell = CreateCommentsPanel();

        _txtDetailContent = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.Card,
            ForeColor = ColorPalette.Text,
            Font = new Font("맑은 고딕", 10.5f),
            Margin = new Padding(0, 8, 0, 8)
        };

        panel.Controls.Add(_txtDetailContent);
        panel.Controls.Add(_commentsShell);
        panel.Controls.Add(headerPanel);
        panel.Resize += (_, _) => LayoutDetailPanel();
        return panel;
    }

    private Panel CreateCommentsPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 240,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = ColorPalette.Surface
        };

        var lblCommentHeader = new Label
        {
            Text = "댓글",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("맑은 고딕", 10.5f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var inputPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = ColorPalette.Surface
        };
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));

        _txtCommentAuthor = new TextBox
        {
            PlaceholderText = "작성자",
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 9f),
            Margin = new Padding(0, 0, 8, 0)
        };
        _txtCommentContent = new TextBox
        {
            PlaceholderText = "댓글 입력",
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 9f),
            Margin = new Padding(0, 0, 8, 0)
        };
        _txtCommentContent.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            _btnAddComment.PerformClick();
        };

        _btnAddComment = ButtonFactory.CreatePrimary("등록", 64);
        _btnAddComment.Dock = DockStyle.Fill;
        _btnAddComment.Click += BtnAddComment_Click;

        inputPanel.Controls.Add(_txtCommentAuthor, 0, 0);
        inputPanel.Controls.Add(_txtCommentContent, 1, 0);
        inputPanel.Controls.Add(_btnAddComment, 2, 0);

        _commentListPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ColorPalette.Surface,
            Padding = new Padding(0, 4, 8, 4)
        };
        _commentListPanel.Resize += (_, _) => LayoutCommentCards();

        panel.Controls.Add(_commentListPanel);
        panel.Controls.Add(inputPanel);
        panel.Controls.Add(lblCommentHeader);
        return panel;
    }

    private static Panel CreateEmptyDetailPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorPalette.Surface
        };
        panel.Controls.Add(new Label
        {
            Text = "인수인계를 선택하세요",
            Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
            ForeColor = ColorPalette.TextTertiary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        });
        return panel;
    }

    private async Task LoadAsync()
    {
        var keyword = _txtSearch.Text.Trim();
        var (itemsEnumerable, totalCount) = await _repo.GetPagedAsync(_page, PageSize,
            string.IsNullOrWhiteSpace(keyword) ? null : keyword);

        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (_page > totalPages)
        {
            _page = totalPages;
            (itemsEnumerable, totalCount) = await _repo.GetPagedAsync(_page, PageSize,
                string.IsNullOrWhiteSpace(keyword) ? null : keyword);
        }

        var items = itemsEnumerable.ToList();
        _lblPageInfo.Text = $"{_page}/{totalPages}";
        _listPanel.Controls.Clear();

        if (items.Count == 0)
        {
            _listPanel.Controls.Add(new Label
            {
                Text = _page == 1 ? "아직 인수인계가 없습니다.\n'새 글 작성'으로 시작하세요." : "더 이상 글이 없습니다.",
                Font = new Font("맑은 고딕", 10.5f, FontStyle.Bold),
                ForeColor = ColorPalette.TextTertiary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 76
            });
            return;
        }

        var y = 2;
        foreach (var handover in items)
        {
            var card = CreateCard(handover);
            card.Location = new Point(0, y);
            _listPanel.Controls.Add(card);
            y += card.Height + 8;
        }
        LayoutListCards();
    }

    private Panel CreateCard(Handover handover)
    {
        var isUnread = !handover.IsNextWorkerChecked;
        var defaultBg = isUnread ? ColorPalette.InfoLight : ColorPalette.Card;
        var title = string.IsNullOrWhiteSpace(handover.Title) ? "(제목 없음)" : handover.Title;

        var card = new Panel
        {
            Size = new Size(Math.Max(220, _listPanel.ClientSize.Width - 12), 92),
            BackColor = defaultBg,
            Cursor = Cursors.Hand,
            Padding = new Padding(12, 8, 12, 8),
            Tag = handover
        };

        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
            using var path = RoundedCard.CreateRoundedPath(rect, 8);
            using var borderPen = new Pen(_selected?.Id == handover.Id ? ColorPalette.Primary : ColorPalette.Border, 1);
            e.Graphics.DrawPath(borderPen, path);

            if (_selected?.Id == handover.Id)
            {
                using var accent = new SolidBrush(ColorPalette.Primary);
                e.Graphics.FillRectangle(accent, 0, 8, 3, card.Height - 16);
            }
        };

        var lblStatus = new Label
        {
            Text = handover.IsNextWorkerChecked ? "확인완료" : "미확인",
            Location = new Point(card.Width - 82, 8),
            Size = new Size(68, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Font = new Font("맑은 고딕", 8.5f, FontStyle.Bold),
            ForeColor = handover.IsNextWorkerChecked ? ColorPalette.Success : ColorPalette.Warning,
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblDate = new Label
        {
            Text = handover.CreatedAt.ToString("MM/dd HH:mm"),
            Location = new Point(12, 8),
            Size = new Size(card.Width - 104, 18),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Font = new Font("맑은 고딕", 8.5f),
            ForeColor = ColorPalette.TextTertiary,
            AutoEllipsis = true
        };

        var lblTitle = new Label
        {
            Text = title,
            Location = new Point(12, 30),
            Size = new Size(card.Width - 24, 24),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            AutoEllipsis = true
        };

        var lblMeta = new Label
        {
            Text = $"{handover.AuthorName}  ·  {GetPreview(handover.Content)}",
            Location = new Point(12, 58),
            Size = new Size(card.Width - 24, 24),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextSecondary,
            AutoEllipsis = true
        };

        card.Controls.AddRange([lblStatus, lblDate, lblTitle, lblMeta]);

        void OnClick(object? s, EventArgs e) => _ = SelectCardAsync(handover);
        card.Click += OnClick;
        foreach (Control child in card.Controls)
            child.Click += OnClick;

        void OnEnter(object? s, EventArgs e) => card.BackColor = ColorPalette.CardHover;
        void OnLeave(object? s, EventArgs e) => card.BackColor = defaultBg;
        card.MouseEnter += OnEnter;
        card.MouseLeave += OnLeave;
        foreach (Control child in card.Controls)
        {
            child.MouseEnter += OnEnter;
            child.MouseLeave += OnLeave;
        }

        return card;
    }

    private async Task SelectCardAsync(Handover handover)
    {
        _selected = handover;
        await LoadDetailAsync(handover);
        _emptyDetail.Visible = false;
        _detailPanel.Visible = true;

        foreach (Control control in _listPanel.Controls)
            control.Invalidate();
    }

    private async Task LoadDetailAsync(Handover handover)
    {
        _isLoadingDetail = true;
        try
        {
            _lblDetailTitle.Text = string.IsNullOrWhiteSpace(handover.Title) ? "(제목 없음)" : handover.Title;
            _lblDetailMeta.Text = $"{handover.AuthorName}  ·  {handover.CreatedAt:yyyy-MM-dd HH:mm}";
            _chkNextWorker.Checked = handover.IsNextWorkerChecked;
            _txtDetailContent.Text = handover.Content;

            var comments = (await _repo.GetCommentsAsync(handover.Id)).ToList();
            RenderComments(comments);
        }
        finally
        {
            _isLoadingDetail = false;
        }
    }

    private void RenderComments(IReadOnlyList<HandoverComment> comments)
    {
        _commentListPanel.Controls.Clear();

        if (comments.Count == 0)
        {
            _commentListPanel.Controls.Add(new Label
            {
                Text = "아직 댓글이 없습니다.",
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("맑은 고딕", 9f),
                ForeColor = ColorPalette.TextTertiary,
                TextAlign = ContentAlignment.MiddleLeft
            });
            return;
        }

        var y = 0;
        foreach (var comment in comments)
        {
            var card = CreateCommentCard(comment);
            card.Location = new Point(0, y);
            _commentListPanel.Controls.Add(card);
            y += card.Height + 8;
        }
        LayoutCommentCards();
    }

    private Panel CreateCommentCard(HandoverComment comment)
    {
        var width = Math.Max(260, _commentListPanel.ClientSize.Width - 16);
        using var measureFont = new Font("맑은 고딕", 9.5f);
        var contentHeight = Math.Max(28, TextRenderer.MeasureText(
            comment.Content,
            measureFont,
            new Size(width - 24, 0),
            TextFormatFlags.WordBreak).Height + 6);

        var card = new Panel
        {
            Size = new Size(width, 42 + contentHeight),
            BackColor = ColorPalette.SubtleBg,
            Padding = new Padding(10, 6, 10, 8),
            Tag = comment
        };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedCard.CreateRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 6);
            using var pen = new Pen(ColorPalette.Border, 1);
            e.Graphics.DrawPath(pen, path);
        };

        card.Controls.Add(new Label
        {
            Text = comment.Content,
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 9.5f),
            ForeColor = ColorPalette.Text,
            Padding = new Padding(0, 4, 0, 0)
        });
        card.Controls.Add(new Label
        {
            Text = $"{comment.AuthorName}  ·  {comment.CreatedAt:MM/dd HH:mm}",
            Dock = DockStyle.Top,
            Height = 20,
            Font = new Font("맑은 고딕", 8.5f, FontStyle.Bold),
            ForeColor = ColorPalette.TextTertiary
        });

        return card;
    }

    private async void ChkNextWorker_Changed(object? sender, EventArgs e)
    {
        if (_selected == null || _isLoadingDetail) return;

        await _repo.UpdateNextWorkerCheckAsync(_selected.Id, _chkNextWorker.Checked);
        _selected.IsNextWorkerChecked = _chkNextWorker.Checked;
        ToastNotification.Show(_chkNextWorker.Checked ? "확인 완료로 변경했습니다." : "미확인으로 변경했습니다.", ToastType.Success);
        await LoadAsync();
    }

    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_selected == null) return;

        var title = string.IsNullOrWhiteSpace(_selected.Title) ? "(제목 없음)" : _selected.Title;
        if (MessageBox.Show($"'{title}' 인수인계를 삭제하시겠습니까?", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        await _repo.DeleteHandoverAsync(_selected.Id);
        _selected = null;
        _detailPanel.Visible = false;
        _emptyDetail.Visible = true;
        ToastNotification.Show("인수인계가 삭제되었습니다.", ToastType.Success);
        await LoadAsync();
    }

    private async void BtnAddComment_Click(object? sender, EventArgs e)
    {
        if (_selected == null)
            return;

        var author = _txtCommentAuthor.Text.Trim();
        var content = _txtCommentContent.Text.Trim();
        if (string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(content))
        {
            ToastNotification.Show("작성자와 댓글 내용을 입력하세요.", ToastType.Warning);
            return;
        }

        await ButtonFactory.RunWithLoadingAsync(_btnAddComment, "...", async () =>
        {
            await _repo.InsertCommentAsync(_selected.Id, author, content);
            _txtCommentContent.Clear();
            await LoadDetailAsync(_selected);
        });
        ToastNotification.Show("댓글 등록 완료.", ToastType.Success);
    }

    private async void BtnNew_Click(object? sender, EventArgs e)
    {
        using var dlg = new HandoverWriteDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        await _repo.InsertHandoverAsync(dlg.AuthorName, dlg.ContentText, dlg.TitleText);
        ToastNotification.Show("인수인계가 등록되었습니다.", ToastType.Success);
        _page = 1;
        await LoadAsync();
    }

    private const int Panel2MinTarget = 320;

    private void LayoutSplit()
    {
        if (_split.Width <= 0) return;

        var room = _split.Width - _split.SplitterWidth;
        if (room <= 0) return;

        // Width가 작을 때는 MinSize도 비례 축소해 SplitterDistance 재조정 throw 방지.
        var p1Min = Math.Max(0, Math.Min(MinListWidth, room / 2));
        var p2Min = Math.Max(0, Math.Min(Panel2MinTarget, room - p1Min));

        try { _split.Panel1MinSize = p1Min; } catch { }
        try { _split.Panel2MinSize = p2Min; } catch { }

        var desired = Math.Max(p1Min, (int)(_split.Width * 0.34));
        var max = _split.Width - p2Min - _split.SplitterWidth;
        if (max < p1Min) return;
        desired = Math.Min(desired, max);
        if (desired > 0 && desired < _split.Width)
        {
            try { _split.SplitterDistance = desired; } catch { }
        }
    }

    private void LayoutDetailPanel()
    {
        if (_detailPanel.ClientSize.Height <= 0) return;

        const int headerHeight = 116;
        const int minContentHeight = 130;
        var clientHeight = _detailPanel.ClientSize.Height - _detailPanel.Padding.Vertical;
        var desired = Math.Clamp((int)(clientHeight * 0.34), 160, 260);
        var maxWithoutCrushingContent = Math.Max(120, clientHeight - headerHeight - minContentHeight);
        _commentsShell.Height = Math.Min(desired, maxWithoutCrushingContent);
    }

    private void LayoutListCards()
    {
        var width = Math.Max(220, _listPanel.ClientSize.Width - 12);
        foreach (Control control in _listPanel.Controls)
        {
            if (control.Tag is not Handover)
                continue;

            control.Width = width;
            foreach (Control child in control.Controls)
            {
                if (child.Anchor.HasFlag(AnchorStyles.Right))
                    continue;

                child.Width = Math.Max(40, control.Width - child.Left - 12);
            }
        }
    }

    private void LayoutCommentCards()
    {
        var y = 0;
        var width = Math.Max(260, _commentListPanel.ClientSize.Width - 16);
        foreach (Control control in _commentListPanel.Controls)
        {
            if (control.Tag is not HandoverComment)
                continue;

            control.Location = new Point(0, y);
            control.Width = width;
            y += control.Height + 8;
        }
    }

    private static string GetPreview(string text)
    {
        var normalized = text.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= 42 ? normalized : $"{normalized[..42]}...";
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
        Size = new Size(520, 430);
        MinimumSize = new Size(460, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);
        BackColor = ColorPalette.Surface;
        Padding = new Padding(18);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = ColorPalette.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        _txtAuthor = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 10f),
            Margin = new Padding(0, 3, 0, 7)
        };
        _txtTitle = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "인수인계 제목",
            Font = new Font("맑은 고딕", 10f),
            Margin = new Padding(0, 3, 0, 7)
        };
        _txtContent = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "인수인계 내용을 입력하세요.",
            Font = new Font("맑은 고딕", 10f),
            Margin = new Padding(0, 3, 0, 7)
        };

        layout.Controls.Add(CreateDialogLabel("작성자"), 0, 0);
        layout.Controls.Add(_txtAuthor, 1, 0);
        layout.Controls.Add(CreateDialogLabel("제목"), 0, 1);
        layout.Controls.Add(_txtTitle, 1, 1);
        layout.Controls.Add(CreateDialogLabel("내용"), 0, 2);
        layout.Controls.Add(_txtContent, 1, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = ColorPalette.Surface
        };

        var btnOk = ButtonFactory.CreatePrimary("등록", 84);
        btnOk.Margin = new Padding(8, 3, 0, 0);
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

        var btnCancel = ButtonFactory.CreateGhost("취소", 84);
        btnCancel.Margin = new Padding(8, 3, 0, 0);
        btnCancel.DialogResult = DialogResult.Cancel;

        actions.Controls.Add(btnOk);
        actions.Controls.Add(btnCancel);
        layout.Controls.Add(actions, 1, 4);

        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        _txtAuthor.TabIndex = 0;
        _txtTitle.TabIndex = 1;
        _txtContent.TabIndex = 2;
        btnOk.TabIndex = 3;
    }

    private static Label CreateDialogLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
        ForeColor = ColorPalette.TextSecondary,
        TextAlign = ContentAlignment.TopLeft,
        Padding = new Padding(0, 6, 0, 0)
    };
}
