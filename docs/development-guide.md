# 개발 가이드 (Step-by-Step)

> 코드가 꼬이지 않도록 **레이어별 → 기능별** 순서로 엄격하게 진행한다.
> 각 Step은 이전 Step이 완료된 후에만 시작한다.

---

## 핵심 원칙

### 1. 레이어 순서를 지킨다

```
어떤 기능이든 반드시 이 순서로 만든다:

  ① Model (데이터 클래스)
  ② DB 테이블 + Repository (데이터 접근)
  ③ Service (비즈니스 로직)
  ④ UI (화면)

절대 UI부터 만들지 않는다.
절대 Service에서 DB를 직접 접근하지 않는다.
```

### 2. 테이블은 필요할 때 만든다

```
❌ 나쁜 방법: Phase 0에서 13개 테이블 전부 생성
  → 나중에 스키마 바꾸면 마이그레이션 복잡

✅ 좋은 방법: 해당 기능 개발 시 필요한 테이블만 생성
  → Step 2에서 employees + app_config
  → Step 4에서 schedules + holidays
  → Step 6에서 reservations, daily_sales, sale_items, cash_balance
  → ...
```

### 3. 인터페이스로 레이어를 분리한다

```
IEmployeeRepository ← EmployeeRepository (SQLite 구현)
IScheduleService    ← ScheduleService (비즈니스 로직)

→ Service는 Repository 인터페이스에만 의존
→ UI는 Service 인터페이스에만 의존
→ 나중에 어떤 레이어든 교체 가능
```

### 4. 한 Step에서 한 기능만 완성한다

```
❌ "스케줄 Service 전부 + 급여 Service 일부" 동시 진행
✅ "스케줄 Model → Repo → Service → UI" 순서대로 완성 후 다음 기능
```

---

## 프로젝트 구조 (솔루션)

```
CubeManager.sln
│
├── CubeManager/                    # WinForms 앱 프로젝트 (UI + 진입점)
│   ├── Program.cs                  # 앱 진입점
│   ├── MainForm.cs                 # 메인 폼 (탭 컨테이너)
│   ├── Forms/                      # 각 탭의 UserControl
│   │   ├── ReservationSalesTab.cs
│   │   ├── ScheduleTab.cs
│   │   ├── SalaryTab.cs
│   │   ├── DocumentTab.cs
│   │   ├── HandoverTab.cs
│   │   ├── InventoryTab.cs
│   │   ├── AttendanceTab.cs
│   │   └── SettingsTab.cs
│   ├── Controls/                   # 커스텀 컨트롤
│   │   ├── TimeTablePanel.cs       # 타임테이블 GDI+ 패널
│   │   └── MarkdownViewer.cs       # MD 뷰어
│   ├── Dialogs/                    # 팝업 다이얼로그
│   │   ├── AdminAuthDialog.cs
│   │   ├── EmployeePickerDialog.cs
│   │   └── ScheduleInputDialog.cs
│   └── Helpers/                    # UI 헬퍼
│       ├── ColorPalette.cs
│       └── ToastNotification.cs
│
├── CubeManager.Core/               # 비즈니스 로직 (클래스 라이브러리)
│   ├── Models/                     # 데이터 모델 (POCO)
│   │   ├── Employee.cs
│   │   ├── Schedule.cs
│   │   ├── Attendance.cs
│   │   ├── Reservation.cs
│   │   ├── DailySales.cs
│   │   ├── SaleItem.cs
│   │   ├── CashBalance.cs
│   │   ├── SalaryRecord.cs
│   │   ├── Handover.cs
│   │   ├── HandoverComment.cs
│   │   ├── InventoryItem.cs
│   │   └── Holiday.cs
│   ├── Interfaces/                 # 인터페이스 정의
│   │   ├── Repositories/
│   │   │   ├── IEmployeeRepository.cs
│   │   │   ├── IScheduleRepository.cs
│   │   │   └── ...
│   │   └── Services/
│   │       ├── IScheduleService.cs
│   │       ├── ISalaryService.cs
│   │       └── ...
│   └── Services/                   # 비즈니스 로직 구현
│       ├── ScheduleService.cs
│       ├── SalaryService.cs
│       ├── AttendanceService.cs
│       ├── ReservationService.cs
│       ├── SalesService.cs
│       ├── HolidayService.cs
│       ├── DocumentService.cs
│       ├── HandoverService.cs
│       └── InventoryService.cs
│
├── CubeManager.Data/               # 데이터 액세스 (클래스 라이브러리)
│   ├── Database.cs                 # SQLite 연결 + PRAGMA 설정
│   ├── Migrations/                 # 테이블 생성/변경 스크립트
│   │   ├── MigrationRunner.cs
│   │   ├── V001_InitBase.cs        # employees, app_config
│   │   ├── V002_Schedule.cs        # schedules, holidays
│   │   ├── V003_Reservation.cs     # reservations, daily_sales, sale_items, cash_balance
│   │   ├── V004_Salary.cs          # salary_records
│   │   ├── V005_Attendance.cs      # attendance
│   │   ├── V006_Handover.cs        # handovers, handover_comments
│   │   └── V007_Inventory.cs       # inventory
│   └── Repositories/               # Repository 구현
│       ├── EmployeeRepository.cs
│       ├── ScheduleRepository.cs
│       └── ...
│
└── CubeManager.Tests/              # 테스트 프로젝트
    ├── Services/
    └── Repositories/
```

### 프로젝트 참조 규칙 (절대 위반 금지)

```
CubeManager (UI)
  └─ 참조 → CubeManager.Core
  └─ 참조 → CubeManager.Data

CubeManager.Core (비즈니스)
  └─ 참조 → 없음 (외부 의존성 없음!)
     ※ Repository 인터페이스만 Core에 정의
     ※ 구현체는 Data에 있음

CubeManager.Data (데이터)
  └─ 참조 → CubeManager.Core (모델+인터페이스 사용)
  └─ NuGet: Microsoft.Data.Sqlite, Dapper

의존성 방향: UI → Core ← Data
Core는 아무것도 참조하지 않는다!
```

```
┌─────────────┐     ┌──────────────────┐     ┌──────────────┐
│  UI (Forms)  │────▶│  Core (Services) │◀────│ Data (Repos) │
│  WinForms    │     │  Models          │     │ SQLite       │
│  AntdUI      │     │  Interfaces      │     │ Dapper       │
└─────────────┘     └──────────────────┘     └──────────────┘
     │                      ▲                       │
     │                      │                       │
     └──────────────────────┴───────────────────────┘
              UI가 Data를 참조하는 이유:
              DI 컨테이너에서 Repository 등록 시 필요
```

---

## Step 0: 프로젝트 뼈대

> 목표: 솔루션 생성, 빈 앱이 실행되는 것까지

### 0-1. 솔루션 + 프로젝트 생성

```
작업:
  1. CubeManager.sln 생성
  2. CubeManager (WinForms App, .NET 8) 프로젝트 추가
  3. CubeManager.Core (Class Library, .NET 8) 프로젝트 추가
  4. CubeManager.Data (Class Library, .NET 8) 프로젝트 추가
  5. 프로젝트 참조 설정 (위 규칙대로)

NuGet 설치:
  - CubeManager: AntdUI
  - CubeManager.Data: Microsoft.Data.Sqlite, Dapper
  - CubeManager.Core: BCrypt.Net-Next

산출물:
  ✓ 솔루션 빌드 성공
  ✓ 빈 윈도우 실행 확인
```

### 0-2. DB 기반 인프라

```
작업:
  1. CubeManager.Data/Database.cs 작성
     - SQLite 연결 문자열 (%APPDATA%/CubeManager/cubemanager.db)
     - PRAGMA 설정 (WAL, synchronous, cache_size 등)
     - 연결 팩토리 메서드
  2. CubeManager.Data/Migrations/MigrationRunner.cs 작성
     - 버전 관리 테이블 (schema_version)
     - 마이그레이션 순차 실행 로직
  3. V001_InitBase.cs: employees + app_config 테이블만 생성

테스트:
  ✓ 앱 시작 시 DB 파일 자동 생성
  ✓ 테이블 2개 존재 확인
  ✓ 앱 재시작 시 마이그레이션 중복 실행 안 됨
```

### 0-3. 메인 폼 + 탭 프레임

```
작업:
  1. MainForm.cs: AntdUI TabControl로 8개 탭 배치
  2. 각 탭에 빈 UserControl 연결 (placeholder)
  3. 탭 지연 로딩 구현 (탭 클릭 시 최초 1회만 UserControl 생성)
  4. 상태바: 현재 시각 표시 (1초 타이머)

산출물:
  ✓ 8개 탭 전환 동작
  ✓ 탭 클릭 시 UserControl 생성 로그 확인
  ✓ 메모리 측정: ~15MB 이내
```

### 0-4. 공통 컴포넌트

```
작업:
  1. AdminAuthDialog.cs: 관리자 비밀번호 입력 팝업
     - 비밀번호 입력 → BCrypt 검증 → true/false 반환
     - 인증 후 5분간 캐시 (재인증 불필요)
  2. ToastNotification.cs: 하단 우측 알림
     - Success(초록), Warning(노란), Error(빨간)
     - 3초 후 자동 사라짐
  3. ColorPalette.cs: 전역 색상 상수
     - 결제 태그 색상, 직원 색상 팔레트, 상태 색상
  4. 최초 실행 시 관리자 비밀번호 설정 화면
     - app_config에 admin_password_hash 저장

의존성: 0-2 (DB에서 비밀번호 해시 읽기/쓰기)

산출물:
  ✓ 최초 실행 → 비밀번호 설정 → 재실행 시 비밀번호 요구 안 함
  ✓ AdminAuthDialog 호출 → 비밀번호 맞으면 true
  ✓ 토스트 알림 3종 동작
```

---

## Step 1: 직원 관리 (설정 탭)

> 목표: 직원 CRUD 완성. 이후 모든 기능의 기반 데이터.

### 1-1. Model + Repository

```
작업:
  1. CubeManager.Core/Models/Employee.cs
     - Id, Name, HourlyWage, IsActive, Phone, CreatedAt, UpdatedAt
  2. CubeManager.Core/Interfaces/Repositories/IEmployeeRepository.cs
     - GetAll(), GetActive(), GetById(id), Insert(emp), Update(emp), Deactivate(id)
  3. CubeManager.Data/Repositories/EmployeeRepository.cs
     - Dapper로 위 인터페이스 구현

테스트:
  ✓ Insert → GetAll → 데이터 일치
  ✓ Deactivate → IsActive = false 확인
```

### 1-2. Service

```
작업:
  1. CubeManager.Core/Interfaces/Services/IEmployeeService.cs
  2. CubeManager.Core/Services/EmployeeService.cs
     - 비즈니스 규칙: 이름 중복 검사, 시급 양수 검증
     - GetActiveEmployees(): 스케줄 추가 가능한 직원 목록

테스트:
  ✓ 이름 중복 시 예외
  ✓ 시급 0 이하 시 예외
```

### 1-3. UI (설정 탭)

```
작업:
  1. SettingsTab.cs 구현
     - 직원 목록 DataGridView (이름, 시급, 연락처, 상태)
     - 활성 체크박스 컬럼
     - [추가] 버튼 → 입력 폼 (이름/시급/연락처)
     - [삭제] 버튼 → 비활성화 처리
     - 행 더블클릭 → 수정
  2. EmployeePickerDialog.cs
     - 활성 직원 목록에서 1명 선택하는 공통 팝업
     - 이후 스케줄 추가, 출퇴근 등에서 재사용

산출물:
  ✓ 직원 추가/수정/비활성화 동작
  ✓ 앱 재시작 후 데이터 유지
  ✓ EmployeePickerDialog에서 직원 선택 가능
```

---

## Step 2: 스케줄 - 데이터 레이어

> 목표: 스케줄 데이터 구조 완성. UI는 다음 Step에서.

### 2-1. DB 마이그레이션

```
작업:
  1. V002_Schedule.cs: schedules 테이블 생성
  2. V002_Schedule.cs: holidays 테이블 생성 (같은 마이그레이션)
  3. holidays에 현재 연도 공휴일 내장 데이터 INSERT

산출물:
  ✓ 마이그레이션 실행 후 테이블 2개 추가 확인
```

### 2-2. Model + Repository

```
작업:
  1. Schedule.cs 모델
  2. Holiday.cs 모델
  3. IScheduleRepository.cs
     - GetByWeek(year, month, weekNum): 주간 스케줄
     - GetByEmployeeAndMonth(empId, yearMonth): 직원 월간 스케줄
     - Insert(schedule), Update(schedule), Delete(id)
     - BulkInsert(schedules): 일괄 추가 (트랜잭션 배치)
  4. IHolidayRepository.cs
     - GetByYear(year), IsHoliday(date), IsWeekdayHoliday(date)
  5. Repository 구현체들

테스트:
  ✓ BulkInsert 5건 → GetByWeek에서 5건 조회
  ✓ IsWeekdayHoliday("2026-03-01") → 삼일절이지만 일요일이면 false
```

### 2-3. Service

```
작업:
  1. IScheduleService.cs
  2. ScheduleService.cs
     - AddSchedule(empId, startTime, endTime, dayOfWeeks[], month)
       → 요일별로 해당 월의 모든 날짜에 schedule 레코드 생성
     - GetWeekSchedule(year, month, weekNum)
       → 타임테이블 렌더링용 데이터 구조 반환
     - CalculateWeeklyHours(empId, year, month, weekNum)
     - DeleteSchedule(scheduleId)
  3. IHolidayService.cs + HolidayService.cs
     - 공공데이터포털 API 호출 (오프라인 시 내장 데이터)
     - IsWeekdayHoliday(date): 평일 공휴일 판별

주의:
  ScheduleService는 EmployeeService에 의존하지 않는다.
  employee_id만 받아서 처리. 직원 유효성은 UI에서 먼저 검증.

테스트:
  ✓ AddSchedule(emp1, 14:00, 19:30, [월,화,수,목,금], 2026-03)
    → 해당 월의 평일 날짜들에 레코드 생성
  ✓ GetWeekSchedule → 7일 × 31슬롯 데이터 구조
  ✓ CalculateWeeklyHours → 5일 × 5.5h = 27.5h
```

---

## Step 3: 스케줄 - 타임테이블 UI

> 목표: 핵심 UI인 커스텀 타임테이블 Panel 완성.
> 이 Step이 가장 복잡하므로 세분화한다.

### 3-1. TimeTablePanel 기본 골격

```
작업:
  1. Controls/TimeTablePanel.cs (UserControl, GDI+ 더블 버퍼링)
  2. 그리드 그리기
     - 열: 시간 라벨 (10:00 ~ 01:00, 30분 간격 = 31행)
     - 행: 7일 (월~일)
     - 헤더: 날짜 표시 (3/16 월, 3/17 화, ...)
  3. 셀 크기 자동 계산 (Panel 크기 ÷ 행열 수)

산출물:
  ✓ 빈 그리드가 화면에 렌더링됨
  ✓ 리사이즈 시 셀 크기 자동 조절
```

### 3-2. 스케줄 블록 렌더링

```
작업:
  1. 데이터 바인딩: ScheduleService.GetWeekSchedule() 결과를 Panel에 전달
  2. 연속 시간대 → 하나의 색상 블록으로 병합 렌더링
     - 예: 홍길동 14:00~19:30 → 하나의 파란 막대
  3. 직원별 고유 색상 자동 할당 (ColorPalette)
  4. 블록 내부에 직원 이름 텍스트 표시
  5. 같은 시간대에 여러 직원 → 블록을 좌우로 분할

산출물:
  ✓ 스케줄 데이터가 색상 블록으로 렌더링
  ✓ 여러 직원이 겹치면 분할 표시
```

### 3-3. 블록 인터랙션

```
작업:
  1. 블록 클릭 → 직원 정보 툴팁 (이름, 시간, 근무시간)
  2. 블록 우클릭 → 컨텍스트 메뉴 (수정/삭제)
  3. 빈 셀 더블클릭 → ScheduleInputDialog 열기
     - ScheduleInputDialog: 직원 선택 + 시간 + 요일 설정

산출물:
  ✓ 빈 셀에서 스케줄 추가 → 블록 즉시 렌더링
  ✓ 블록 우클릭 삭제 → 블록 사라짐
```

### 3-4. 편집 모드

```
작업:
  1. [편집 모드 🔒] 버튼 → AdminAuthDialog → 인증 성공 시 편집 활성화
  2. 편집 모드에서: 셀 클릭 → 직접 이름 타이핑 가능
  3. 드래그로 여러 셀 선택 → 일괄 입력/삭제
  4. 5분 비활동 → 자동 잠금

산출물:
  ✓ 편집 모드 진입/종료
  ✓ 셀에 직접 이름 입력 → DB 반영
```

### 3-5. 스케줄 탭 조립

```
작업:
  1. ScheduleTab.cs에 모든 부품 조립
     - 상단: 월/주 네비게이션 (◀ 이전주 / 다음주 ▶)
     - 중앙: TimeTablePanel
     - 하단: 범례 (직원별 색상) + 주간 근무시간 요약
  2. [+ 직원 추가] 버튼 → ScheduleInputDialog
  3. 주차 전환 시 데이터 다시 로드

산출물:
  ✓ 주간 스케줄 조회/추가/수정/삭제 전체 동작
  ✓ 주차 이동 시 정상 전환
  ✓ 근무시간 합계 표시
```

---

## Step 4: 출퇴근

> 목표: 스케줄 데이터를 기반으로 출퇴근 기록 + 판정

### 4-1. DB + Model + Repository

```
작업:
  1. V005_Attendance.cs: attendance 테이블 생성
  2. Attendance.cs 모델
  3. IAttendanceRepository + 구현체
     - ClockIn(empId, date, time), ClockOut(empId, date, time)
     - GetByDate(date), GetByEmployeeMonth(empId, yearMonth)

의존성: Step 0 (DB 인프라)
```

### 4-2. Service

```
작업:
  1. IAttendanceService + AttendanceService
     - ClockIn(empId): 현재 시각 기록 + 스케줄 대비 판정
     - ClockOut(empId): 현재 시각 기록 + 스케줄 대비 판정
     - JudgeClockIn(scheduled, actual) → on_time / late
     - JudgeClockOut(scheduled, actual) → on_time / early
     - GetTodayStatus(): 오늘 근무 예정 직원 + 출퇴근 현황

의존성: Step 2 (IScheduleRepository - 예정 시간 조회)

주의:
  AttendanceService는 IScheduleRepository를 주입받아 예정 시간을 조회한다.
  ScheduleService를 직접 참조하지 않는다 (순환 의존 방지).
```

### 4-3. UI

```
작업:
  1. AttendanceTab.cs
     - 오늘 근무 예정 직원 테이블 (DataGridView)
     - 직원 선택 드롭다운
     - 출근/퇴근 버튼 (큰 버튼, 현재 시각 표시)
     - 결과 색상: 🔵파란(정상) / 🔴빨간(지각/조퇴)
  2. 이력 조회 (월별)

산출물:
  ✓ 출근 버튼 → 시각 기록 + 색상 판정
  ✓ 퇴근 버튼 → 시각 기록 + 색상 판정
  ✓ 이미 출근한 직원은 출근 버튼 비활성화
```

### 4-4. 타임테이블 색상 반영

```
작업:
  1. TimeTablePanel에 출퇴근 상태 오버레이 추가
     - 출근 셀: 파란 체크(정상) / 빨간 글씨(지각)
     - 퇴근 셀: 파란 체크(정상) / 빨간 글씨(조퇴)
  2. ScheduleTab에서 AttendanceService 데이터도 함께 로드

의존성: Step 3 (TimeTablePanel), Step 4-2 (AttendanceService)

산출물:
  ✓ 스케줄 타임테이블에서 출퇴근 색상 확인 가능
```

---

## Step 5: 예약 & 매출

> 목표: 웹 연동 + 매출 관리. 다른 기능과 독립적.

### 5-1. 웹 스크래핑 서비스

```
작업:
  1. NuGet: AngleSharp 설치 (CubeManager.Core에)
  2. IReservationScraperService + ReservationScraperService
     - Login(id, pw): AngleSharp BrowsingContext + 쿠키 자동관리
     - FetchReservations(date): HTML 테이블 파싱 → List<Reservation>
  3. 설정 탭에 웹 연동 설정 UI 추가 (URL, ID, PW, 연결 테스트)

주의:
  스크래핑 서비스는 async로 구현 (UI 블로킹 방지).
  타임아웃 10초. 실패 시 예외를 UI까지 전파하지 않고 캐시 데이터 반환.

테스트:
  ✓ 실제 사이트 로그인 성공 확인
  ✓ 특정 날짜 예약 데이터 파싱 확인
  ✓ 네트워크 끊김 시 예외 처리 확인
```

### 5-2. 예약 DB + Repository

```
작업:
  1. V003_Reservation.cs: reservations, daily_sales, sale_items, cash_balance 테이블
  2. Reservation.cs, DailySales.cs, SaleItem.cs, CashBalance.cs 모델
  3. IReservationRepository + 구현체
  4. ISalesRepository + 구현체

의존성: Step 0 (DB 인프라)
```

### 5-3. 매출 Service

```
작업:
  1. ISalesService + SalesService
     - AddSaleItem(item): 매출/지출 항목 추가
     - GetDailySummary(date): 결제수단별 합계
     - CalculateCashBalance(date): 전일 이월 + 당일 현금 = 잔액
  2. ReservationService (스크래핑 결과 DB 저장)
     - SyncReservations(date): 웹에서 조회 → DB 저장

주의:
  SalesService는 ReservationService에 의존하지 않는다.
  예약과 매출은 sale_items.reservation_id FK로만 연결.
```

### 5-4. 예약/매출 UI

```
작업:
  1. ReservationSalesTab.cs
     - 상단: 날짜 선택 + 조회 버튼 + 자동갱신 체크박스
     - 중단: 예약 테이블 (DataGridView, 읽기전용)
     - 하단: 매출 항목 테이블 + 합계 패널
  2. [+ 매출 추가] [+ 지출 추가] 다이얼로그
  3. 결제 태그 색상 (카드=파랑, 현금=초록, 계좌=노랑)
  4. 현금 잔액 실시간 표시

산출물:
  ✓ 날짜 입력 → 예약 데이터 조회/표시
  ✓ 매출 추가 → 합계 자동 갱신
  ✓ 현금 잔액 표시 정확
```

---

## Step 6: 급여 관리

> 목표: 스케줄 + 출퇴근 + 공휴일 데이터를 종합하여 급여 자동 계산

### 6-1. DB + Model + Repository

```
작업:
  1. V004_Salary.cs: salary_records 테이블
  2. SalaryRecord.cs 모델
  3. ISalaryRepository + 구현체

의존성: Step 0
```

### 6-2. Service

```
작업:
  1. ISalaryService + SalaryService
     - CalculateMonthlySalary(empId, yearMonth):
       ① IScheduleRepository로 주차별 근무시간 집계
       ② IHolidayRepository로 평일 공휴일 근무시간 산출
       ③ 식비 계산 (6h 이상 근무일 × 식비 단가)
       ④ 택시비 계산 (23:30 이후 퇴근일 × 10,000)
       ⑤ 총급여 = 기본급 + 공휴일수당 + 식비 + 택시비
       ⑥ 세금 = 총급여 × 0.033
       ⑦ 실수령 = 총급여 - 세금
     - GetMonthlySalaryTable(yearMonth): 전 직원 급여 테이블

의존성:
  - IScheduleRepository (Step 2)
  - IHolidayRepository (Step 2)
  - IEmployeeRepository (Step 1)
  - 설정값: app_config에서 식비단가, 택시비, 공휴일수당 읽기

주의:
  SalaryService는 Repository 인터페이스만 주입받는다.
  ScheduleService, AttendanceService를 직접 참조하지 않는다.

테스트:
  ✓ 주 37.5h × 4주 = 150h, 시급 10,000 → 기본급 1,500,000
  ✓ 공휴일(평일) 8h → +24,000
  ✓ 6h 이상 근무 20일 → 식비 100,000
  ✓ 23:30↑ 퇴근 3일 → 택시비 30,000
  ✓ 3.3% 세금 계산 정확
```

### 6-3. UI

```
작업:
  1. SalaryTab.cs
     - 월 선택 네비게이션
     - 급여 테이블 (DataGridView, 가로 스크롤)
     - 컬럼: 이름|시급|1주|2주|3주|4주|5주|합계|식비|택시|공휴일|총급여|세금|실수령
     - 수기 수정 셀: 노란 배경 표시
  2. [편집 모드 🔒] → AdminAuthDialog → 셀 직접 수정 가능
  3. 식비/택시비 클릭 → 상세 내역 팝업
  4. 이번달 공휴일 표시 패널

산출물:
  ✓ 월 전환 → 전 직원 급여 자동 계산 표시
  ✓ 수기 수정 → 노란 배경 + DB 저장
  ✓ 수기 수정 셀은 자동 재계산에서 제외
```

---

## Step 7: 부가 기능 (독립)

> 이 3개 기능은 서로 의존성이 없으므로 순서 무관.

### 7-1. 물품 관리

```
작업:
  1. V007_Inventory.cs: inventory 테이블
  2. InventoryItem.cs + IInventoryRepository + 구현체
  3. IInventoryService + InventoryService
  4. InventoryTab.cs
     - DataGridView (물품명, 보유기준, 현재수량, 부족, 비고)
     - 현재수량 셀: 직접 편집 가능
     - 부족 자동계산 + 색상 (빨강/초록)

난이도: ★☆☆ (가장 간단)
```

### 7-2. 인수인계

```
작업:
  1. V006_Handover.cs: handovers + handover_comments 테이블
  2. Handover.cs, HandoverComment.cs + Repository
  3. IHandoverService + HandoverService
  4. HandoverTab.cs
     - 글 목록 (카드형 레이아웃, 최신순)
     - 댓글/대댓글 (들여쓰기 2단계까지)
     - 페이지네이션 (10건)
     - [새 글 작성] 다이얼로그

난이도: ★★☆
```

### 7-3. 업무자료

```
작업:
  1. NuGet: Markdig 설치
  2. DocumentService: 파일시스템 기반 (data/documents/ 폴더)
  3. Controls/MarkdownViewer.cs
     - Markdig로 MD → RTF 변환 → RichTextBox 표시
  4. DocumentTab.cs
     - 좌측: TreeView (폴더 트리)
     - 우측: MarkdownViewer (조회) / TextBox (편집)
     - 검색: 전문 검색 (파일명 + 내용)

난이도: ★★☆

주의:
  이 기능은 DB를 사용하지 않는다 (파일시스템 직접 접근).
  다른 기능과 완전히 독립적.
```

---

## Step 8: 설정 탭 완성 + 마무리

### 8-1. 설정 탭 나머지

```
작업:
  1. 웹 연동 설정 (URL, ID, PW, 연결 테스트) ← Step 5에서 기본은 만듦
  2. 급여 설정 (식비 단가, 택시비, 공휴일 수당)
  3. 관리자 비밀번호 변경
  4. 데이터 백업/복원
     - 백업: SQLite Online Backup API → 파일 복사
     - 복원: DB 교체 + 앱 재시작
```

### 8-2. 통합 테스트

```
시나리오:
  1. 직원 추가 → 스케줄 등록 → 출퇴근 → 급여 계산 (전체 흐름)
  2. 예약 조회 → 매출 등록 → 현금 잔액 확인 (전체 흐름)
  3. 월 전환 시 데이터 정합성
  4. 앱 재시작 후 데이터 유지
```

### 8-3. 배포 준비

```
작업:
  1. Inno Setup 스크립트 작성
  2. .NET 8 런타임 번들 포함
  3. 시작 메뉴 바로가기
  4. 첫 실행 가이드
```

---

## 의존성 그래프 (전체)

```
Step 0: 프로젝트 뼈대
  ├── 0-1: 솔루션 생성
  ├── 0-2: DB 인프라 ← 0-1
  ├── 0-3: 메인 폼 ← 0-1
  └── 0-4: 공통 컴포넌트 ← 0-2, 0-3
       │
Step 1: 직원 관리 ← 0-4
  ├── 1-1: Model + Repo
  ├── 1-2: Service ← 1-1
  └── 1-3: UI ← 1-2
       │
Step 2: 스케줄 데이터 ← 1-1 (employees 테이블 필요)
  ├── 2-1: DB 마이그레이션
  ├── 2-2: Model + Repo ← 2-1
  └── 2-3: Service ← 2-2
       │
Step 3: 스케줄 UI ← 2-3
  ├── 3-1: Panel 골격
  ├── 3-2: 블록 렌더링 ← 3-1, 2-3
  ├── 3-3: 인터랙션 ← 3-2
  ├── 3-4: 편집 모드 ← 3-3, 0-4(AdminAuth)
  └── 3-5: 탭 조립 ← 3-4
       │
Step 4: 출퇴근 ← 2-2 (스케줄 Repo)
  ├── 4-1: DB + Model + Repo
  ├── 4-2: Service ← 4-1, 2-2
  ├── 4-3: UI ← 4-2
  └── 4-4: 타임테이블 반영 ← 3-5, 4-2
       │
Step 5: 예약/매출 ← 0-4 (독립, Step 1~4와 병행 가능)
  ├── 5-1: 웹 스크래핑
  ├── 5-2: DB + Repo
  ├── 5-3: Service ← 5-1, 5-2
  └── 5-4: UI ← 5-3
       │
Step 6: 급여 ← 2-2(스케줄), 1-1(직원), 2-2(공휴일)
  ├── 6-1: DB + Repo
  ├── 6-2: Service ← 6-1, 2-2, 1-1
  └── 6-3: UI ← 6-2
       │
Step 7: 부가기능 ← 0-4 (독립)
  ├── 7-1: 물품 관리
  ├── 7-2: 인수인계
  └── 7-3: 업무자료
       │
Step 8: 마무리 ← ALL
```

### 시각화

```
            Step 0 (뼈대)
                │
         ┌──────┼──────────────────────┐
         │      │                      │
     Step 1   Step 5              Step 7
    (직원)  (예약/매출)         (부가기능)
         │    [독립]           [독립]
     Step 2                  ┌──┼──┐
    (스케줄                 7-1 7-2 7-3
     데이터)
         │
     Step 3
    (스케줄 UI)
       ┌─┘
   Step 4          Step 6
  (출퇴근) ──────▶ (급여)
                      │
                   Step 8
                  (마무리)
```

### 병행 가능 구간

```
구간 A: Step 1 → Step 2 → Step 3 → Step 4 → Step 6 (메인 라인)
구간 B: Step 5 (예약/매출) ← Step 0 이후 언제든 시작 가능
구간 C: Step 7 (부가기능) ← Step 0 이후 언제든 시작 가능

※ 1인 개발이면: Step 0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 순서 권장
※ 2인 이상이면: 메인라인(A) + 부가(B or C) 병행
```

---

## 코드 꼬임 방지 체크리스트

매 Step 완료 시 아래를 점검한다:

```
□ 빌드 에러 0개
□ 새로 추가한 클래스가 올바른 프로젝트에 있는가
  - Model/Interface → Core
  - Repository 구현 → Data
  - UI → CubeManager (앱)
□ Core 프로젝트가 Data나 UI를 참조하고 있지 않은가
□ Service에서 다른 Service를 직접 참조하지 않는가
  (Repository 인터페이스만 주입)
□ UI에서 Repository를 직접 호출하지 않는가
  (Service를 통해서만 접근)
□ DB 마이그레이션 버전 번호가 순차적인가
□ 새 기능이 기존 기능을 깨뜨리지 않는가
  (기존 탭 정상 동작 확인)
```
