# Git 정책

## 1. 브랜치 전략

```
main                    ← 안정 버전 (배포 가능)
└── develop             ← 개발 통합 브랜치
    ├── feature/step0-project-setup
    ├── feature/step1-employee
    ├── feature/step2-schedule-data
    ├── feature/step3-schedule-ui
    ├── feature/step4-attendance
    ├── feature/step5-reservation-sales
    ├── feature/step6-salary
    ├── feature/step7-inventory
    ├── feature/step7-handover
    ├── feature/step7-documents
    ├── feature/step8-settings-deploy
    └── fix/이슈설명
```

### 규칙

- `main`에 직접 커밋 금지
- `develop`에서 feature 브랜치 생성 → 완료 후 develop에 머지
- Step 완료 시점에 develop → main 머지
- hotfix는 main에서 브랜치 → main + develop에 머지

---

## 2. 커밋 메시지

### 형식

```
<type>(<scope>): <subject>

<body (선택)>
```

### Type

| Type | 용도 |
|------|------|
| `feat` | 새 기능 |
| `fix` | 버그 수정 |
| `refactor` | 리팩토링 (기능 변경 없음) |
| `style` | 코드 스타일/포맷 변경 |
| `docs` | 문서 변경 |
| `test` | 테스트 추가/수정 |
| `chore` | 빌드, 패키지, 설정 변경 |
| `db` | DB 마이그레이션, 스키마 변경 |

### Scope

```
core     — CubeManager.Core 프로젝트
data     — CubeManager.Data 프로젝트
ui       — CubeManager 앱 (Forms/Controls/Dialogs)
employee — 직원 관련
schedule — 스케줄 관련
salary   — 급여 관련
attend   — 출퇴근 관련
reserve  — 예약/매출 관련
handover — 인수인계 관련
inv      — 물품 관리 관련
doc      — 업무자료 관련
```

### 예시

```
feat(employee): 직원 CRUD 구현
db(data): V001 employees, app_config 테이블 생성
feat(schedule): 주간 타임테이블 GDI+ Panel 구현
fix(salary): 공휴일 수당 계산 시 주말 공휴일 제외 누락 수정
refactor(core): ScheduleService 인터페이스 분리
chore: AntdUI NuGet 패키지 추가
```

---

## 3. 커밋 단위

### 원칙

```
1 커밋 = 1 논리적 변경 단위

✅ 좋은 커밋:
  "feat(employee): Employee 모델 및 인터페이스 정의"
  "feat(data): EmployeeRepository Dapper 구현"
  "feat(ui): SettingsTab 직원 목록 DataGridView"

❌ 나쁜 커밋:
  "작업 중"
  "여러 가지 수정"
  "Step 1 전체" (너무 큼)
```

### Step별 권장 커밋 수

| Step | 예상 커밋 수 | 기준 |
|------|------------|------|
| Step 0 | 4~6 | 솔루션, DB, 메인폼, 공통컴포넌트 |
| Step 1 | 3~4 | Model+Repo, Service, UI |
| Step 2 | 3~4 | Migration, Model+Repo, Service |
| Step 3 | 5~7 | Panel골격, 블록렌더링, 인터랙션, 편집, 탭조립 |
| Step 4 | 4~5 | DB+Repo, Service, UI, 타임테이블연동 |
| Step 5 | 4~5 | 스크래핑, DB+Repo, Service, UI |
| Step 6 | 3~4 | DB+Repo, Service, UI |
| Step 7 | 각 2~3 | 기능별 |
| Step 8 | 3~4 | 설정, 테스트, 배포 |

---

## 4. .gitignore

```gitignore
# .NET
bin/
obj/
*.user
*.suo
*.cache
*.dll
*.exe

# IDE
.vs/
.idea/
*.sln.DotSettings.user

# DB (운영 데이터는 커밋 금지)
*.db
*.db-wal
*.db-shm

# OS
Thumbs.db
.DS_Store

# 빌드 산출물
publish/
packages/

# 사용자 설정
appsettings.local.json
```

### 주의

```
DB 파일(*.db)은 절대 커밋하지 않는다
마이그레이션 코드(V001_InitBase.cs 등)만 커밋
테스트용 시드 데이터는 마이그레이션 또는 별도 스크립트로 관리
```

---

## 5. 코드 리뷰 체크리스트 (PR 시)

```
□ 올바른 프로젝트에 파일이 위치하는가 (Core/Data/UI)
□ Core가 Data나 UI를 참조하지 않는가
□ 인터페이스가 정의되어 있는가
□ SQL 파라미터 바인딩을 사용하는가
□ async/await를 올바르게 사용하는가
□ 에러 처리가 레이어 규칙을 따르는가
□ 네이밍 컨벤션을 따르는가
□ 새 마이그레이션 번호가 순차적인가
□ 커밋 메시지가 형식을 따르는가
□ 기존 기능이 깨지지 않는가
```

---

## 6. 릴리스 태그

```
형식: v{major}.{minor}.{patch}
예시: v0.1.0, v0.2.0, v1.0.0

v0.1.0 — Step 0~1 완료 (직원 관리)
v0.2.0 — Step 2~3 완료 (스케줄)
v0.3.0 — Step 4 완료 (출퇴근)
v0.4.0 — Step 5 완료 (예약/매출)
v0.5.0 — Step 6 완료 (급여)
v0.6.0 — Step 7 완료 (부가기능)
v1.0.0 — Step 8 완료 (첫 정식 릴리스)
```
