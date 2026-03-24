using System.Drawing;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Dialogs;
using CubeManager.Helpers;

namespace CubeManager.Forms;

/// <summary>
/// 테마 힌트 관리 탭.
/// 좌측: 테마 목록 | 우측: 선택된 테마의 힌트 그리드 + JSON Export
/// </summary>
public class ThemeHintTab : UserControl
{
    private readonly IThemeRepository _themeRepo;
    private readonly IThemeExportService _exportService;

    // 좌측 테마 목록
    private readonly Panel _themeListPanel;
    private readonly List<Theme> _themes = [];
    private int _selectedThemeId = -1;

    // 우측 힌트 그리드
    private readonly Label _lblSelectedTheme;
    private readonly DataGridView _hintGrid;

    public ThemeHintTab(IThemeRepository themeRepo, IThemeExportService exportService)
    {
        _themeRepo = themeRepo;
        _exportService = exportService;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Background;
        Padding = new Padding(16);

        // === 상단 헤더 ===
        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 50 };

        var lblTitle = new Label
        {
            Text = "테마 힌트 관리",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Location = new Point(0, 10),
            AutoSize = true
        };

        var btnExportAll = ButtonFactory.CreateSecondary("전체 Export");
        btnExportAll.Location = new Point(0, 10);
        btnExportAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnExportAll.Click += BtnExportAll_Click;

        var btnExportTheme = ButtonFactory.CreateGhost("선택 Export");
        btnExportTheme.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnExportTheme.Click += BtnExportTheme_Click;

        headerPanel.Controls.AddRange([lblTitle, btnExportAll, btnExportTheme]);
        headerPanel.Resize += (_, _) =>
        {
            btnExportAll.Location = new Point(headerPanel.Width - btnExportAll.Width - btnExportTheme.Width - 10, 10);
            btnExportTheme.Location = new Point(headerPanel.Width - btnExportTheme.Width, 10);
        };

        // === 좌측 테마 패널 (250px) ===
        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 250,
            Padding = new Padding(0, 0, 12, 0)
        };

        var btnAddTheme = ButtonFactory.CreatePrimary("+ 테마 추가", 226);
        btnAddTheme.Dock = DockStyle.Top;
        btnAddTheme.Height = 40;
        btnAddTheme.Click += BtnAddTheme_Click;

        _themeListPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ColorPalette.Surface
        };

        leftPanel.Controls.Add(_themeListPanel);
        leftPanel.Controls.Add(btnAddTheme);

        // === 우측 힌트 패널 ===
        var rightPanel = new Panel { Dock = DockStyle.Fill };

        var rightHeader = new Panel { Dock = DockStyle.Top, Height = 45 };
        _lblSelectedTheme = new Label
        {
            Text = "테마를 선택하세요",
            Font = new Font("맑은 고딕", 13f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary,
            Location = new Point(4, 10),
            AutoSize = true
        };

        var btnAddHint = ButtonFactory.CreatePrimary("+ 힌트 추가");
        btnAddHint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAddHint.Click += BtnAddHint_Click;
        rightHeader.Controls.AddRange([_lblSelectedTheme, btnAddHint]);
        rightHeader.Resize += (_, _) =>
        {
            btnAddHint.Location = new Point(rightHeader.Width - btnAddHint.Width - 4, 6);
        };

        _hintGrid = new DataGridView { Dock = DockStyle.Fill };
        GridTheme.ApplyTheme(_hintGrid);
        SetupHintColumns();
        _hintGrid.CellDoubleClick += HintGrid_CellDoubleClick;
        _hintGrid.CellContentClick += HintGrid_CellContentClick;

        rightPanel.Controls.Add(_hintGrid);
        rightPanel.Controls.Add(rightHeader);

        // === 조립 ===
        Controls.Add(rightPanel);
        Controls.Add(leftPanel);
        Controls.Add(headerPanel);

        _ = LoadThemesAsync();
    }

    // ==================== 테마 목록 ====================

    private async Task LoadThemesAsync()
    {
        try
        {
            _themes.Clear();
            _themes.AddRange(await _themeRepo.GetAllThemesAsync());
            RenderThemeList();

            // 첫 테마 자동 선택
            if (_themes.Count > 0 && _selectedThemeId < 0)
                await SelectThemeAsync(_themes[0].Id);
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"테마 로드 실패: {ex.Message}", ToastType.Error);
        }
    }

    private void RenderThemeList()
    {
        _themeListPanel.Controls.Clear();
        var y = 4;
        foreach (var theme in _themes)
        {
            var card = CreateThemeCard(theme, y);
            _themeListPanel.Controls.Add(card);
            y += 52;
        }
    }

    private Panel CreateThemeCard(Theme theme, int top)
    {
        var isSelected = theme.Id == _selectedThemeId;
        var normalBg = isSelected ? ColorPalette.Card : ColorPalette.Surface;       // 선택: Card(#3F425D), 일반: Surface(#2D3047)
        var hoverBg = isSelected ? ColorPalette.Card : ColorPalette.NavHoverBg;      // hover: #353850

        var card = new Panel
        {
            Location = new Point(4, top),
            Size = new Size(220, 46),
            BackColor = normalBg,
            Cursor = Cursors.Hand,
            Tag = theme.Id
        };

        var lbl = new Label
        {
            Text = theme.ThemeName,
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
            ForeColor = isSelected ? ColorPalette.Accent : ColorPalette.Text,       // 선택: 주황(보색), 일반: 밝은 흰
            Location = new Point(12, 4),
            Size = new Size(196, 22),
            Cursor = Cursors.Hand
        };

        var lblDesc = new Label
        {
            Text = theme.Description ?? (theme.IsActive ? "활성" : "비활성"),
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = ColorPalette.TextTertiary,
            Location = new Point(12, 24),
            Size = new Size(196, 18),
            Cursor = Cursors.Hand
        };

        // hover 효과 (테마 가이드 색상)
        void SetHover(bool hover)
        {
            if (isSelected) return; // 선택된 카드는 hover 변경 안 함
            card.BackColor = hover ? hoverBg : normalBg;
        }
        card.MouseEnter += (_, _) => SetHover(true);
        card.MouseLeave += (_, _) => SetHover(false);
        lbl.MouseEnter += (_, _) => SetHover(true);
        lbl.MouseLeave += (_, _) => SetHover(false);
        lblDesc.MouseEnter += (_, _) => SetHover(true);
        lblDesc.MouseLeave += (_, _) => SetHover(false);

        // 클릭 이벤트 (카드 전체)
        EventHandler onClick = async (_, _) => await SelectThemeAsync(theme.Id);
        card.Click += onClick;
        lbl.Click += onClick;
        lblDesc.Click += onClick;

        // 우클릭 컨텍스트 메뉴
        var ctx = new ContextMenuStrip();
        ctx.Items.Add("수정", null, async (_, _) => await EditThemeAsync(theme));
        ctx.Items.Add("삭제", null, async (_, _) => await DeleteThemeAsync(theme));
        card.ContextMenuStrip = ctx;
        lbl.ContextMenuStrip = ctx;

        card.Controls.AddRange([lbl, lblDesc]);
        return card;
    }

    private async Task SelectThemeAsync(int themeId)
    {
        _selectedThemeId = themeId;
        RenderThemeList(); // 선택 스타일 갱신

        var theme = _themes.FirstOrDefault(t => t.Id == themeId);
        _lblSelectedTheme.Text = theme?.ThemeName ?? "테마를 선택하세요";
        _lblSelectedTheme.ForeColor = ColorPalette.Text;

        await LoadHintsAsync(themeId);
    }

    // ==================== 테마 CRUD ====================

    private async void BtnAddTheme_Click(object? sender, EventArgs e)
    {
        using var dlg = new ThemeEditDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var theme = new Theme { ThemeName = dlg.ThemeName, Description = dlg.Description };
            var id = await _themeRepo.InsertThemeAsync(theme);
            ToastNotification.Show($"'{dlg.ThemeName}' 테마가 추가되었습니다.", ToastType.Success);
            await LoadThemesAsync();
            await SelectThemeAsync(id);
        }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }

    private async Task EditThemeAsync(Theme theme)
    {
        using var dlg = new ThemeEditDialog(theme);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            theme.ThemeName = dlg.ThemeName;
            theme.Description = dlg.Description;
            await _themeRepo.UpdateThemeAsync(theme);
            ToastNotification.Show("테마가 수정되었습니다.", ToastType.Success);
            await LoadThemesAsync();
        }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }

    private async Task DeleteThemeAsync(Theme theme)
    {
        if (MessageBox.Show($"'{theme.ThemeName}' 테마를 삭제하시겠습니까?\n포함된 모든 힌트도 삭제됩니다.",
                "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            await _themeRepo.DeleteThemeAsync(theme.Id);
            ToastNotification.Show($"'{theme.ThemeName}' 삭제 완료.", ToastType.Success);
            _selectedThemeId = -1;
            await LoadThemesAsync();
        }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }

    // ==================== 힌트 그리드 ====================

    private void SetupHintColumns()
    {
        _hintGrid.Columns.Clear();
        _hintGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
        _hintGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "HintCode", HeaderText = "힌트코드", Width = 90, ReadOnly = true,
            DefaultCellStyle = GridTheme.CenterStyle
        });
        _hintGrid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Question", HeaderText = "문제", FillWeight = 30, ReadOnly = true });
        _hintGrid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Hint1", HeaderText = "힌트 1", FillWeight = 25, ReadOnly = true });
        _hintGrid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Hint2", HeaderText = "힌트 2", FillWeight = 20, ReadOnly = true });
        _hintGrid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Answer", HeaderText = "정답", FillWeight = 25, ReadOnly = true });
        _hintGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Delete", HeaderText = "삭제", Width = 60,
            Text = "🗑", UseColumnTextForButtonValue = true,
            DefaultCellStyle = GridTheme.CenterStyle
        });
    }

    private async Task LoadHintsAsync(int themeId)
    {
        try
        {
            var hints = await _themeRepo.GetHintsByThemeIdAsync(themeId);
            _hintGrid.Rows.Clear();

            foreach (var h in hints)
            {
                var idx = _hintGrid.Rows.Add();
                var row = _hintGrid.Rows[idx];
                row.Cells["Id"].Value = h.Id;
                row.Cells["HintCode"].Value = h.HintCode;
                row.Cells["Question"].Value = h.Question;
                row.Cells["Hint1"].Value = h.Hint1;
                row.Cells["Hint2"].Value = h.Hint2 ?? "";
                row.Cells["Answer"].Value = h.Answer;
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show($"힌트 로드 실패: {ex.Message}", ToastType.Error);
        }
    }

    // ==================== 힌트 CRUD ====================

    private async void BtnAddHint_Click(object? sender, EventArgs e)
    {
        if (_selectedThemeId < 0)
        {
            ToastNotification.Show("테마를 먼저 선택하세요.", ToastType.Warning);
            return;
        }

        using var dlg = new HintEditDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            // 힌트코드 중복 체크
            var code = dlg.HintCode;
            while (await _themeRepo.IsHintCodeExistsAsync(_selectedThemeId, code))
                code = Random.Shared.Next(1000, 10000);

            var hint = new ThemeHint
            {
                ThemeId = _selectedThemeId,
                HintCode = code,
                Question = dlg.Question,
                Hint1 = dlg.Hint1Text,
                Hint2 = dlg.Hint2Text,
                Answer = dlg.Answer
            };

            await _themeRepo.InsertHintAsync(hint);
            ToastNotification.Show("힌트가 추가되었습니다.", ToastType.Success);
            await LoadHintsAsync(_selectedThemeId);
        }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }

    private async void HintGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _hintGrid.Columns[e.ColumnIndex].Name == "Delete") return;

        var id = (int)_hintGrid.Rows[e.RowIndex].Cells["Id"].Value;
        var hint = await _themeRepo.GetHintByIdAsync(id);
        if (hint == null) return;

        using var dlg = new HintEditDialog(hint);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            hint.HintCode = dlg.HintCode;
            hint.Question = dlg.Question;
            hint.Hint1 = dlg.Hint1Text;
            hint.Hint2 = dlg.Hint2Text;
            hint.Answer = dlg.Answer;

            await _themeRepo.UpdateHintAsync(hint);
            ToastNotification.Show("힌트가 수정되었습니다.", ToastType.Success);
            await LoadHintsAsync(_selectedThemeId);
        }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }

    private async void HintGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _hintGrid.Columns[e.ColumnIndex].Name != "Delete") return;

        var id = (int)_hintGrid.Rows[e.RowIndex].Cells["Id"].Value;
        var code = _hintGrid.Rows[e.RowIndex].Cells["HintCode"].Value;

        if (MessageBox.Show($"힌트 [{code}]를 삭제하시겠습니까?", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            await _themeRepo.DeleteHintAsync(id);
            ToastNotification.Show("힌트가 삭제되었습니다.", ToastType.Success);
            await LoadHintsAsync(_selectedThemeId);
        }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }

    // ==================== JSON Export ====================

    private async void BtnExportAll_Click(object? sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "JSON 파일|*.json",
            FileName = $"themes_all_{DateTime.Now:yyyyMMdd}.json",
            Title = "전체 테마 Export"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            await _exportService.ExportAllToJsonAsync(sfd.FileName);
            ToastNotification.Show($"전체 테마 Export 완료: {Path.GetFileName(sfd.FileName)}", ToastType.Success);
        }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }

    private async void BtnExportTheme_Click(object? sender, EventArgs e)
    {
        if (_selectedThemeId < 0)
        {
            ToastNotification.Show("테마를 먼저 선택하세요.", ToastType.Warning);
            return;
        }

        var theme = _themes.FirstOrDefault(t => t.Id == _selectedThemeId);
        using var sfd = new SaveFileDialog
        {
            Filter = "JSON 파일|*.json",
            FileName = $"theme_{theme?.ThemeName ?? "unknown"}_{DateTime.Now:yyyyMMdd}.json",
            Title = "선택 테마 Export"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            await _exportService.ExportThemeToJsonAsync(_selectedThemeId, sfd.FileName);
            ToastNotification.Show($"'{theme?.ThemeName}' Export 완료.", ToastType.Success);
        }
        catch (Exception ex) { ToastNotification.Show(ex.Message, ToastType.Error); }
    }
}
