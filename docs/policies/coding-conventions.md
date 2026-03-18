# 코딩 컨벤션

## 1. 언어 설정

```xml
<!-- .csproj 공통 설정 -->
<PropertyGroup>
  <TargetFramework>net8.0-windows</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <LangVersion>12</LangVersion>
</PropertyGroup>
```

- Nullable reference types 항상 활성화
- 경고 레벨: `<WarningLevel>7</WarningLevel>`
- 경고를 에러로: `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>` (단, nullable 경고는 반드시 해결)

---

## 2. 네이밍 규칙

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스/인터페이스 | PascalCase | `EmployeeService`, `IScheduleRepository` |
| 공개 메서드/프로퍼티 | PascalCase | `GetActiveEmployees()`, `HourlyWage` |
| private 필드 | _camelCase | `_employeeRepo`, `_dbConnection` |
| 로컬 변수/파라미터 | camelCase | `employeeId`, `startTime` |
| 상수 | PascalCase | `MaxRetryCount`, `DefaultPageSize` |
| Enum | PascalCase | `PaymentType.Card`, `ClockStatus.Late` |
| 인터페이스 접두사 | I | `IEmployeeRepository`, `ISalaryService` |
| async 메서드 접미사 | Async | `FetchReservationsAsync()`, `ClockInAsync()` |
| Boolean 프로퍼티 | Is/Has/Can | `IsActive`, `HasClockIn`, `CanEdit` |

### 파일 네이밍

| 종류 | 패턴 | 예시 |
|------|------|------|
| Model | `{Entity}.cs` | `Employee.cs`, `SalaryRecord.cs` |
| Interface | `I{Name}.cs` | `IEmployeeRepository.cs` |
| Repository | `{Entity}Repository.cs` | `EmployeeRepository.cs` |
| Service | `{Domain}Service.cs` | `SalaryService.cs` |
| UserControl (탭) | `{Feature}Tab.cs` | `ScheduleTab.cs` |
| Dialog | `{Purpose}Dialog.cs` | `AdminAuthDialog.cs` |
| Custom Control | `{Name}Panel.cs` / `{Name}Viewer.cs` | `TimeTablePanel.cs` |
| Migration | `V{NNN}_{Description}.cs` | `V001_InitBase.cs` |

---

## 3. 코드 구조

### 3.1 Model (POCO)

```csharp
// Core/Models/Employee.cs
namespace CubeManager.Core.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int HourlyWage { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

- POCO (Plain Old CLR Object) — 로직 없음
- nullable string은 `string?`, non-nullable은 `= string.Empty` 초기화
- 날짜는 `DateTime`, 금액은 `int` (원 단위)

### 3.2 Interface

```csharp
// Core/Interfaces/Repositories/IEmployeeRepository.cs
namespace CubeManager.Core.Interfaces.Repositories;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<IEnumerable<Employee>> GetActiveAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<int> InsertAsync(Employee employee);
    Task<bool> UpdateAsync(Employee employee);
    Task<bool> DeactivateAsync(int id);
}
```

- 모든 DB 접근 메서드는 `async` + `Task` 반환
- 조회 실패 시 `null` 반환 (예외 아님)
- 삽입 시 생성된 ID 반환

### 3.3 Repository

```csharp
// Data/Repositories/EmployeeRepository.cs
namespace CubeManager.Data.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly Database _db;

    public EmployeeRepository(Database db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Employee>> GetActiveAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Employee>(
            "SELECT id, name, hourly_wage, is_active, phone, created_at, updated_at " +
            "FROM employees WHERE is_active = 1 ORDER BY name");
    }

    public async Task<int> InsertAsync(Employee employee)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO employees (name, hourly_wage, is_active, phone, created_at, updated_at) " +
            "VALUES (@Name, @HourlyWage, @IsActive, @Phone, @CreatedAt, @UpdatedAt); " +
            "SELECT last_insert_rowid()",
            employee);
    }
}
```

- `using var conn = _db.CreateConnection();` 패턴 통일
- SQL 키워드 대문자, 테이블/컬럼명 소문자 snake_case
- Dapper 파라미터 바인딩 (`@Name`) 필수, 문자열 연결 금지

### 3.4 Service

```csharp
// Core/Services/EmployeeService.cs
namespace CubeManager.Core.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepo;

    public EmployeeService(IEmployeeRepository employeeRepo)
    {
        _employeeRepo = employeeRepo;
    }

    public async Task<int> AddEmployeeAsync(string name, int hourlyWage, string? phone)
    {
        // 비즈니스 검증
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("이름은 필수입니다.", nameof(name));
        if (hourlyWage <= 0)
            throw new ArgumentException("시급은 양수여야 합니다.", nameof(hourlyWage));

        var employee = new Employee
        {
            Name = name.Trim(),
            HourlyWage = hourlyWage,
            Phone = phone,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        return await _employeeRepo.InsertAsync(employee);
    }
}
```

- 생성자 주입 (DI) — Repository 인터페이스만 주입
- 비즈니스 검증은 Service에서 수행
- 다른 Service를 직접 참조하지 않음

### 3.5 UI (이벤트 핸들러)

```csharp
// Forms/SettingsTab.cs
private async void BtnAdd_Click(object sender, EventArgs e)
{
    try
    {
        var id = await _employeeService.AddEmployeeAsync(
            txtName.Text, int.Parse(txtWage.Text), txtPhone.Text);

        ToastNotification.Show("직원이 추가되었습니다.", ToastType.Success);
        await LoadEmployeeListAsync();
    }
    catch (ArgumentException ex)
    {
        ToastNotification.Show(ex.Message, ToastType.Warning);
    }
    catch (Exception ex)
    {
        ToastNotification.Show("오류가 발생했습니다.", ToastType.Error);
        // 로깅
    }
}
```

- `async void`는 이벤트 핸들러에서만 허용
- try-catch는 UI 레이어에서만 (Service에서 예외를 던지고 UI에서 잡는다)
- 사용자에게 보여줄 메시지는 한국어

---

## 4. 비동기 규칙

```
✅ 올바른 사용:
  await _repo.GetAllAsync()
  await Task.Run(() => heavyComputation)

❌ 금지:
  _repo.GetAllAsync().Result     // 데드락 위험
  _repo.GetAllAsync().Wait()     // 데드락 위험
  Task.Run(async () => ...)      // 불필요한 래핑 지양
```

- I/O 바운드 작업 (DB, HTTP): `async/await` 사용
- CPU 바운드 작업 (급여 계산 등): `Task.Run`으로 백그라운드 스레드
- UI 업데이트: 반드시 UI 스레드에서 (await 후 자동으로 UI 스레드 복귀)

---

## 5. 주석 규칙

```csharp
// ✅ 좋은 주석: "왜"를 설명
// 평일 공휴일만 추가수당 적용 (주말 공휴일은 제외)
if (IsWeekdayHoliday(date))

// ❌ 나쁜 주석: "무엇"을 설명 (코드를 읽으면 알 수 있음)
// 직원 목록을 가져온다
var employees = await _repo.GetAllAsync();
```

- "왜"를 설명하는 주석만 작성
- 공개 API (Service 인터페이스)에는 XML 문서 주석 `///` 작성
- TODO 주석: `// TODO: [담당자] 설명` 형식

---

## 6. 에러 처리 전략

| 레이어 | 역할 |
|--------|------|
| Repository | 예외 발생 시 그대로 전파 (catch 안 함) |
| Service | 비즈니스 검증 예외 throw, 외부 연동(HTTP) 실패는 catch 후 기본값 반환 |
| UI | 모든 예외 catch, 사용자에게 토스트 알림 표시 |

```
Repository → 예외 발생
Service    → 비즈니스 예외는 throw, 외부 실패는 catch
UI         → 모든 예외 catch + 토스트 알림
```
