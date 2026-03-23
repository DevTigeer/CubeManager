using System.Drawing;
using CubeManager.Helpers;

namespace CubeManager.Dialogs;

/// <summary>
/// 손님 요금 계산기.
/// 성인 요금표 + 아동(초2이하) + 할인정책(계좌/군인/생일/재방문) 적용.
/// </summary>
public class CustomerCalcDialog : Form
{
    // 성인 요금표 (인원 → 금액)
    private static readonly Dictionary<int, int> PriceTable = new()
    {
        [2] = 36_000, [3] = 51_000, [4] = 64_000,
        [5] = 75_000, [6] = 84_000, [7] = 91_000
    };

    private const int ChildCashPrice = 10_000;
    private const int ChildCardPrice = 11_000;
    private const int AccountDiscount = 1_000;  // 인당
    private const int MilitaryDiscount = 2_000; // 인당
    private const int BirthdayDiscount = 2_000; // 인당
    private const int RevisitPerTheme = 1_000;  // 테마당

    private readonly NumericUpDown _numAdults;
    private readonly NumericUpDown _numChildren;
    private readonly CheckBox _chkAccount;
    private readonly CheckBox _chkCardChild;
    private readonly Panel _discountPanel;
    private readonly Label _lblResult;
    private readonly Label _lblBreakdown;

    // 인별 할인 ComboBox 목록
    private readonly List<ComboBox> _personDiscounts = [];
    private readonly List<NumericUpDown> _revisitThemes = [];

    public CustomerCalcDialog()
    {
        Text = "손님 계산";
        Size = new Size(420, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 10f);
        BackColor = ColorPalette.Surface;
        AutoScroll = true;

        var y = 12;

        // ─── 성인 인원 ───
        Controls.Add(MakeLabel("성인 인원:", 15, y));
        _numAdults = new NumericUpDown
        {
            Location = new Point(110, y - 2), Size = new Size(60, 28),
            Minimum = 2, Maximum = 7, Value = 2
        };
        _numAdults.ValueChanged += (_, _) => { RebuildDiscountRows(); Recalculate(); };
        Controls.Add(_numAdults);

        Controls.Add(MakeLabel("아동(초2이하):", 190, y));
        _numChildren = new NumericUpDown
        {
            Location = new Point(310, y - 2), Size = new Size(60, 28),
            Minimum = 0, Maximum = 5, Value = 0
        };
        _numChildren.ValueChanged += (_, _) => Recalculate();
        Controls.Add(_numChildren);

        y += 38;

        // ─── 결제수단 ───
        _chkAccount = new CheckBox
        {
            Text = "계좌/현금 결제 (성인 인당 1,000원 할인)",
            Location = new Point(15, y), Size = new Size(350, 24),
            Font = new Font("맑은 고딕", 9.5f)
        };
        _chkAccount.CheckedChanged += (_, _) => Recalculate();
        Controls.Add(_chkAccount);

        y += 28;

        _chkCardChild = new CheckBox
        {
            Text = "아동 카드결제 (11,000원, 미체크 시 10,000원)",
            Location = new Point(15, y), Size = new Size(350, 24),
            Font = new Font("맑은 고딕", 9.5f)
        };
        _chkCardChild.CheckedChanged += (_, _) => Recalculate();
        Controls.Add(_chkCardChild);

        y += 34;

        // ─── 인별 할인 (동적) ───
        Controls.Add(new Label
        {
            Text = "── 인별 할인 (택1: 군인/생일/재방문) ──",
            Location = new Point(15, y), Size = new Size(370, 22),
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary
        });
        y += 26;

        _discountPanel = new Panel
        {
            Location = new Point(15, y), Size = new Size(370, 120),
            AutoScroll = true
        };
        Controls.Add(_discountPanel);

        y += 130;

        // ─── 계산 결과 ───
        Controls.Add(new Label
        {
            Text = "── 계산 결과 ──",
            Location = new Point(15, y), Size = new Size(370, 22),
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = ColorPalette.TextSecondary
        });
        y += 26;

        _lblBreakdown = new Label
        {
            Location = new Point(15, y), Size = new Size(370, 100),
            Font = new Font("맑은 고딕", 10f),
            ForeColor = ColorPalette.Text
        };
        Controls.Add(_lblBreakdown);

        y += 105;

        _lblResult = new Label
        {
            Location = new Point(15, y), Size = new Size(370, 35),
            Font = new Font("맑은 고딕", 18f, FontStyle.Bold),
            ForeColor = ColorPalette.Primary,
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(_lblResult);

        y += 42;

        // ─── 버튼 ───
        var btnCopy = ButtonFactory.CreatePrimary("복사", 80);
        btnCopy.Location = new Point(220, y);
        btnCopy.Click += (_, _) =>
        {
            var text = _lblResult.Text.Replace(",", "").Replace("원", "").Trim();
            Clipboard.SetText(text);
            ToastNotification.Show("금액이 복사되었습니다.", ToastType.Success);
        };

        var btnClose = ButtonFactory.CreateGhost("닫기", 80);
        btnClose.Location = new Point(310, y);
        btnClose.Click += (_, _) => Close();

        Controls.AddRange([btnCopy, btnClose]);
        CancelButton = btnClose;

        RebuildDiscountRows();
        Recalculate();
    }

    /// <summary>성인 인원 수에 맞게 할인 ComboBox 행 재구성</summary>
    private void RebuildDiscountRows()
    {
        _discountPanel.Controls.Clear();
        _personDiscounts.Clear();
        _revisitThemes.Clear();

        var count = (int)_numAdults.Value;
        var py = 0;

        for (var i = 0; i < count; i++)
        {
            var idx = i;
            var lbl = new Label
            {
                Text = $"{i + 1}번 손님:",
                Location = new Point(0, py + 2), Size = new Size(70, 22),
                Font = new Font("맑은 고딕", 9f)
            };

            var cmb = new ComboBox
            {
                Location = new Point(75, py), Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "없음", "군인", "생일", "재방문" },
                Font = new Font("맑은 고딕", 9f)
            };
            cmb.SelectedIndex = 0;

            var numThemes = new NumericUpDown
            {
                Location = new Point(185, py), Size = new Size(55, 25),
                Minimum = 1, Maximum = 10, Value = 1,
                Visible = false,
                Font = new Font("맑은 고딕", 9f)
            };

            var lblTheme = new Label
            {
                Text = "테마",
                Location = new Point(245, py + 2), Size = new Size(40, 22),
                Font = new Font("맑은 고딕", 9f),
                ForeColor = ColorPalette.TextTertiary,
                Visible = false
            };

            cmb.SelectedIndexChanged += (_, _) =>
            {
                var isRevisit = cmb.SelectedIndex == 3;
                numThemes.Visible = isRevisit;
                lblTheme.Visible = isRevisit;
                Recalculate();
            };
            numThemes.ValueChanged += (_, _) => Recalculate();

            _discountPanel.Controls.AddRange([lbl, cmb, numThemes, lblTheme]);
            _personDiscounts.Add(cmb);
            _revisitThemes.Add(numThemes);

            py += 30;
        }

        _discountPanel.Height = Math.Max(py, 30);
    }

    /// <summary>실시간 요금 재계산</summary>
    private void Recalculate()
    {
        var adults = (int)_numAdults.Value;
        var children = (int)_numChildren.Value;

        // 1. 성인 기본가
        if (!PriceTable.TryGetValue(adults, out var basePrice))
            basePrice = adults <= 1 ? 0 : PriceTable.OrderByDescending(kv => kv.Key).First().Value;

        // 2. 아동가
        var childUnitPrice = _chkCardChild.Checked ? ChildCardPrice : ChildCashPrice;
        var childTotal = children * childUnitPrice;

        // 3. 계좌/현금 할인
        var accountDsc = _chkAccount.Checked ? adults * AccountDiscount : 0;

        // 4. 인별 할인 합산
        var militaryCount = 0;
        var birthdayCount = 0;
        var revisitThemeTotal = 0;

        for (var i = 0; i < _personDiscounts.Count; i++)
        {
            switch (_personDiscounts[i].SelectedIndex)
            {
                case 1: militaryCount++; break;
                case 2: birthdayCount++; break;
                case 3: revisitThemeTotal += (int)_revisitThemes[i].Value; break;
            }
        }

        var militaryDsc = militaryCount * MilitaryDiscount;
        var birthdayDsc = birthdayCount * BirthdayDiscount;
        var revisitDsc = revisitThemeTotal * RevisitPerTheme;

        var totalDiscount = accountDsc + militaryDsc + birthdayDsc + revisitDsc;
        var finalPrice = basePrice + childTotal - totalDiscount;
        if (finalPrice < 0) finalPrice = 0;

        // 명세 텍스트
        var lines = new List<string>();
        lines.Add($"성인 기본가: {basePrice:N0}원 ({adults}인)");
        if (children > 0)
            lines.Add($"아동가: +{childTotal:N0}원 ({children}명 × {childUnitPrice:N0})");
        if (accountDsc > 0)
            lines.Add($"계좌/현금 할인: -{accountDsc:N0}원 ({adults}명 × {AccountDiscount:N0})");
        if (militaryDsc > 0)
            lines.Add($"군인 할인: -{militaryDsc:N0}원 ({militaryCount}명)");
        if (birthdayDsc > 0)
            lines.Add($"생일 할인: -{birthdayDsc:N0}원 ({birthdayCount}명)");
        if (revisitDsc > 0)
            lines.Add($"재방문 할인: -{revisitDsc:N0}원 ({revisitThemeTotal}테마)");

        _lblBreakdown.Text = string.Join("\n", lines);
        _lblResult.Text = $"{finalPrice:N0}원";
    }

    private static Label MakeLabel(string text, int x, int y) => new()
    {
        Text = text, Location = new Point(x, y + 2),
        Size = new Size(80, 22), Font = new Font("맑은 고딕", 10f)
    };
}
