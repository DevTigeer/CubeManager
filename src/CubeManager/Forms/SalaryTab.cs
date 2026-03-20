using System.Drawing;
using CubeManager.Controls;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class SalaryTab : UserControl
{
    private readonly ISalaryService _salaryService;
    private readonly DataGridView _grid;
    private readonly Label _lblMonth;
    private int _year, _month;

    public SalaryTab(ISalaryService salaryService)
    {
        _salaryService = salaryService;
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Padding = new Padding(10);

        _year = DateTime.Today.Year;
        _month = DateTime.Today.Month;

        // Top bar
        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 50,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 5)
        };

        topBar.Controls.Add(new Label
        {
            Text = "급여 관리", Size = new Size(100, 32),
            Font = new Font("맑은 고딕", 14f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });

        var btnPrev = new Button { Text = "◀", Size = new Size(40, 32), FlatStyle = FlatStyle.Flat };
        btnPrev.Click += (_, _) => Navigate(-1);

        _lblMonth = new Label
        {
            Size = new Size(140, 32),
            Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var btnNext = new Button { Text = "▶", Size = new Size(40, 32), FlatStyle = FlatStyle.Flat };
        btnNext.Click += (_, _) => Navigate(1);

        var btnCalc = new Button
        {
            Text = "재계산", Size = new Size(80, 32),
            BackColor = ColorPalette.Primary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Margin = new Padding(20, 0, 0, 0)
        };
        btnCalc.FlatAppearance.BorderSize = 0;
        btnCalc.Click += BtnCalc_Click;

        topBar.Controls.AddRange([btnPrev, _lblMonth, btnNext, btnCalc]);

        // Grid
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false, ReadOnly = true,
            RowHeadersVisible = false, BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle, GridColor = ColorPalette.Border,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("맑은 고딕", 9f),
                Alignment = DataGridViewContentAlignment.MiddleRight
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
                BackColor = ColorPalette.Background,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            }
        };

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "이름", FillWeight = 12, DefaultCellStyle = new() { Alignment = DataGridViewContentAlignment.MiddleLeft } },
            new DataGridViewTextBoxColumn { HeaderText = "시급", FillWeight = 8 },
            new DataGridViewTextBoxColumn { HeaderText = "1주", FillWeight = 6 },
            new DataGridViewTextBoxColumn { HeaderText = "2주", FillWeight = 6 },
            new DataGridViewTextBoxColumn { HeaderText = "3주", FillWeight = 6 },
            new DataGridViewTextBoxColumn { HeaderText = "4주", FillWeight = 6 },
            new DataGridViewTextBoxColumn { HeaderText = "5주+", FillWeight = 6 },
            new DataGridViewTextBoxColumn { HeaderText = "합계", FillWeight = 7 },
            new DataGridViewTextBoxColumn { HeaderText = "공휴일", FillWeight = 8 },
            new DataGridViewTextBoxColumn { HeaderText = "총급여", FillWeight = 10 },
            new DataGridViewTextBoxColumn { HeaderText = "3.3%\n(수령액)", FillWeight = 10 },
            new DataGridViewTextBoxColumn { HeaderText = "", FillWeight = 2, ReadOnly = true }, // 구분선
            new DataGridViewTextBoxColumn { HeaderText = "식비", FillWeight = 8, DefaultCellStyle = new() { ForeColor = Color.FromArgb(120, 120, 120), Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { HeaderText = "택시", FillWeight = 8, DefaultCellStyle = new() { ForeColor = Color.FromArgb(120, 120, 120), Alignment = DataGridViewContentAlignment.MiddleRight } });

        GridTheme.ApplyTheme(_grid);

        // Summary Cards
        var cards = new SummaryCardRow();
        cards.AddCard("총 급여", "₩0", ColorPalette.AccentBlue.Main, ColorPalette.AccentBlue.Light);
        cards.AddCard("대상 인원", "0명", ColorPalette.AccentGreen.Main, ColorPalette.AccentGreen.Light);
        cards.AddCard("공휴일수당", "₩0", ColorPalette.AccentOrange.Main, ColorPalette.AccentOrange.Light);
        cards.AddCard("식비+택시", "₩0", ColorPalette.AccentRed.Main, ColorPalette.AccentRed.Light);

        Controls.Add(_grid);
        Controls.Add(cards);
        Controls.Add(topBar);

        _ = LoadAsync();
    }

    private void Navigate(int dir)
    {
        _month += dir;
        if (_month > 12) { _month = 1; _year++; }
        else if (_month < 1) { _month = 12; _year--; }
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _lblMonth.Text = $"{_year}년 {_month:D2}월";
        try
        {
            var ym = $"{_year:D4}-{_month:D2}";
            var records = (await _salaryService.GetMonthlySalaryTableAsync(ym)).ToList();
            _grid.Rows.Clear();

            foreach (var r in records)
            {
                // 컬럼: 이름|시급|1주|2주|3주|4주|5주+|합계|공휴일|총급여|3.3%(수령액)|구분|식비|택시
                var idx = _grid.Rows.Add(
                    r.EmployeeName, r.HourlyWage.ToString("N0"),
                    r.Week1Hours.ToString("F1"), r.Week2Hours.ToString("F1"),
                    r.Week3Hours.ToString("F1"), r.Week4Hours.ToString("F1"),
                    r.Week5Hours.ToString("F1"), r.TotalHours.ToString("F1"),
                    r.HolidayBonus.ToString("N0"), r.GrossSalary.ToString("N0"),
                    r.NetSalary.ToString("N0"),  // 3.3% 수령액
                    "",                           // 구분선
                    r.MealAllowance.ToString("N0"), r.TaxiAllowance.ToString("N0"));

                if (r.IsManualEdit)
                {
                    foreach (DataGridViewCell cell in _grid.Rows[idx].Cells)
                        cell.Style.BackColor = ColorPalette.ManualEdit;
                }
            }

            if (records.Count == 0)
                ToastNotification.Show("급여 데이터가 없습니다. '재계산' 버튼을 눌러주세요.", ToastType.Warning);
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }

    private async void BtnCalc_Click(object? sender, EventArgs e)
    {
        try
        {
            var ym = $"{_year:D4}-{_month:D2}";
            await _salaryService.CalculateAllAsync(ym);
            ToastNotification.Show($"{_year}년 {_month}월 급여 계산 완료.", ToastType.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ToastNotification.Show(ex.Message, ToastType.Error);
        }
    }
}
