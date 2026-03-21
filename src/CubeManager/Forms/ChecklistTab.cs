using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using CubeManager.Helpers;

namespace CubeManager.Forms;

/// <summary>
/// 체크리스트 탭. 요일+역할별 할일 + 근무자 매칭 + 진행률 표시.
/// </summary>
public class ChecklistTab : UserControl
{
    private readonly IChecklistRepository _checklistRepo;
    private readonly IScheduleService _scheduleService;
    private readonly IEmployeeService _employeeService;

    private readonly Label _lblDate;
    private readonly Label _lblWorkers;
    private readonly Panel _checkPanel;
    private readonly Panel _progressPanel;
    private DateTime _currentDate = DateTime.Today;
    private List<ChecklistRecord> _records = [];

    private static readonly Font TaskFont = new("맑은 고딕", 11f);
    private static readonly Font TaskBoldFont = new("맑은 고딕", 11f, FontStyle.Bold);
    private static readonly Font SmallFont = new("맑은 고딕", 9f);
    private static readonly Font StrikeFont = new("맑은 고딕", 11f, FontStyle.Strikeout);
    private static readonly Font RoleHeaderFont = new("맑은 고딕", 10f, FontStyle.Bold);

    private static readonly Dictionary<string, string> RoleNames = new()
    {
        ["open"] = "🌅 오픈",
        ["close"] = "🌙 마감",
        ["middle1"] = "☀️ 1미들",
        ["middle2"] = "☀️ 2미들",
        ["all"] = "📋 전체"
    };

    private static readonly Dictionary<string, Color> RoleColors = new()
    {
        ["open"] = Color.FromArgb(227, 242, 253),    // 파랑 라이트
        ["close"] = Color.FromArgb(255, 243, 224),    // 주황 라이트
        ["middle1"] = Color.FromArgb(232, 245, 233),  // 초록 라이트
        ["middle2"] = Color.FromArgb(232, 245, 233),
        ["all"] = Color.FromArgb(245, 245, 245)
    };

    public ChecklistTab(IChecklistRepository checklistRepo,
        IScheduleService scheduleService, IEmployeeService employeeService)
    {
        _checklistRepo = checklistRepo;
        _scheduleService = scheduleService;
        _employeeService = employeeService;
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(20);

        // ===== 상단 헤더 =====
        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 45,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 0, 5)
        };

        topBar.Controls.Add(new Label
        {
            Text = "오늘의 체크리스트",
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            Size = new Size(200, 32),
            TextAlign = ContentAlignment.MiddleLeft
        });

        var btnPrev = ButtonFactory.CreateNavArrow("◀");
        btnPrev.Click += (_, _) => { _currentDate = _currentDate.AddDays(-1); _ = LoadAsync(); };
        topBar.Controls.Add(btnPrev);

        _lblDate = new Label
        {
            Size = new Size(180, 32),
            Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            TextAlign = ContentAlignment.MiddleCenter
        };
        topBar.Controls.Add(_lblDate);

        var btnNext = ButtonFactory.CreateNavArrow("▶");
        btnNext.Click += (_, _) => { _currentDate = _currentDate.AddDays(1); _ = LoadAsync(); };
        topBar.Controls.Add(btnNext);

        var btnToday = ButtonFactory.CreateGhost("오늘", 60);
        btnToday.Click += (_, _) => { _currentDate = DateTime.Today; _ = LoadAsync(); };
        topBar.Controls.Add(btnToday);

        // ===== 근무자 라벨 =====
        _lblWorkers = new Label
        {
            Dock = DockStyle.Top, Height = 30,
            Font = new Font("맑은 고딕", 10f),
            ForeColor = ColorPalette.AccentBlue.Main,
            Padding = new Padding(0, 5, 0, 0)
        };

        // ===== 진행률 바 =====
        _progressPanel = new Panel
        {
            Dock = DockStyle.Bottom, Height = 40,
            Padding = new Padding(0, 10, 0, 0)
        };
        _progressPanel.Paint += ProgressPanel_Paint;

        // ===== 체크리스트 패널 =====
        _checkPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 5, 0, 5)
        };

        Controls.Add(_checkPanel);
        Controls.Add(_progressPanel);
        Controls.Add(_lblWorkers);
        Controls.Add(topBar);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _lblDate.Text = _currentDate.ToString("yyyy-MM-dd (ddd)");

        // 근무자 로드
        try
        {
            var schedules = await _scheduleService.GetByDateAsync(_currentDate.ToString("yyyy-MM-dd"));
            var empNames = schedules.Select(s => s.EmployeeName).Distinct().ToList();
            _lblWorkers.Text = empNames.Count > 0
                ? $"👤 근무자: {string.Join(", ", empNames)}"
                : "👤 근무자: (스케줄 없음)";
        }
        catch { _lblWorkers.Text = "👤 근무자: -"; }

        // 체크리스트 로드
        try
        {
            _records = (await _checklistRepo.GetRecordsForDateAsync(
                _currentDate.ToString("yyyy-MM-dd"))).ToList();
        }
        catch { _records = []; }

        RenderChecklist();
        _progressPanel.Invalidate();
    }

    private void RenderChecklist()
    {
        _checkPanel.SuspendLayout();
        _checkPanel.Controls.Clear();

        if (_records.Count == 0)
        {
            _checkPanel.Controls.Add(new Label
            {
                Text = "등록된 체크리스트가 없습니다.\n관리자 탭에서 항목을 추가하세요.",
                Font = new Font("맑은 고딕", 11f),
                ForeColor = ColorPalette.TextTertiary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });
            _checkPanel.ResumeLayout();
            return;
        }

        // 역할별 그룹핑
        var grouped = _records.GroupBy(r => r.Role).OrderBy(g => RoleOrder(g.Key));

        var y = 5;
        foreach (var group in grouped)
        {
            var roleName = RoleNames.GetValueOrDefault(group.Key, group.Key);
            var roleColor = RoleColors.GetValueOrDefault(group.Key, ColorPalette.Surface);

            // 역할 헤더
            var headerPanel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(_checkPanel.Width - 20, 30),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BackColor = roleColor,
                Padding = new Padding(10, 0, 0, 0)
            };
            headerPanel.Controls.Add(new Label
            {
                Text = roleName,
                Font = RoleHeaderFont,
                ForeColor = ColorPalette.Text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            _checkPanel.Controls.Add(headerPanel);
            y += 32;

            foreach (var record in group.OrderBy(r => r.SortOrder))
            {
                var row = CreateCheckRow(record, y);
                _checkPanel.Controls.Add(row);
                y += 48;
            }

            y += 8; // 그룹 간 여백
        }

        _checkPanel.ResumeLayout();
    }

    private static int RoleOrder(string role) => role switch
    {
        "all" => 0,
        "open" => 1,
        "middle1" => 2,
        "middle2" => 3,
        "close" => 4,
        _ => 5
    };

    private Panel CreateCheckRow(ChecklistRecord record, int y)
    {
        var panel = new Panel
        {
            Location = new Point(0, y),
            Size = new Size(_checkPanel.Width - 20, 44),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            BackColor = record.IsChecked ? ColorPalette.SuccessLight : ColorPalette.Surface,
            Padding = new Padding(10, 0, 10, 0)
        };

        var chk = new CheckBox
        {
            Checked = record.IsChecked,
            Location = new Point(10, 10),
            Size = new Size(22, 22),
            Tag = record.TemplateId
        };
        chk.CheckedChanged += async (_, _) =>
        {
            try
            {
                await _checklistRepo.UpsertRecordAsync(
                    record.TemplateId,
                    _currentDate.ToString("yyyy-MM-dd"),
                    chk.Checked,
                    chk.Checked ? "직원" : null);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ToastNotification.Show(ex.Message, ToastType.Error);
            }
        };
        panel.Controls.Add(chk);

        var lblTask = new Label
        {
            Text = record.TaskText ?? "",
            Font = record.IsChecked ? StrikeFont : TaskBoldFont,
            ForeColor = record.IsChecked ? ColorPalette.TextTertiary : ColorPalette.Text,
            Location = new Point(40, 10),
            Size = new Size(400, 24),
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(lblTask);

        if (record.IsChecked && record.CheckedBy != null)
        {
            var lblInfo = new Label
            {
                Text = $"✓ {record.CheckedBy} {record.CheckedAt}",
                Font = SmallFont,
                ForeColor = ColorPalette.Success,
                Location = new Point(panel.Width - 200, 12),
                Size = new Size(180, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            panel.Controls.Add(lblInfo);
        }

        var divider = new Panel
        {
            Dock = DockStyle.Bottom, Height = 1,
            BackColor = ColorPalette.Divider
        };
        panel.Controls.Add(divider);

        return panel;
    }

    // ===== 진행률 바 =====
    private void ProgressPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var total = _records.Count;
        var done = _records.Count(r => r.IsChecked);
        var pct = total > 0 ? (float)done / total : 0;

        var barRect = new Rectangle(0, 8, _progressPanel.Width - 1, 16);

        using var bgBrush = new SolidBrush(ColorPalette.Border);
        using var bgPath = CubeManager.Controls.RoundedCard.CreateRoundedPath(barRect, 8);
        g.FillPath(bgBrush, bgPath);

        if (pct > 0)
        {
            var fillW = Math.Max(16, (int)(barRect.Width * pct));
            var fillRect = new Rectangle(0, 8, fillW, 16);
            using var fillBrush = new SolidBrush(ColorPalette.Primary);
            using var fillPath = CubeManager.Controls.RoundedCard.CreateRoundedPath(fillRect, 8);
            g.FillPath(fillBrush, fillPath);
        }

        var text = $"{done}/{total} ({(int)(pct * 100)}%)";
        using var textBrush = new SolidBrush(ColorPalette.TextSecondary);
        using var font = new Font("맑은 고딕", 9f);
        var textSize = g.MeasureString(text, font);
        g.DrawString(text, font, textBrush,
            (_progressPanel.Width - textSize.Width) / 2, 28);
    }
}
