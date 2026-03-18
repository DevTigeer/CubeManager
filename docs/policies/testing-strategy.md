# 테스트 전략

## 1. 프레임워크

| 용도 | 라이브러리 | NuGet |
|------|-----------|-------|
| 테스트 프레임워크 | **xUnit** | xunit, xunit.runner.visualstudio |
| Assertion | **FluentAssertions** | FluentAssertions |
| Mocking | **NSubstitute** | NSubstitute |
| DB 테스트 | **In-Memory SQLite** | Microsoft.Data.Sqlite |

```xml
<!-- CubeManager.Tests.csproj -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
<PackageReference Include="FluentAssertions" Version="7.*" />
<PackageReference Include="NSubstitute" Version="5.*" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="8.*" />
<PackageReference Include="Dapper" Version="2.*" />
```

---

## 2. 테스트 구조

```
CubeManager.Tests/
├── Unit/
│   ├── Services/
│   │   ├── EmployeeServiceTests.cs
│   │   ├── ScheduleServiceTests.cs
│   │   ├── SalaryServiceTests.cs
│   │   ├── AttendanceServiceTests.cs
│   │   └── SalesServiceTests.cs
│   └── Helpers/
│       └── WeekCalculatorTests.cs
├── Integration/
│   ├── Repositories/
│   │   ├── EmployeeRepositoryTests.cs
│   │   ├── ScheduleRepositoryTests.cs
│   │   └── ...
│   └── Flows/
│       ├── ScheduleToSalaryFlowTests.cs
│       └── AttendanceFlowTests.cs
├── Fixtures/
│   ├── InMemoryDatabaseFixture.cs
│   └── TestDataBuilder.cs
└── _usings.cs
```

---

## 3. 단위 테스트 (Service)

Repository를 Mock하여 비즈니스 로직만 검증한다.

```csharp
// Unit/Services/SalaryServiceTests.cs
public class SalaryServiceTests
{
    private readonly ISalaryService _sut;
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IHolidayRepository _holidayRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IConfigRepository _configRepo;

    public SalaryServiceTests()
    {
        _scheduleRepo = Substitute.For<IScheduleRepository>();
        _holidayRepo = Substitute.For<IHolidayRepository>();
        _employeeRepo = Substitute.For<IEmployeeRepository>();
        _configRepo = Substitute.For<IConfigRepository>();

        _sut = new SalaryService(
            _scheduleRepo, _holidayRepo,
            _employeeRepo, _configRepo);
    }

    [Fact]
    public async Task CalculateSalary_BasicCase_ReturnsCorrectAmount()
    {
        // Arrange
        _employeeRepo.GetByIdAsync(1).Returns(new Employee
            { Id = 1, HourlyWage = 10000 });
        _scheduleRepo.GetByEmployeeAndMonthAsync(1, "2026-03")
            .Returns(CreateSchedules(totalHours: 165.0));
        _holidayRepo.GetWeekdayHolidayHoursAsync(1, "2026-03")
            .Returns(8.0);
        _configRepo.GetIntAsync("holiday_bonus_per_hour", 3000)
            .Returns(3000);

        // Act
        var result = await _sut.CalculateMonthlySalaryAsync(1, "2026-03");

        // Assert
        result.BaseSalary.Should().Be(1_650_000);    // 165h × 10,000
        result.HolidayBonus.Should().Be(24_000);      // 8h × 3,000
    }

    [Theory]
    [InlineData(1_754_000, 57_882)]   // 소수점 이하 버림
    [InlineData(1_000_000, 33_000)]   // 정확한 경우
    [InlineData(1_000_001, 33_000)]   // 소수점 버림
    public void ApplyTax33_VariousAmounts_TruncatesCorrectly(
        int gross, int expectedTax)
    {
        var result = SalaryService.CalculateTax33(gross);
        result.Should().Be(expectedTax);
    }
}
```

---

## 4. 통합 테스트 (Repository)

In-Memory SQLite로 실제 DB 동작을 검증한다.

```csharp
// Fixtures/InMemoryDatabaseFixture.cs
public class InMemoryDatabaseFixture : IDisposable
{
    public SqliteConnection Connection { get; }
    public Database Db { get; }

    public InMemoryDatabaseFixture()
    {
        // In-Memory SQLite: 연결 유지하는 동안 DB 존재
        Connection = new SqliteConnection("Data Source=:memory:");
        Connection.Open();

        // PRAGMA 설정
        Connection.Execute("PRAGMA foreign_keys = ON");

        // 마이그레이션 실행 (모든 테이블 생성)
        var runner = new MigrationRunner(/* in-memory db */);
        runner.RunAll(Connection);
    }

    public void Dispose() => Connection.Dispose();
}

// Integration/Repositories/EmployeeRepositoryTests.cs
public class EmployeeRepositoryTests : IClassFixture<InMemoryDatabaseFixture>
{
    private readonly EmployeeRepository _repo;

    public EmployeeRepositoryTests(InMemoryDatabaseFixture fixture)
    {
        _repo = new EmployeeRepository(fixture.Db);
    }

    [Fact]
    public async Task Insert_ThenGetById_ReturnsSameEmployee()
    {
        var id = await _repo.InsertAsync(new Employee
            { Name = "홍길동", HourlyWage = 10000 });

        var result = await _repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("홍길동");
        result.HourlyWage.Should().Be(10000);
    }
}
```

---

## 5. 통합 플로우 테스트

```csharp
// Integration/Flows/ScheduleToSalaryFlowTests.cs
// 직원 추가 → 스케줄 등록 → 급여 계산 전체 흐름
[Fact]
public async Task FullFlow_EmployeeToSalary_CalculatesCorrectly()
{
    // 1. 직원 추가
    var empId = await _employeeRepo.InsertAsync(
        new Employee { Name = "테스트", HourlyWage = 10000 });

    // 2. 스케줄 등록 (월~금, 14:00~19:30, 3월)
    await _scheduleService.AddScheduleAsync(
        empId, "14:00", "19:30",
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
         DayOfWeek.Thursday, DayOfWeek.Friday],
        2026, 3);

    // 3. 급여 계산
    var salary = await _salaryService.CalculateMonthlySalaryAsync(empId, "2026-03");

    // 4. 검증
    salary.TotalHours.Should().BeApproximately(5.5 * 22, 0.1); // 22 평일
    salary.BaseSalary.Should().Be((int)(5.5 * 22 * 10000));
}
```

---

## 6. 테스트하지 않는 것

| 대상 | 이유 |
|------|------|
| UI (WinForms) | 자동화 테스트 ROI 낮음, 수동 검증 |
| GDI+ 렌더링 | 시각적 확인 필요, 자동화 어려움 |
| AngleSharp 웹 스크래핑 | 외부 사이트 의존, 별도 수동 검증 |
| AntdUI 컴포넌트 | 서드파티, 자체 테스트 영역 |

UI는 수동 테스트 체크리스트로 대체:
```
□ 각 탭 전환 정상
□ DataGridView 데이터 표시 정확
□ 타임테이블 블록 렌더링 정확
□ 출퇴근 버튼 색상 판정 정확
□ 토스트 알림 동작
□ 관리자 인증 다이얼로그 동작
```

---

## 7. 테스트 네이밍

```
{메서드명}_{시나리오}_{기대결과}

예시:
CalculateSalary_BasicCase_ReturnsCorrectAmount
ClockIn_BeforeScheduledTime_ReturnsOnTime
ClockIn_AfterScheduledTime_ReturnsLate
GetWeekNumber_March1OnSaturday_ReturnsWeek1
ApplyTax33_RoundingCase_TruncatesDown
```
