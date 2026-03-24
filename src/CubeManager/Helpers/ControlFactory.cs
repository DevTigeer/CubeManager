using System.Drawing;
using System.Drawing.Drawing2D;

namespace CubeManager.Helpers;

/// <summary>
/// 현대식 WinForms 컨트롤 팩토리.
/// 모든 기본 컨트롤(TextBox, CheckBox, ComboBox, DateTimePicker 등)을
/// 둥근 모서리 + ColorPalette 기반으로 스타일링.
/// 성능 영향: 0 (FlatStyle + Region + 1회성 이벤트 바인딩)
/// </summary>
public static class ControlFactory
{
    private const int InputHeight = 34;
    private const int Radius = 6;

    // ═══════════════════════════════════════════
    // TextBox — 둥근 테두리 Panel로 래핑
    // ═══════════════════════════════════════════

    /// <summary>모던 TextBox: 둥근 테두리 + Focus 색상 변경</summary>
    public static (Panel container, TextBox textBox) CreateTextBox(int width = 200, bool multiline = false)
    {
        var textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("맑은 고딕", 10f),
            ForeColor = ColorPalette.Text,
            BackColor = ColorPalette.Surface,
            Width = width - 16,
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };

        if (multiline)
        {
            textBox.Multiline = true;
            textBox.ScrollBars = ScrollBars.Vertical;
        }

        var container = new RoundedInputPanel(Radius)
        {
            Width = width,
            Height = multiline ? 80 : InputHeight,
            Padding = new Padding(8, multiline ? 6 : 7, 8, 0),
            BackColor = ColorPalette.Surface
        };

        textBox.GotFocus += (_, _) => container.IsFocused = true;
        textBox.LostFocus += (_, _) => container.IsFocused = false;
        textBox.Location = new Point(8, multiline ? 6 : (InputHeight - textBox.Height) / 2);
        container.Controls.Add(textBox);

        return (container, textBox);
    }

    // ═══════════════════════════════════════════
    // ComboBox — Flat + 커스텀 색상
    // ═══════════════════════════════════════════

    /// <summary>모던 ComboBox: Flat 스타일 + 색상 통일</summary>
    public static ComboBox CreateComboBox(int width = 160)
    {
        var combo = new ComboBox
        {
            FlatStyle = FlatStyle.Flat,
            Font = new Font("맑은 고딕", 10f),
            ForeColor = ColorPalette.Text,
            BackColor = ColorPalette.Surface,
            Width = width,
            Height = InputHeight,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 30
        };

        combo.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var isSelected = (e.State & DrawItemState.Selected) != 0;
            var bg = isSelected ? ColorPalette.Primary50 : ColorPalette.Surface;
            var fg = isSelected ? ColorPalette.Primary : ColorPalette.Text;

            using var bgBrush = new SolidBrush(bg);
            g.FillRectangle(bgBrush, e.Bounds);

            if (isSelected)
            {
                // 좌측 2px Primary 바
                using var barBrush = new SolidBrush(ColorPalette.Primary);
                g.FillRectangle(barBrush, e.Bounds.X, e.Bounds.Y + 4, 2, e.Bounds.Height - 8);
            }

            using var textBrush = new SolidBrush(fg);
            using var font = new Font("맑은 고딕", 10f);
            var textRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height);
            var sf = new StringFormat { LineAlignment = StringAlignment.Center };
            g.DrawString(combo.GetItemText(combo.Items[e.Index]), font, textBrush, textRect, sf);
        };

        return combo;
    }

    // ═══════════════════════════════════════════
    // CheckBox — Flat + 커스텀 색상
    // ═══════════════════════════════════════════

    /// <summary>모던 CheckBox: Flat 스타일 + Primary 체크 색상</summary>
    public static CheckBox CreateCheckBox(string text, bool isChecked = false)
    {
        var chk = new CheckBox
        {
            Text = text,
            Checked = isChecked,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("맑은 고딕", 10f),
            ForeColor = ColorPalette.Text,
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        chk.FlatAppearance.BorderColor = ColorPalette.Border;
        chk.FlatAppearance.CheckedBackColor = ColorPalette.Primary;
        chk.FlatAppearance.MouseOverBackColor = ColorPalette.Primary50;

        return chk;
    }

    // ═══════════════════════════════════════════
    // NumericUpDown — 스타일 통일
    // ═══════════════════════════════════════════

    /// <summary>모던 NumericUpDown: 색상 통일 + 폰트</summary>
    public static NumericUpDown CreateNumeric(decimal min = 0, decimal max = 100, decimal value = 0, int width = 80)
    {
        var num = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            Width = width,
            Height = InputHeight,
            Font = new Font("Segoe UI", 10f),
            ForeColor = ColorPalette.Text,
            BackColor = ColorPalette.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Center
        };

        return num;
    }

    // ═══════════════════════════════════════════
    // DateTimePicker — 스타일 통일
    // ═══════════════════════════════════════════

    /// <summary>모던 DateTimePicker: 색상 통일</summary>
    public static DateTimePicker CreateDatePicker(int width = 150)
    {
        var dtp = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 10f),
            Width = width,
            Height = InputHeight,
            CalendarForeColor = ColorPalette.Text,
            CalendarMonthBackground = ColorPalette.Surface,
            CalendarTitleBackColor = ColorPalette.Primary,
            CalendarTitleForeColor = Color.White,
            CalendarTrailingForeColor = ColorPalette.TextTertiary
        };

        return dtp;
    }

    // ═══════════════════════════════════════════
    // Label — 계층별 생성
    // ═══════════════════════════════════════════

    /// <summary>페이지 제목 라벨 (16f Bold)</summary>
    public static Label CreatePageTitle(string text)
    {
        return new Label
        {
            Text = text,
            Font = DesignTokens.FontPageTitle,
            ForeColor = ColorPalette.Text,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, DesignTokens.SpaceSM)
        };
    }

    /// <summary>섹션 제목 라벨 (12f Bold, TextSecondary)</summary>
    public static Label CreateSectionTitle(string text)
    {
        return new Label
        {
            Text = text,
            Font = DesignTokens.FontSectionTitle,
            ForeColor = ColorPalette.TextSecondary,
            AutoSize = true,
            Padding = new Padding(0, DesignTokens.SpaceSM, 0, DesignTokens.SpaceXS)
        };
    }

    /// <summary>캡션/힌트 라벨 (9f, TextTertiary)</summary>
    public static Label CreateCaption(string text)
    {
        return new Label
        {
            Text = text,
            Font = DesignTokens.FontCaption,
            ForeColor = ColorPalette.TextTertiary,
            AutoSize = true
        };
    }

    // ═══════════════════════════════════════════
    // 기존 컨트롤에 모던 스타일 일괄 적용
    // ═══════════════════════════════════════════

    /// <summary>다이얼로그 전체를 다크 모던 스타일로 적용 (타이틀바 제거 + X 버튼)</summary>
    public static void ApplyModernDialog(Form form)
    {
        form.Font = DesignTokens.FontBody;
        form.BackColor = ColorPalette.Surface;
        form.ForeColor = ColorPalette.Text;

        // MainForm은 이미 FormBorderStyle.None — 서브 다이얼로그만 처리
        if (form.FormBorderStyle != FormBorderStyle.None && form.Owner != null)
        {
            var title = form.Text;
            form.FormBorderStyle = FormBorderStyle.None;

            // 커스텀 타이틀바 (35px)
            var titleBar = new Panel
            {
                Dock = DockStyle.Top, Height = 35,
                BackColor = ColorPalette.Card
            };

            // 제목 라벨
            var lblTitle = new Label
            {
                Text = title,
                Font = DesignTokens.FontTabMenu,
                ForeColor = ColorPalette.Text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };

            // X 닫기 버튼
            var btnX = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ColorPalette.TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(35, 35),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnX.FlatAppearance.BorderSize = 0;
            btnX.FlatAppearance.MouseOverBackColor = ColorPalette.Danger;
            btnX.Click += (_, _) =>
            {
                if (form.Modal)
                    form.DialogResult = DialogResult.Cancel;
                form.Close();
            };

            titleBar.Controls.Add(lblTitle);
            titleBar.Controls.Add(btnX);

            // 드래그 이동
            var dragging = false;
            var dragStart = Point.Empty;
            lblTitle.MouseDown += (_, me) => { dragging = true; dragStart = me.Location; };
            lblTitle.MouseMove += (_, me) =>
            {
                if (!dragging) return;
                form.Location = new Point(
                    form.Location.X + me.X - dragStart.X,
                    form.Location.Y + me.Y - dragStart.Y);
            };
            lblTitle.MouseUp += (_, _) => dragging = false;

            // 기존 컨트롤을 아래로 밀기
            form.Height += 35;
            form.Controls.Add(titleBar);
            titleBar.BringToFront();

            // 테두리 (1px)
            form.Paint += (_, pe) =>
            {
                using var pen = new Pen(ColorPalette.Border);
                pe.Graphics.DrawRectangle(pen, 0, 0, form.Width - 1, form.Height - 1);
            };
        }

        ApplyModernStyle(form);
    }

    /// <summary>앱 전역: 새 Form이 열릴 때 자동으로 모던 스타일 적용</summary>
    public static void EnableGlobalDialogStyling(Form mainForm)
    {
        // MainForm 자체는 스타일만 (타이틀바 이미 None)
        mainForm.Font = DesignTokens.FontBody;
        mainForm.BackColor = ColorPalette.Surface;
        mainForm.ForeColor = ColorPalette.Text;
        ApplyModernStyle(mainForm);

        // 이후 열리는 모든 자식 Form에도 적용
        mainForm.Activated += (_, _) =>
        {
            foreach (Form f in Application.OpenForms)
            {
                if (f == mainForm) continue;
                if (f.Tag as string != "__styled")
                {
                    f.Tag = "__styled";
                    ApplyModernDialog(f);
                }
            }
        };
    }

    /// <summary>TabControl OwnerDraw 모던 스타일 (하단 Primary 인디케이터)</summary>
    public static void StyleTabControl(TabControl tc)
    {
        tc.DrawMode = TabDrawMode.OwnerDrawFixed;
        tc.SizeMode = TabSizeMode.Fixed;
        tc.ItemSize = new Size(Math.Max(100, tc.Width / Math.Max(tc.TabCount, 1)), 36);
        tc.Padding = new Point(DesignTokens.SpaceMD, DesignTokens.SpaceSM);

        // 이미 바인딩된 핸들러 중복 방지 (Tag 체크)
        if (tc.Tag as string == "__tabStyled") return;
        tc.Tag = "__tabStyled";

        tc.DrawItem += (sender, e) =>
        {
            var tab = (TabControl)sender!;
            if (e.Index < 0 || e.Index >= tab.TabCount) return;
            var page = tab.TabPages[e.Index];
            var isSelected = e.Index == tab.SelectedIndex;
            var bounds = e.Bounds;

            using var bgBrush = new SolidBrush(
                isSelected ? ColorPalette.Surface : ColorPalette.Background);
            e.Graphics.FillRectangle(bgBrush, bounds);

            // 선택 탭 하단 Primary 인디케이터 (3px)
            if (isSelected)
            {
                using var barBrush = new SolidBrush(ColorPalette.Primary);
                e.Graphics.FillRectangle(barBrush,
                    bounds.X + 8, bounds.Bottom - 3, bounds.Width - 16, 3);
            }

            using var textFont = isSelected ? DesignTokens.FontTabMenu : DesignTokens.FontBody;
            using var textBrush = new SolidBrush(
                isSelected ? ColorPalette.Primary : ColorPalette.TextSecondary);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            e.Graphics.DrawString(page.Text, textFont, textBrush, bounds, sf);
        };
    }

    /// <summary>기존 Form/Panel 내 모든 컨트롤에 모던 스타일 적용</summary>
    public static void ApplyModernStyle(Control parent)
    {
        foreach (Control ctrl in parent.Controls)
        {
            switch (ctrl)
            {
                case TextBox tb when tb.BorderStyle != BorderStyle.None:
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.Font = DesignTokens.FontBody;
                    tb.ForeColor = ColorPalette.Text;
                    tb.BackColor = ColorPalette.Card;
                    break;

                case ComboBox cb:
                    cb.FlatStyle = FlatStyle.Flat;
                    cb.Font = DesignTokens.FontBody;
                    cb.ForeColor = ColorPalette.Text;
                    cb.BackColor = ColorPalette.Card;
                    break;

                case CheckBox chk when chk.FlatStyle != FlatStyle.Flat:
                    chk.FlatStyle = FlatStyle.Flat;
                    chk.Font = DesignTokens.FontBody;
                    chk.ForeColor = ColorPalette.Text;
                    chk.FlatAppearance.BorderColor = ColorPalette.Border;
                    chk.FlatAppearance.CheckedBackColor = ColorPalette.Primary;
                    break;

                case NumericUpDown nud:
                    nud.Font = DesignTokens.FontBody;
                    nud.ForeColor = ColorPalette.Text;
                    nud.BackColor = ColorPalette.Card;
                    break;

                case DateTimePicker dtp:
                    dtp.Font = DesignTokens.FontBody;
                    dtp.CalendarTitleBackColor = ColorPalette.Primary;
                    dtp.CalendarTitleForeColor = Color.White;
                    dtp.CalendarMonthBackground = ColorPalette.Card;
                    break;

                case GroupBox gb:
                    gb.Font = DesignTokens.FontSectionTitle;
                    gb.ForeColor = ColorPalette.TextSecondary;
                    gb.BackColor = ColorPalette.Surface;
                    break;

                case TabControl tc when tc.DrawMode != TabDrawMode.OwnerDrawFixed:
                    StyleTabControl(tc);
                    break;

                case Label lbl when lbl.ForeColor == SystemColors.ControlText:
                    lbl.ForeColor = ColorPalette.Text;
                    break;
            }

            // 모든 컨트롤 폰트 Bold 강제 (크기는 유지)
            if (ctrl.Font is { Bold: false } f)
                ctrl.Font = new Font(f.FontFamily, f.Size, f.Style | FontStyle.Bold);

            // 재귀 적용
            if (ctrl.HasChildren)
                ApplyModernStyle(ctrl);
        }
    }
}

/// <summary>
/// 둥근 테두리 입력 패널 (TextBox 래핑용).
/// Focus 시 Primary 테두리로 변경.
/// </summary>
public class RoundedInputPanel : Panel
{
    private readonly int _radius;
    private bool _isFocused;

    public bool IsFocused
    {
        get => _isFocused;
        set { _isFocused = value; Invalidate(); }
    }

    public RoundedInputPanel(int radius = 6)
    {
        _radius = radius;
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedPath(rect, _radius);

        // 배경
        using var bg = new SolidBrush(ColorPalette.Surface);
        g.FillPath(bg, path);

        // 테두리: Focus 시 Primary 2px, 일반 시 Border 1px
        if (_isFocused)
        {
            using var pen = new Pen(ColorPalette.Primary, 2f);
            g.DrawPath(pen, path);
        }
        else
        {
            using var pen = new Pen(ColorPalette.Border, 1f);
            g.DrawPath(pen, path);
        }
    }

    private static GraphicsPath RoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
