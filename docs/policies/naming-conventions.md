# 네이밍 규칙

## 1. 프로젝트/솔루션

```
솔루션:  CubeManager.sln
프로젝트: CubeManager          (WinForms 앱)
         CubeManager.Core      (비즈니스 로직)
         CubeManager.Data      (데이터 접근)
         CubeManager.Tests     (테스트)
```

---

## 2. 네임스페이스

```csharp
// 프로젝트 → 폴더 경로 매핑
CubeManager.Core.Models              // 데이터 모델
CubeManager.Core.Services            // 서비스 구현
CubeManager.Core.Interfaces.Repositories  // Repo 인터페이스
CubeManager.Core.Interfaces.Services      // Service 인터페이스
CubeManager.Data                     // DB 연결
CubeManager.Data.Repositories        // Repo 구현
CubeManager.Data.Migrations          // 마이그레이션
CubeManager.Forms                    // 탭 UserControl
CubeManager.Controls                 // 커스텀 컨트롤
CubeManager.Dialogs                  // 팝업 다이얼로그
CubeManager.Helpers                  // UI 유틸리티
```

---

## 3. 클래스

| 종류 | 패턴 | 예시 |
|------|------|------|
| Model | `{Entity}` | `Employee`, `Schedule`, `SalaryRecord` |
| Repository Interface | `I{Entity}Repository` | `IEmployeeRepository` |
| Repository 구현 | `{Entity}Repository` | `EmployeeRepository` |
| Service Interface | `I{Domain}Service` | `ISalaryService`, `IScheduleService` |
| Service 구현 | `{Domain}Service` | `SalaryService` |
| 탭 UserControl | `{Feature}Tab` | `ScheduleTab`, `SalaryTab` |
| 다이얼로그 | `{Purpose}Dialog` | `AdminAuthDialog` |
| 커스텀 컨트롤 | `{Name}Panel` / `{Name}Viewer` | `TimeTablePanel` |
| 마이그레이션 | `V{NNN}_{Description}` | `V001_InitBase` |
| Enum | `{Name}` (단수) | `PaymentType`, `ClockStatus` |

---

## 4. DB 네이밍

| 대상 | 규칙 | 예시 |
|------|------|------|
| 테이블 | **복수형 snake_case** | `employees`, `sale_items`, `salary_records` |
| 컬럼 | **snake_case** | `hourly_wage`, `created_at`, `is_active` |
| PK | `id` | 모든 테이블 공통 |
| FK | `{테이블단수}_id` | `employee_id`, `daily_sales_id` |
| 인덱스 | `idx_{테이블}_{컬럼}` | `idx_schedules_date` |
| 유니크 인덱스 | `idx_{테이블}_{컬럼}` | `idx_schedules_emp_date` |

### C# 프로퍼티 ↔ DB 컬럼 매핑

```
DB: hourly_wage   →  C#: HourlyWage    (Dapper 자동 매핑: snake_case → PascalCase)
DB: created_at    →  C#: CreatedAt
DB: is_active     →  C#: IsActive
```

Dapper에서 `DefaultTypeMap.MatchNamesWithUnderscores = true;` 설정으로 자동 매핑.

---

## 5. 메서드

| 작업 | 접두사 | 예시 |
|------|--------|------|
| 조회 (단건) | `GetBy{Key}` | `GetByIdAsync(id)` |
| 조회 (목록) | `Get{조건}` | `GetActiveAsync()`, `GetByDateAsync(date)` |
| 조회 (전체) | `GetAll` | `GetAllAsync()` |
| 생성 | `Insert` / `Add` | `InsertAsync(entity)`, `AddEmployeeAsync(...)` |
| 수정 | `Update` | `UpdateAsync(entity)` |
| 삭제 | `Delete` / `Deactivate` | `DeleteAsync(id)`, `DeactivateAsync(id)` |
| 계산 | `Calculate` | `CalculateMonthlySalaryAsync(...)` |
| 판정 | `Judge` / `Is` | `JudgeClockInStatus(...)`, `IsWeekdayHoliday(date)` |
| 동기화 | `Sync` / `Fetch` | `SyncReservationsAsync(date)`, `FetchReservationsAsync(date)` |
| 일괄 처리 | `Bulk{Action}` | `BulkInsertAsync(items)` |

### async 규칙

```
Repository 메서드: 항상 Async 접미사 + Task<T> 반환
Service 메서드:    항상 Async 접미사 + Task<T> 반환
UI 이벤트 핸들러: async void (이벤트 핸들러에서만 예외)
```

---

## 6. 변수

```csharp
// private 필드: _camelCase
private readonly IEmployeeRepository _employeeRepo;
private readonly Database _db;
private bool _isEditMode;

// 로컬 변수: camelCase
var employeeList = await _employeeRepo.GetActiveAsync();
var totalHours = 0.0;
var startDate = new DateTime(2026, 3, 1);

// 상수: PascalCase
public const int DefaultPageSize = 10;
public const int AdminAuthCacheMinutes = 5;
public const int TaxiAllowanceDefault = 10000;
```

---

## 7. Enum

```csharp
// 단수형 PascalCase
public enum PaymentType
{
    Card,       // 카드
    Cash,       // 현금
    Transfer    // 계좌이체
}

public enum ClockStatus
{
    OnTime,     // 정상
    Late,       // 지각
    Early       // 조퇴
}

public enum SaleCategory
{
    Revenue,    // 매출
    Expense     // 지출
}
```

---

## 8. 파일/폴더

```
프로젝트 내 폴더: PascalCase (Forms/, Controls/, Models/)
문서 파일: kebab-case (coding-conventions.md, db-policy.md)
데이터 폴더: 소문자 (data/documents/, data/backups/)
설정 파일: 소문자 (appsettings.json)
```
