# CLAUDE.md — CubeManager 프로젝트 컨텍스트

> 이 파일은 AI 에이전트(Claude)가 프로젝트를 이해하고 일관된 코드를 작성하기 위한 기준 문서입니다.

---

## 프로젝트 정보

- **이름**: CubeManager
- **목적**: Cube Escape 업장의 HR 자동화 + 업무 자동화 Windows 데스크톱 앱
- **대상 환경**: Windows 10/11, 저사양 (4GB RAM, HDD, i3/Celeron급)

---

## 확정 기술 스택

| 항목 | 기술 | 비고 |
|------|------|------|
| 프레임워크 | **WinForms + .NET 8** (C# 12) | 저사양 최적화 |
| UI 라이브러리 | **AntdUI** | MIT, .NET 8 지원, 모던 디자인 |
| DB | **SQLite** (WAL 모드) | Microsoft.Data.Sqlite |
| ORM | **Dapper** | 경량, 수동 SQL |
| 웹 스크래핑 | **AngleSharp** | 로그인+쿠키+파싱 통합 |
| MD 렌더링 | **Markdig → RTF → RichTextBox** | 메모리 2~5MB |
| 비밀번호 해싱 | **BCrypt.Net-Next** | |
| DI 컨테이너 | **Microsoft.Extensions.DependencyInjection** | Singleton 기반 |
| 로깅 | **Serilog** + File Sink | 7일 롤링, %APPDATA%/logs/ |
| 테스트 | **xUnit** + NSubstitute + FluentAssertions | In-Memory SQLite |
| 타임테이블 | **커스텀 Panel (GDI+)** | DataGridView는 일반 표에만 |
| 배포 | **Inno Setup** + Framework-dependent | .NET 8 런타임 번들 |

### 절대 사용하지 않는 것

- WPF, MAUI, Avalonia, Electron (메모리 과다)
- EF Core (Dapper로 충분, 무거움)
- WebBrowser 컨트롤 (IE 기반, deprecated)
- WebView2 (MD 표시 용도로는 메모리 과다)
- MetroFramework, MaterialSkin (미유지보수)
- HtmlAgilityPack (AngleSharp로 대체)

---

## 솔루션 구조

```
CubeManager.sln
├── CubeManager/          # WinForms UI 앱 (.exe)
├── CubeManager.Core/     # 비즈니스 로직 (클래스 라이브러리)
└── CubeManager.Data/     # 데이터 액세스 (클래스 라이브러리)
```

### 프로젝트 참조 규칙 (위반 금지)

```
CubeManager (UI)     →  Core, Data 참조
CubeManager.Core     →  아무것도 참조하지 않음
CubeManager.Data     →  Core 참조 (모델+인터페이스)

의존성 방향:  UI → Core ← Data
```

- **Core 프로젝트는 외부 프로젝트를 절대 참조하지 않는다**
- Model, Interface는 반드시 Core에 위치
- Repository 구현체는 반드시 Data에 위치
- UI(Forms, Controls, Dialogs)는 반드시 CubeManager 앱 프로젝트에 위치

---

## 아키텍처 규칙

### 레이어 순서 (기능 개발 시 반드시 준수)

```
① Model (Core/Models/)        — 데이터 클래스 정의
② Repository (Data/)           — DB 접근 (Dapper SQL)
③ Service (Core/Services/)     — 비즈니스 로직
④ UI (CubeManager/Forms/)     — 화면
```

- UI에서 Repository를 직접 호출하지 않는다 → Service를 통해서만
- Service에서 다른 Service를 직접 참조하지 않는다 → Repository 인터페이스만 주입
- 모든 Service와 Repository는 인터페이스 기반 (Core/Interfaces/)

### DB 마이그레이션

- 테이블은 한번에 전부 만들지 않는다
- 해당 기능 개발 시점에 마이그레이션 파일 추가 (V001, V002, ...)
- MigrationRunner가 버전 순서대로 자동 실행

---

## 코딩 컨벤션

> 상세: docs/policies/coding-conventions.md

- **언어**: C# 12, nullable reference types 활성화
- **네이밍**: PascalCase (public), _camelCase (private field), camelCase (local/parameter)
- **async**: I/O 작업은 반드시 async/await (UI 블로킹 방지)
- **SQL**: 대문자 키워드 (`SELECT`, `INSERT INTO`), 파라미터 바인딩 필수 (SQL Injection 방지)
- **에러 처리**: Service에서 catch, UI까지 예외 전파 금지, 실패 시 기본값/캐시 반환

---

## 저사양 최적화 규칙

> 상세: docs/architecture/low-spec-review.md

- **탭 지연 로딩**: 탭 클릭 시 최초 1회만 UserControl 생성
- **데이터 범위 제한**: 당일/현재 주차/현재 월 데이터만 메모리에 유지
- **페이징**: 인수인계 10건, 출퇴근 이력 30건씩
- **SELECT * 금지**: 필요한 컬럼만 명시적으로 조회
- **GDI+ 더블 버퍼링**: 커스텀 Panel은 반드시 DoubleBuffered = true
- **대량 쓰기는 트랜잭션**: 개별 INSERT 금지, BEGIN/COMMIT으로 묶기
- **리소스**: 시스템 폰트(맑은 고딕), 아이콘은 16x16/24x24 PNG

---

## DB 규칙

> 상세: docs/policies/db-policy.md

- **PRAGMA**: WAL, synchronous=NORMAL, cache_size=-8000, mmap_size=64MB
- **FK**: `PRAGMA foreign_keys = ON` 항상 활성화
- **날짜 형식**: `YYYY-MM-DD` (DATE), `YYYY-MM-DD HH:MM:SS` (DATETIME)
- **금액**: INTEGER (원 단위, 소수점 없음)
- **시간**: `HH:MM` TEXT (24시간제)
- **Boolean**: INTEGER (0/1)
- **인덱스**: 조회 빈도 높은 FK/날짜 컬럼에만, 소량 테이블은 인덱스 금지

---

## 보안 규칙

> 상세: docs/policies/security-policy.md

- 관리자 비밀번호: BCrypt 해싱, 평문 저장 금지
- 웹 로그인 자격증명: DPAPI 암호화하여 app_config에 저장
- SQL 파라미터 바인딩 필수 (문자열 연결로 SQL 절대 금지)
- 인증 후 5분간 캐시, 이후 재인증 필요

---

## UI 규칙

> 상세: docs/policies/ui-policy.md

- **색상 체계**: Primary=#1976D2, Success=#4CAF50, Warning=#FFC107, Danger=#F44336
- **결제 태그**: 카드=#2196F3, 현금=#4CAF50, 계좌=#FFC107, 지출=#F44336
- **출퇴근**: 정상=파란(#2196F3), 지각/조퇴=빨간(#F44336)
- **수기 수정 셀**: 노란 배경 #FFF9C4
- **타임테이블**: 커스텀 Panel(GDI+), 직원별 고유 배경색 자동 할당
- **일반 테이블**: DataGridView (예약, 급여, 물품, 출퇴근 이력)
- **토스트**: 하단 우측 3초 표시 (Success/Warning/Error)

---

## 개발 순서

> 상세: docs/development-guide.md

```
Step 0: 솔루션 뼈대 + DB 인프라 + 공통 컴포넌트
Step 1: 직원 관리 (설정 탭)
Step 2: 스케줄 데이터 레이어
Step 3: 스케줄 타임테이블 UI (커스텀 Panel)
Step 4: 출퇴근
Step 5: 예약/매출 (독립)
Step 6: 급여 관리
Step 7: 부가기능 (물품/인수인계/업무자료)
Step 8: 설정 완성 + 배포
```

각 Step 내에서: Model → Repository → Service → UI 순서 엄수

---

## 핵심 비즈니스 규칙 (요약)

> 상세: docs/policies/business-rules.md

- **주차 계산**: 월요일 시작~일요일 종료, 해당 월 날짜만 포함
- **자정 보정**: 00:00~09:59는 +24시간 (00:30→24:30, 01:00→25:00)
- **금액 반올림**: 모든 소수점은 **내림(truncate)** (세금 계산 포함)
- **식비 기준**: 스케줄 상 근무시간 >= 6.0h (실제 출퇴근 아님)
- **택시비 기준**: 스케줄 상 퇴근시간 >= 23:30 (포함, 실제 퇴근 아님)
- **공휴일 수당**: 평일(월~금) 공휴일만 적용, 주말 공휴일은 미적용

---

## DI 컨테이너

> 상세: docs/architecture/di-container.md

- Service/Repository: **Singleton** (상태 없음, DB 연결은 메서드 내 using)
- UI 탭/다이얼로그: **Transient** (필요 시 생성)
- MainForm만 IServiceProvider 보유, 탭 생성 시 Service 주입
- Service → Repository 인터페이스만 의존 (Service 간 직접 참조 금지)

---

## 로깅

> 상세: docs/architecture/app-initialization.md

- **Serilog + File Sink**: `%APPDATA%/CubeManager/logs/cube-YYYYMMDD.log`
- 보관: 7일 롤링
- 급여/스케줄 수기 수정 시 `[AUDIT]` 태그 감사 로그

---

## 명세 문서 위치

```
docs/
├── architecture/
│   ├── system-architecture.md     # 시스템 아키텍처
│   ├── database-schema.md         # 13개 테이블 스키마
│   ├── di-container.md            # DI 컨테이너 설정
│   ├── app-initialization.md      # 앱 초기화 + 시드 데이터 + 로깅
│   ├── low-spec-review.md         # 저사양 환경 검토
│   └── decision-review.md         # 기술 선택 재검토
├── features/                      # 기능 명세 (01~08)
├── screens/
│   └── ui-specification.md        # 화면 와이어프레임
├── integration/
│   └── cubeescape-scraping.md     # 웹 스크래핑 상세 (HTML 파싱)
├── policies/
│   ├── coding-conventions.md      # 코딩 컨벤션
│   ├── db-policy.md               # DB 정책
│   ├── security-policy.md         # 보안 정책
│   ├── ui-policy.md               # UI 정책
│   ├── naming-conventions.md      # 네이밍 규칙
│   ├── git-policy.md              # Git 정책
│   ├── business-rules.md          # 비즈니스 규칙 (계산/반올림/경계)
│   └── testing-strategy.md        # 테스트 전략
├── development-guide.md           # Step-by-Step 개발 가이드
└── roadmap.md                     # Phase 개요
```

---

## 코드 작성 시 체크리스트

새 코드를 작성하기 전에 반드시 확인:

```
□ 올바른 프로젝트에 파일을 만들고 있는가? (Model→Core, Repo→Data, UI→App)
□ Core 프로젝트가 Data나 UI를 참조하고 있지 않은가?
□ 인터페이스를 먼저 정의했는가?
□ Service에서 다른 Service를 직접 참조하지 않는가?
□ SQL에 파라미터 바인딩을 사용했는가?
□ async I/O인가? (UI 블로킹 없는가?)
□ SELECT * 대신 필요한 컬럼만 조회하는가?
□ 대량 INSERT는 트랜잭션으로 묶었는가?
□ nullable reference 경고가 없는가?
```
