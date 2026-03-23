using System.Drawing;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class HandoverTab : UserControl
{
    private readonly IHandoverRepository _repo;
    private readonly Panel _listPanel;
    private readonly TextBox _txtSearch;
    private readonly Label _lblPageInfo;
    private int _page = 1;
    private const int PageSize = 10;

    public HandoverTab(IHandoverRepository repo)
    {
        _repo = repo;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(15);

        var topBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 5) };
        topBar.Controls.Add(new Label
        {
            Text = "인수인계", Size = new Size(100, 32),
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
        });

        var btnNew = ButtonFactory.CreatePrimary("새 글 작성", 100);
        btnNew.Margin = new Padding(10, 0, 0, 0);
        btnNew.Click += BtnNew_Click;

        _txtSearch = new TextBox
        {
            PlaceholderText = "검색...", Size = new Size(180, 28), Margin = new Padding(20, 2, 0, 0)
        };
        _txtSearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { _page = 1; _ = LoadAsync(); } };

        topBar.Controls.AddRange([btnNew, _txtSearch]);

        _listPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ColorPalette.Background
        };

        var navBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 35, Padding = new Padding(0, 5, 0, 0) };
        var btnPrev = ButtonFactory.CreateGhost("◀ 이전", 70);
        btnPrev.Click += (_, _) => { if (_page > 1) { _page--; _ = LoadAsync(); } };
        _lblPageInfo = new Label
        {
            Size = new Size(100, 28), TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("맑은 고딕", 9f), ForeColor = ColorPalette.TextSecondary,
            Margin = new Padding(0, 2, 0, 0)
        };
        var btnNextPage = ButtonFactory.CreateGhost("다음 ▶", 70);
        btnNextPage.Click += (_, _) => { _page++; _ = LoadAsync(); };
        navBar.Controls.AddRange([btnPrev, _lblPageInfo, btnNextPage]);

        Controls.Add(_listPanel);
        Controls.Add(navBar);
        Controls.Add(topBar);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var keyword = _txtSearch.Text.Trim();
        var (items, totalCount) = await _repo.GetPagedAsync(_page, PageSize,
            string.IsNullOrEmpty(keyword) ? null : keyword);

        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        _lblPageInfo.Text = $"{_page} / {totalPages} 페이지";

        _listPanel.Controls.Clear();

        if (items.Count() == 0)
        {
            _listPanel.Controls.Add(new Label
            {
                Text = _page == 1 ? "📋 아직 인수인계가 없습니다.\n'새 글 작성' 버튼을 눌러 첫 글을 남겨보세요."
                    : "더 이상 글이 없습니다.",
                Font = new Font("맑은 고딕", 11f),
                ForeColor = ColorPalette.TextTertiary,
                Size = new Size(400, 60),
                Location = new Point(20, 30),
                TextAlign = ContentAlignment.MiddleLeft
            });
            return;
        }

        var y = 5;
        foreach (var h in items)
        {
            var card = CreateCard(h);
            card.Location = new Point(5, y);
            card.Width = _listPanel.Width - 30;
            _listPanel.Controls.Add(card);
            y += card.Height + 8;
        }
    }

    private Panel CreateCard(Handover h)
    {
        var card = new Panel
        {
            Size = new Size(600, 100), BackColor = ColorPalette.Surface,
            BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(10)
        };

        card.Controls.Add(new Label
        {
            Text = $"{h.AuthorName}  │  {h.CreatedAt:yyyy-MM-dd HH:mm}",
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary,
            Dock = DockStyle.Top, Height = 20
        });

        card.Controls.Add(new Label
        {
            Text = h.Content.Length > 150 ? h.Content[..150] + "..." : h.Content,
            Font = new Font("맑은 고딕", 10f),
            ForeColor = ColorPalette.Text,
            Location = new Point(10, 28), Size = new Size(560, 55),
            AutoEllipsis = true
        });

        // 댓글 추가 버튼
        var btnComment = new LinkLabel
        {
            Text = "댓글 달기",
            Location = new Point(10, 75), Size = new Size(80, 18),
            Font = new Font("맑은 고딕", 8f)
        };
        btnComment.Click += async (_, _) =>
        {
            var author = InputDialog.Show("작성자:", "댓글");
            if (string.IsNullOrEmpty(author)) return;
            var content = InputDialog.Show("내용:", "댓글");
            if (string.IsNullOrEmpty(content)) return;
            await _repo.InsertCommentAsync(h.Id, author, content);
            ToastNotification.Show("댓글이 등록되었습니다.", ToastType.Success);
        };
        card.Controls.Add(btnComment);

        return card;
    }

    private async void BtnNew_Click(object? sender, EventArgs e)
    {
        var author = InputDialog.Show("작성자:", "인수인계 작성");
        if (string.IsNullOrEmpty(author)) return;
        var content = InputDialog.Show("내용:", "인수인계 작성");
        if (string.IsNullOrEmpty(content)) return;

        await _repo.InsertHandoverAsync(author, content);
        ToastNotification.Show("인수인계가 등록되었습니다.", ToastType.Success);
        _page = 1;
        await LoadAsync();
    }
}
