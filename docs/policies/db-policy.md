# DB 정책

## 1. 기본 설정

### 1.1 DB 파일

```
위치: %APPDATA%/CubeManager/cubemanager.db
백업: %APPDATA%/CubeManager/backups/cubemanager_YYYYMMDD.db
아카이브: %APPDATA%/CubeManager/archive/cubemanager_archive_YYYY.db
```

### 1.2 PRAGMA (앱 시작 시 1회 실행)

```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA temp_store = MEMORY;
PRAGMA mmap_size = 67108864;    -- 64MB
PRAGMA cache_size = -8000;      -- 8MB
PRAGMA page_size = 4096;
PRAGMA foreign_keys = ON;
```

### 1.3 앱 종료 시

```sql
PRAGMA wal_checkpoint(TRUNCATE);  -- WAL 파일 정리
PRAGMA optimize;                   -- 쿼리 플래너 최적화
```

---

## 2. 데이터 타입 규칙

| 용도 | SQLite 타입 | C# 타입 | 형식 | 예시 |
|------|------------|---------|------|------|
| 날짜 | TEXT | DateTime | `YYYY-MM-DD` | `2026-03-18` |
| 날짜+시간 | TEXT | DateTime | `YYYY-MM-DD HH:MM:SS` | `2026-03-18 14:30:00` |
| 시간 | TEXT | string | `HH:MM` (24시간제) | `14:30`, `01:00` |
| 금액 | INTEGER | int | 원 단위 (소수점 없음) | `10000` |
| 근무시간 | REAL | double | 시간 단위 (소수점 1자리) | `5.5` |
| Boolean | INTEGER | bool | 0 = false, 1 = true | `1` |
| 연월 | TEXT | string | `YYYY-MM` | `2026-03` |
| PK | INTEGER | int | 자동증가 | |
| FK | INTEGER | int | | |

---

## 3. 테이블 규칙

### 3.1 네이밍

- 테이블명: **복수형 snake_case** (`employees`, `sale_items`)
- 컬럼명: **snake_case** (`hourly_wage`, `created_at`)
- PK: `id` (모든 테이블 공통)
- FK: `{참조테이블_단수}_id` (`employee_id`, `handover_id`)
- 타임스탬프: `created_at`, `updated_at` (모든 테이블에 포함)

### 3.2 공통 컬럼 패턴

```sql
-- 모든 테이블에 포함
id INTEGER PRIMARY KEY AUTOINCREMENT,
created_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
updated_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
```

---

## 4. SQL 작성 규칙

### 4.1 키워드

```sql
-- ✅ 키워드 대문자, 테이블/컬럼 소문자
SELECT e.name, e.hourly_wage
FROM employees e
WHERE e.is_active = 1
ORDER BY e.name;

-- ❌ 금지
select Name from Employees where IsActive = 1;
```

### 4.2 파라미터 바인딩 (필수)

```csharp
// ✅ Dapper 파라미터 바인딩
conn.QueryAsync<Employee>(
    "SELECT * FROM employees WHERE name = @Name",
    new { Name = searchName });

// ❌ 절대 금지: 문자열 연결
conn.QueryAsync<Employee>(
    $"SELECT * FROM employees WHERE name = '{searchName}'");
```

### 4.3 SELECT * 금지

```csharp
// ✅ 필요한 컬럼만 명시
"SELECT id, name, hourly_wage, is_active FROM employees"

// ❌ 금지
"SELECT * FROM employees"
```

예외: 테스트 코드에서만 `SELECT *` 허용

### 4.4 트랜잭션

```csharp
// 다중 INSERT/UPDATE는 반드시 트랜잭션
using var conn = _db.CreateConnection();
using var transaction = conn.BeginTransaction();
try
{
    foreach (var schedule in schedules)
    {
        await conn.ExecuteAsync(sql, schedule, transaction);
    }
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

---

## 5. 마이그레이션

### 5.1 버전 관리 테이블

```sql
CREATE TABLE IF NOT EXISTS schema_version (
    version INTEGER PRIMARY KEY,
    description TEXT NOT NULL,
    applied_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
);
```

### 5.2 마이그레이션 파일 규칙

```
파일명: V{NNN}_{Description}.cs
예시:
  V001_InitBase.cs        → employees, app_config
  V002_Schedule.cs        → schedules, holidays
  V003_Reservation.cs     → reservations, daily_sales, sale_items, cash_balance
  V004_Salary.cs          → salary_records
  V005_Attendance.cs      → attendance
  V006_Handover.cs        → handovers, handover_comments
  V007_Inventory.cs       → inventory
```

- 번호는 절대 재사용하지 않는다
- 한번 적용된 마이그레이션은 수정하지 않는다
- 스키마 변경이 필요하면 새 번호로 ALTER 마이그레이션 추가

### 5.3 실행 순서

```
앱 시작
  → Database.cs: 연결 생성 + PRAGMA 설정
  → MigrationRunner.cs: schema_version 확인
  → 미적용 마이그레이션 순차 실행
  → 완료
```

---

## 6. 인덱스 정책

```sql
-- 필수 인덱스 (해당 마이그레이션에서 함께 생성)
CREATE INDEX idx_schedules_date ON schedules(work_date);
CREATE UNIQUE INDEX idx_schedules_emp_date ON schedules(employee_id, work_date);
CREATE UNIQUE INDEX idx_attendance_emp_date ON attendance(employee_id, work_date);
CREATE INDEX idx_reservations_date ON reservations(reservation_date);
CREATE INDEX idx_sale_items_daily ON sale_items(daily_sales_id);
CREATE INDEX idx_handovers_created ON handovers(created_at DESC);
CREATE INDEX idx_holidays_date ON holidays(holiday_date);
CREATE UNIQUE INDEX idx_salary_emp_month ON salary_records(employee_id, year_month);
```

- 소량 테이블(inventory, app_config)은 인덱스 금지
- 과도한 인덱스는 쓰기 성능 저하 → 필요 최소만

---

## 7. 백업 정책

| 항목 | 규칙 |
|------|------|
| 자동 백업 | 앱 종료 시 실행 |
| 보관 | 최근 3일치 |
| 방식 | SQLite Online Backup API |
| 수동 백업 | 설정 탭에서 [데이터 백업] 버튼 |
| 복원 | 설정 탭에서 [데이터 복원] → 파일 선택 → 앱 재시작 |

---

## 8. 데이터 보관 기간

| 데이터 | 보관 | 이후 |
|--------|------|------|
| 예약 | 6개월 | 아카이브 DB |
| 매출 | 1년 | 아카이브 DB |
| 스케줄 | 6개월 | 삭제 |
| 출퇴근 | 1년 | 아카이브 DB |
| 급여 | 3년 | 영구 보관 |
| 인수인계 | 1년 | 삭제 |
| 물품 | 현재만 | - |
| 공휴일 | 2년 | 갱신 |
