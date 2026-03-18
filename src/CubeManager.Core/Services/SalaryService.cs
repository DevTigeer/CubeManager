using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;

namespace CubeManager.Core.Services;

public class SalaryService : ISalaryService
{
    private readonly ISalaryRepository _salaryRepo;
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IHolidayRepository _holidayRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IConfigRepository _configRepo;

    public SalaryService(ISalaryRepository salaryRepo, IScheduleRepository scheduleRepo,
        IHolidayRepository holidayRepo, IEmployeeRepository employeeRepo, IConfigRepository configRepo)
    {
        _salaryRepo = salaryRepo;
        _scheduleRepo = scheduleRepo;
        _holidayRepo = holidayRepo;
        _employeeRepo = employeeRepo;
        _configRepo = configRepo;
    }

    public Task<IEnumerable<SalaryRecord>> GetMonthlySalaryTableAsync(string yearMonth) =>
        _salaryRepo.GetByMonthAsync(yearMonth);

    public async Task CalculateAllAsync(string yearMonth)
    {
        var parts = yearMonth.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);

        var employees = await _employeeRepo.GetActiveAsync();
        var mealUnit = await _configRepo.GetIntAsync("default_meal_allowance", 5000);
        var taxiUnit = await _configRepo.GetIntAsync("taxi_allowance", 10000);
        var mealMinH = await _configRepo.GetIntAsync("meal_min_hours", 6);
        var holidayBonus = await _configRepo.GetIntAsync("holiday_bonus_per_hour", 3000);
        var taxiCutoff = (await _configRepo.GetAsync("taxi_cutoff_time")) ?? "23:30";
        var taxiCutoffMin = TimeHelper.ToMinutes(taxiCutoff);

        var totalWeeks = TimeHelper.GetTotalWeeks(year, month);

        foreach (var emp in employees)
        {
            // 수기 수정된 레코드는 건너뛰기
            var existing = await _salaryRepo.GetByEmployeeMonthAsync(emp.Id, yearMonth);
            if (existing is { IsManualEdit: true }) continue;

            var schedules = (await _scheduleRepo.GetByEmployeeAndMonthAsync(emp.Id, yearMonth)).ToList();

            // 주차별 시간
            var weekHours = new double[5];
            double totalHolidayHours = 0;
            int mealDays = 0, taxiDays = 0;

            foreach (var s in schedules)
            {
                var date = DateTime.Parse(s.WorkDate);
                var weekIdx = Math.Min(TimeHelper.GetWeekOfMonth(date) - 1, 4); // 5주차 이후는 4에 합산
                var hours = TimeHelper.CalcHours(s.StartTime, s.EndTime);
                weekHours[weekIdx] += hours;

                // 공휴일 (평일만)
                if (await _holidayRepo.IsWeekdayHolidayAsync(s.WorkDate))
                    totalHolidayHours += hours;

                // 식비 (6h 이상)
                if (hours >= mealMinH) mealDays++;

                // 택시비 (퇴근 >= 23:30)
                if (TimeHelper.ToMinutes(s.EndTime) >= taxiCutoffMin) taxiDays++;
            }

            var totalHours = weekHours.Sum();
            var baseSalary = (int)(totalHours * emp.HourlyWage);
            var holidayBonusAmt = (int)(totalHolidayHours * holidayBonus);
            var mealAmt = mealDays * mealUnit;
            var taxiAmt = taxiDays * taxiUnit;
            var gross = baseSalary + holidayBonusAmt + mealAmt + taxiAmt;
            var tax = (int)(gross * 0.033); // 내림
            var net = gross - tax;

            var record = new SalaryRecord
            {
                EmployeeId = emp.Id,
                YearMonth = yearMonth,
                Week1Hours = weekHours[0],
                Week2Hours = weekHours[1],
                Week3Hours = weekHours[2],
                Week4Hours = weekHours[3],
                Week5Hours = weekHours[4],
                TotalHours = totalHours,
                HolidayHours = totalHolidayHours,
                HolidayBonus = holidayBonusAmt,
                BaseSalary = baseSalary,
                MealAllowance = mealAmt,
                TaxiAllowance = taxiAmt,
                GrossSalary = gross,
                Tax33 = tax,
                NetSalary = net,
                IsManualEdit = false
            };

            await _salaryRepo.UpsertAsync(record);
        }
    }
}
