# CubeManager

**Cube Escape 방탈출 업장 HR 자동화 + 업무 자동화 Windows 데스크톱 앱**

---

## 빠른 시작 (다른 PC에서 실행)

### 사전 요구사항
- Windows 10/11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) 설치

### 실행 방법

```bash
# 1. 저장소 클론
git clone https://github.com/DevTigeer/CubeManager.git
cd CubeManager

# 2. 빌드
dotnet build src/CubeManager.sln -c Release

# 3. 실행
dotnet run --project src/CubeManager -c Release
```

### EXE로 배포 (단일 실행파일)

```bash
# Framework-dependent (경량, .NET Runtime 필요)
dotnet publish src/CubeManager/CubeManager.csproj -c Release -r win-x64 --no-self-contained -o publish

# Self-contained (독립 실행, .NET Runtime 불필요, ~80MB)
dotnet publish src/CubeManager/CubeManager.csproj -c Release -r win-x64 --self-contained -o publish-standalone
```

생성된 `publish/CubeManager.exe` 또는 `publish-standalone/CubeManager.exe`를 실행합니다.

---

## 핵심 기능

| # | 기능 | 설명 |
|---|------|------|
| 1 | **예약/매출** | 웹 예약 연동 + 일 매출 집계 + 카드/현금/계좌 분류 + 손님 금액계산기 |
| 2 | **스케줄** | 주간 타임테이블 + 파트별(오픈/마감/미들) 자동 시간 설정 |
| 3 | **체크리스트** | 요일별 오픈/마감 업무 자동 배정 + 완료율 추적 + HR 알림 |
| 4 | **출퇴근** | 실시간 기록 + 스케줄 대비 지각/조퇴 감지 |
| 5 | **인수인계** | 날짜별 인수인계 + 다음근무자 확인 체크 + 댓글 |
| 6 | **무료이용권** | A2000~ 자동 번호 발급 + 사유 태그 + 사용 체크 |
| 7 | **물품** | 비품 재고 현황 (보유량/현재량/부족량) |
| 8 | **업무자료** | 디렉토리 구조 문서 관리 |
| 9 | **테마힌트** | 테마별 힌트 관리 + CSV 내보내기 |
| 10 | **급여** | 주차별 시간 + 공휴일수당 + 식비/택시비 자동 계산 |
| 11 | **관리자** | 대시보드 + 직원관리 + 파트관리 + 알람 + 체크리스트 관리 + 출퇴근 이력 + 설정 |

## 기술 스택

| 항목 | 기술 |
|------|------|
| 프레임워크 | WinForms + .NET 8 (C# 12) |
| DB | SQLite (WAL 모드) + Dapper |
| 웹 스크래핑 | HttpClient + AngleSharp |
| 보안 | BCrypt (비밀번호) + DPAPI (자격증명) |
| 로깅 | Serilog + File Sink (7일 롤링) |
| 디자인 | #2D3047 기반 다크 톤 + 뉴모피즘/글래스 근사 |

## 디자인 색상 가이드

```
주색:   #2D3047 (깊은 네이비 그레이)
보색:   #F18A3D (따뜻한 주황 — CTA/강조)
중성:   #F0F0F0 (밝은 회색 — 표/카드 배경)
삼분색: #47A8D7 (청록 — 활성), #D747A8 (자홍 — 특수)
유사색: #1E2335 (어두운), #3F425D (밝은)
```

## 프로젝트 구조

```
src/
├── CubeManager/           # WinForms UI 앱 (.exe)
│   ├── Controls/          # 커스텀 GDI+ 컨트롤 (타임테이블, 사이드바, 카드)
│   ├── Dialogs/           # 다이얼로그 (스케줄추가, 계산기, 인증)
│   ├── Forms/             # 탭 UI (11개)
│   └── Helpers/           # ColorPalette, ButtonFactory, GridTheme, DesignTokens
├── CubeManager.Core/      # 비즈니스 로직 (모델, 서비스, 인터페이스)
└── CubeManager.Data/      # 데이터 접근 (SQLite, Dapper, 마이그레이션 V001~V019)
```

## 문서

- [사용자 매뉴얼 (HTML)](docs/user-manual.html)
- [운영 매뉴얼](docs/policies/operation-manual.md)
- [색상 정책](docs/policies/color-policy.md)
- [요금/할인 정책](docs/policies/pricing-policy.md)
- [CLAUDE.md](CLAUDE.md) — AI 에이전트 컨텍스트

## 라이선스

Private — Cube Escape 인천구월점 전용
