# 시스템 아키텍처

## 1. 아키텍처 개요

CubeManager는 **MVP(Model-View-Presenter)** 패턴 기반의 Windows 데스크톱 애플리케이션이다.
> **저사양 대응**: WPF → WinForms 변경. 상세 검토는 [low-spec-review.md](low-spec-review.md) 참고.

```
┌──────────────────────────────────────────────────────┐
│                    UI Layer (Views)                    │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐   │
│  │예약 │ │스케줄│ │급여 │ │업무 │ │인수 │ │출퇴근│  │
│  │매출 │ │관리 │ │관리 │ │자료 │ │인계 │ │관리 │   │
│  └──┬──┘ └──┬──┘ └──┬──┘ └──┬──┘ └──┬──┘ └──┬──┘   │
│     └───────┴───────┴───┬───┴───────┴───────┘        │
├─────────────────────────┼────────────────────────────┤
│                  ViewModel Layer                       │
│         (데이터 바인딩 / 커맨드 / 상태 관리)          │
├─────────────────────────┼────────────────────────────┤
│                  Service Layer                         │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐              │
│  │Web Scraper│ │Calculator │ │File Mgr  │              │
│  │(예약연동) │ │(급여계산) │ │(MD/자료) │              │
│  └──────────┘ └──────────┘ └──────────┘              │
├──────────────────────────────────────────────────────┤
│                  Data Layer                            │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐              │
│  │ SQLite   │ │ MD Files │ │ Config   │              │
│  │ Database │ │ (업무자료)│ │ (JSON)   │              │
│  └──────────┘ └──────────┘ └──────────┘              │
└──────────────────────────────────────────────────────┘
```

## 2. 기술 스택

| 구분 | 기술 | 비고 |
|------|------|------|
| **프레임워크** | WinForms + .NET 8 | 저사양 최적화, 메모리 절약 |
| **UI 스타일** | AntdUI | Ant Design 포트, MIT, .NET 8 지원 |
| **언어** | C# 12 | .NET 생태계 활용 |
| **UI 패턴** | MVP / CodeBehind | WinForms에 적합 |
| **타임테이블** | 커스텀 Panel (GDI+) | 블록형 스케줄 표시 |
| **데이터베이스** | SQLite (WAL 모드) | Microsoft.Data.Sqlite |
| **ORM** | Dapper | 경량 ORM (EF Core 대비 메모리 절약) |
| **웹 스크래핑** | AngleSharp | 로그인+쿠키+HTML 파싱 통합 |
| **MD 렌더링** | Markdig + RichTextBox | RTF 변환, 메모리 2~5MB |
| **공휴일 API** | 공공데이터포털 API | 한국 공휴일 조회 |
| **스케줄러** | 내장 Timer | 자동 데이터 갱신 |

## 3. 모듈 구성

### 3.1 Core Modules

```
CubeManager/
├── CubeManager.App/            # WinForms 앱 프로젝트
├── CubeManager.Core/           # 비즈니스 로직
│   ├── Services/
│   │   ├── ReservationService   # 예약 데이터 스크래핑 & 관리
│   │   ├── SalesService         # 매출 집계 & 결제 분류
│   │   ├── ScheduleService      # 근무 스케줄 CRUD
│   │   ├── SalaryService        # 급여 계산 엔진
│   │   ├── AttendanceService    # 출퇴근 기록 & 판정
│   │   ├── DocumentService      # MD 파일 관리
│   │   ├── HandoverService      # 인수인계 관리
│   │   ├── InventoryService     # 물품 관리
│   │   └── HolidayService       # 공휴일 조회
│   ├── Models/
│   └── Interfaces/
├── CubeManager.Data/           # 데이터 액세스
│   ├── Repositories/
│   ├── Migrations/
│   └── DbContext
└── CubeManager.Tests/          # 테스트
```

### 3.2 데이터 흐름

```
[cubeescape.co.kr] --HTTP--> [ReservationService] --Parse--> [DB: reservations]
                                                                    │
[UI: 예약/매출 탭] <--바인딩-- [ReservationVM] <--쿼리--────────────┘
                                    │
                              [SalesService] --> [DB: daily_sales]
                                    │
[UI: 급여 탭] <--바인딩-- [SalaryVM] <--계산-- [SalaryService]
                                                    │
                              [ScheduleService] ────┘
                                    │
[UI: 스케줄 탭] <--바인딩-- [ScheduleVM]
                                    │
[UI: 출퇴근 탭] <--바인딩-- [AttendanceVM] <-- [AttendanceService]
```

## 4. 인증 및 보안

### 4.1 웹 연동 인증
- cubeescape.co.kr 로그인 세션 관리 (Cookie 기반)
- 로그인 자격증명은 암호화하여 로컬 config에 저장
- DPAPI(Windows Data Protection API) 활용

### 4.2 관리자 비밀번호
- 스케줄/급여 수기 수정 시 관리자 비밀번호 필요
- bcrypt 해싱으로 저장
- 앱 최초 실행 시 관리자 비밀번호 설정

## 5. 데이터 동기화

| 항목 | 주기 | 방식 |
|------|------|------|
| 예약 테이블 | 수동 조회 + 자동(10분) | HTTP GET + HTML 파싱 |
| 스케줄 | 실시간 | 로컬 DB 직접 반영 |
| 급여 | 실시간 계산 | 스케줄 데이터 기반 자동 산정 |
| 출퇴근 | 실시간 | 버튼 클릭 즉시 기록 |
| 공휴일 | 월 1회 | 공공데이터포털 API |

## 6. 배포

- **패키징**: MSIX 또는 ClickOnce
- **자동 업데이트**: Squirrel.Windows 또는 MSIX auto-update
- **최소 요구사항**: Windows 10 이상, .NET 8 Runtime
