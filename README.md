# CubeManager - HR 자동화 및 업무 자동화 시스템

## 프로젝트 개요

CubeManager는 **Cube Escape** 업장의 HR 관리 및 업무 자동화를 위한 Windows 데스크톱 애플리케이션입니다.
예약 관리, 매출 관리, 직원 스케줄, 급여 산정, 근태 관리 등을 하나의 프로그램에서 통합 관리합니다.

## 핵심 기능

| # | 기능 | 설명 |
|---|------|------|
| 1 | **예약/매출 관리** | 웹 예약 테이블 연동, 일 매출 집계, 결제수단별 분류 |
| 2 | **근무 스케줄** | 월간/주간 타임테이블, 직원별 근무시간 배정 |
| 3 | **급여 관리** | 주차별 근무시간 집계, 공휴일 수당, 3.3% 세금 계산 |
| 4 | **식비/택시비** | 6시간 이상 근무 시 식비, 23:30 이후 퇴근 시 택시비 자동 집계 |
| 5 | **업무자료** | Markdown 파일 기반 문서 관리 (검색/수정/삭제/삽입) |
| 6 | **인수인계** | 근무자별 인수인계 작성, 댓글/대댓글 커뮤니티 형식 |
| 7 | **물품 관리** | 비품 재고 현황 (보유량/현재량/부족량) 관리 |
| 8 | **출/퇴근** | 실시간 출퇴근 기록, 스케줄 대비 지각/조퇴 색상 표시 |

## 기술 스택

- **플랫폼**: Windows Desktop
- **프레임워크**: WinForms + .NET 8 + AntdUI (저사양 최적화)
- **언어**: C#
- **데이터베이스**: SQLite (로컬)
- **웹 연동**: HTTP Client (예약 데이터 스크래핑)
- **문서**: Markdown 렌더러 내장

## 디렉토리 구조

```
Cube/
├── CLAUDE.md                          # AI 에이전트 컨텍스트 (기술스택, 규칙 요약)
├── README.md                          # 프로젝트 개요
├── docs/
│   ├── architecture/
│   │   ├── system-architecture.md     # 시스템 아키텍처
│   │   ├── database-schema.md         # DB 스키마 (13개 테이블)
│   │   ├── low-spec-review.md         # 저사양 환경 검토
│   │   └── decision-review.md         # 기술 선택 재검토
│   ├── features/                      # 기능 명세 (01~08)
│   ├── screens/
│   │   └── ui-specification.md        # 화면 와이어프레임
│   ├── policies/                      # 개발 정책
│   │   ├── coding-conventions.md      # 코딩 컨벤션
│   │   ├── db-policy.md               # DB 정책
│   │   ├── security-policy.md         # 보안 정책
│   │   ├── ui-policy.md               # UI 정책
│   │   ├── naming-conventions.md      # 네이밍 규칙
│   │   └── git-policy.md              # Git 정책
│   ├── development-guide.md           # Step-by-Step 개발 가이드
│   └── roadmap.md                     # Phase 개요
└── src/ (솔루션)
    ├── CubeManager/                   # WinForms UI 앱
    ├── CubeManager.Core/              # 비즈니스 로직
    ├── CubeManager.Data/              # 데이터 접근
    └── CubeManager.Tests/             # 테스트
```

## 명세 문서 목록

- [시스템 아키텍처](docs/architecture/system-architecture.md)
- [저사양 환경 검토](docs/architecture/low-spec-review.md)
- [기술 선택 재검토](docs/architecture/decision-review.md)
- [DB 스키마](docs/architecture/database-schema.md)
- [예약/매출 관리](docs/features/01-reservation-sales.md)
- [근무 스케줄](docs/features/02-work-schedule.md)
- [급여 관리](docs/features/03-salary.md)
- [식비/택시비](docs/features/04-meal-taxi.md)
- [업무자료](docs/features/05-work-documents.md)
- [인수인계](docs/features/06-handover.md)
- [물품 관리](docs/features/07-inventory.md)
- [출/퇴근](docs/features/08-attendance.md)
- [화면 명세](docs/screens/ui-specification.md)
- [개발 가이드 (Step-by-Step)](docs/development-guide.md)
- [개발 로드맵 (Phase 개요)](docs/roadmap.md)

### 정책 문서

- [코딩 컨벤션](docs/policies/coding-conventions.md)
- [DB 정책](docs/policies/db-policy.md)
- [보안 정책](docs/policies/security-policy.md)
- [UI 정책](docs/policies/ui-policy.md)
- [네이밍 규칙](docs/policies/naming-conventions.md)
- [Git 정책](docs/policies/git-policy.md)

### AI 에이전트

- [CLAUDE.md](CLAUDE.md) — 프로젝트 컨텍스트 (기술스택, 규칙, 체크리스트)
