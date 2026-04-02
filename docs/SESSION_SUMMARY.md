# CubeManager 개발 세션 요약

> 최종 업데이트: 2026-04-02

---

## 1. 프로젝트 개요

| 항목 | 내용 |
|------|------|
| **앱명** | CubeManager |
| **목적** | 방탈출 매장 HR 자동화 + 업무 자동화 Windows 데스크톱 앱 |
| **대상** | Cube Escape 인천구월점 |
| **환경** | Windows 10/11, 8GB RAM, 2020년급 노트북 |
| **기술** | WinForms + .NET 8 (C# 12), SQLite, Dapper, GDI+ |
| **리포** | github.com/DevTigeer/CubeManager (Public) |
| **브랜치** | develop (개발), main (배포) |

---

## 2. 디자인 시스템 (#2D3047 컬러 가이드)

### 색상 정책
```
주색:   #2D3047 (깊은 네이비 그레이) — 메인 배경
보색:   #F18A3D (따뜻한 주황) — CTA/강조/현재시간선
중성:   #F0F0F0 (밝은 회색) — 표/카드 배경

삼분색: #47A8D7 (청록) — 활성/링크
        #D747A8 (자홍) — 특수 강조

유사색: #1E2335 (더 어두운) — 사이드바
        #3F425D (약간 밝은) — 카드/헤더
```

### 디자인 원칙
- 배경: 3단 계층 (#1E2335 → #2D3047 → #3F425D)
- 표/카드: 배경과 강한 대비 (#F0F0F0 밝은 배경)
- 텍스트: **모두 Bold**, 크기로만 계층 구분
- 포인트: 주황(CTA), 청록(활성), 초록(성공), 빨강(위험)만

### 폰트 체계
| 용도 | 폰트 | 크기 |
|------|------|------|
| 페이지 제목 | Aptos Bold | 16f |
| 섹션 제목 | Aptos Bold | 13f |
| 탭/메뉴 | Aptos Bold | 10.5f |
| 본문 | 맑은 고딕 Bold | 10f |
| 캡션 | 맑은 고딕 Bold | 8.5f |
| 통계값 | Segoe UI Bold | 24f |
| 버튼 | Aptos Bold | 10f |

### 적용된 트렌드
- 뉴모피즘: 밝은/어두운 그림자 쌍 (RoundedCard)
- 글래스모피즘 근사: 반투명 배경 + 흰 테두리 (블러 없이)
- 마이크로 인터랙션: 버튼 Pressed 1px 오프셋
- 다크 톤 UI: 전체 어두운 배경 + 밝은 텍스트

---

## 3. 솔루션 구조

```
CubeManager.sln
├── CubeManager/          # WinForms UI 앱 (.exe)
│   ├── Controls/         # 커스텀 컨트롤 (SideNav, Header, TimeTable, SummaryCard, RoundedCard)
│   ├── Dialogs/          # 다이얼로그 (계산기, 스케줄입력, 관리자인증, 손님계산 등)
│   ├── Forms/            # 탭 UserControl (11개 탭)
│   └── Helpers/          # ColorPalette, ButtonFactory, GridTheme, ControlFactory, DesignTokens
├── CubeManager.Core/     # 비즈니스 로직 (모델, 인터페이스, 서비스)
└── CubeManager.Data/     # 데이터 액세스 (Repository, Migrations, Database)
```

### 의존성 방향
```
UI → Core ← Data
(Core는 아무것도 참조하지 않음)
```

---

## 4. 탭 구성 (11개)

| # | 탭명 | 사이드바 아이콘 | 주요 기능 |
|---|------|:---:|------|
| 0 | 예약/매출 | 📅 | 웹 스크래핑 예약 조회, 결제 입력, 통계, 손님계산기, 간이계산기 |
| 1 | 스케줄 | 📋 | 주간 타임테이블 (GDI+), 파트 기반 자동 시간, 우클릭 수정/삭제 |
| 2 | 체크리스트 | ✅ | 요일별 체크리스트, 오픈/마감/미들 역할별, 진행률 바 |
| 3 | 출퇴근 | ⏰ | 현재시간 표시, 출/퇴근 버튼, 오늘 스케줄 근무자 |
| 4 | 인수인계 | 📝 | 카드형 리스트, 댓글, 다음근무자 확인 체크, 미확인=하늘색 |
| 5 | 무료이용권 | 🎫 | A2000+ 자동번호, 사유 태그, 사용체크 |
| 6 | 물품 | 📦 | 재고 관리, 보유기준 대비 현재수량 |
| 7 | 업무자료 | 📄 | 디렉토리 구조 + 문서 표시 |
| 8 | 테마힌트 | 🔑 | 테마별 힌트 관리 |
| 9 | 설정 | ⚙️ | 웹 연동 (URL/ID/PW) |
| 10 | 관리자 | 🛡️ | 비밀번호 인증 후 접근. 하위 탭으로 구성 |

### 관리자 하위 탭
| 하위탭 | 내용 |
|--------|------|
| 대시보드 | 통계 카드, 현금 보정, DB 백업/초기화, 비밀번호 변경 |
| 급여관리 | 주차별 급여 계산, 공휴일수당, 식비/택시비 |
| 출퇴근이력 | 날짜 범위 조회 |
| 직원관리 | 직원 추가/수정/비활성화 |
| 알람 | 미끼팝업 CRUD + HR 자동 알림 설정 |
| 체크리스트관리 | 요일 체크박스 + 역할 기반 템플릿 CRUD |
| 파트관리 | 근무 파트 CRUD (오픈/마감/미들1/미들2) |
| 알림이력 | 자동 생성된 알림 로그 조회 |

---

## 5. DB 마이그레이션 (V001~V019)

| 버전 | 설명 |
|------|------|
| V001 | 기본 테이블 (employees, app_config) |
| V002 | schedules |
| V003 | attendance |
| V004 | reservations, daily_sales, sale_items, cash_balance |
| V005 | salary_records |
| V006 | handovers, handover_comments, inventory |
| V007 | themes, theme_hints |
| V008 | reservation theme_name 컬럼 |
| V009 | free_passes |
| V010 | mice_popups, checklist_templates, checklist_records |
| V011 | checklist role 컬럼 |
| V012 | sale_items note/verified 컬럼 |
| V013 | handover title/check 컬럼 |
| V014 | alert_logs |
| V015 | work_parts (근무 파트) |
| V016 | 운영 체크리스트 시드 데이터 |
| V017 | checklist_template_days (요일 매핑 분리) |
| V018 | 금요일 마감 체크리스트 |
| V019 | 일요일 체크리스트 (인터폰 + 장치점검) |

---

## 6. 주요 설정값 (app_config)

### 관리자에서 변경 가능
| 키 | 기본값 | 용도 |
|----|--------|------|
| default_meal_allowance | 7000 | 식비 (원) |
| taxi_allowance | 10000 | 택시비 (원) |
| meal_min_hours | 6 | 식비 기준 최소 근무시간 |
| holiday_bonus_per_hour | 3000 | 공휴일 수당 시급 가산 |
| taxi_cutoff_time | 23:30 | 택시비 기준 퇴근시간 |
| alert_checklist_enabled | 1 | 체크리스트 미완료 알림 |
| alert_checklist_minutes | 60 | 출근 후 N분 기준 |
| alert_handover_enabled | 1 | 인수인계 미확인 알림 |
| alert_handover_minutes | 30 | 출근 후 N분 기준 |
| alert_noshow_enabled | 1 | 무단결근 감지 |
| alert_late_enabled | 1 | 지각 누적 경고 |
| alert_late_threshold | 3 | 월간 N회 이상 |
| admin_password_hash | (BCrypt) | 관리자 비밀번호 |

### 하드코딩 (코드 내 고정)
| 항목 | 값 | 파일 |
|------|-----|------|
| 성인 요금표 | 2인=36,000~7인=91,000 | CustomerCalcDialog.cs |
| 아동 요금 | 카드 11,000 / 현금 10,000 | CustomerCalcDialog.cs |
| 할인 정책 | 계좌 1,000 / 군인 2,000 / 생일 2,000 / 재방문 1,000 | CustomerCalcDialog.cs |
| 지점명 | 인천구월점 | HeaderPanel.cs |
| 이용권 시작번호 | A2000 | FreePassRepository.cs |

---

## 7. 웹 스크래핑 (예약 조회)

### 기술
- HttpClient + CookieContainer + AngleSharp DOM 파서
- DPAPI 암호화로 자격증명 저장

### 세션 관리 (3중 방어)
1. **조회 시 세션 검증**: GET /adm/ → 로그인 페이지면 재로그인
2. **HTTP 에러 시 재로그인**: 네트워크 실패 → 1회 재시도
3. **응답 리다이렉트 감지**: login_check/mb_password 포함 시 재로그인

---

## 8. 스케줄 타임테이블

### 현재 방식: 방안A (풀폭 + 투명 겹침)
- 각 직원 블록을 전체 폭으로 반투명하게 렌더링
- 겹치는 구간은 색이 진해짐
- 구간 변경 시 경계선 + 시간 라벨 표시

### 색상 규칙
- 1명 근무: **개인 고유색** (컬러바, tint, 이름)
- 2명+ 겹침: **구간 전용색** (직원색과 별도 팔레트)
- 겹침 구간 이름: 한줄 합침 ("kimjaja, baro")

### 파트 시스템
- 관리자에서 파트 CRUD (오픈/마감/미들1/미들2)
- 스케줄 추가 시 파트 선택 → 출퇴근 시간 자동 설정
- 다중 파트 체크 → 전체 범위로 시간 설정

### 상호작용
- 빈 셀 더블클릭: 스케줄 추가
- 블록 더블클릭: 삭제 확인
- 블록 우클릭: 컨텍스트 메뉴 (직원 변경 / 삭제)

---

## 9. 비즈니스 규칙

### 주차 계산
- 수요일 기반: 해당 주의 수요일이 속한 월에 귀속
- 예: 3/31(화)은 수요일이 4/1이므로 → 4월 1주차

### 금액 계산
- 모든 소수점은 **내림(truncate)**
- 공휴일 수당: 평일(월~금) 공휴일만 적용
- 식비: 스케줄 상 근무시간 >= 6.0h
- 택시비: 스케줄 상 퇴근시간 >= 23:30

### 자정 보정
- 00:00~09:59는 +24시간 (00:30→24:30)

---

## 10. HR 자동 알림 시스템

| 알림 | 조건 | 기본값 |
|------|------|--------|
| 체크리스트 미완료 | 출근 후 N분, 완료율 50% 미만 | 60분 |
| 인수인계 미확인 | 출근 후 N분, 미확인 건 존재 | 30분 |
| 무단결근 | 12시 이후, 스케줄 있는데 출근 없음 | 매일 |
| 지각 누적 | 월간 지각 N회 이상 | 3회 |

---

## 11. 운영 체크리스트 요약

### 매일 공통
- **오픈**: 환기 (장기밀매 창문 윗 고리 열기, 계단/문/출입문 30분 환기)
- **마감**: 청소 + 사진 전송
- **5:30~6:00**: 모든 테마 물건닦기, 비품체크, 건전지 교체, 장치 작동확인

### 요일별 특수
| 요일 | 오픈 | 마감 |
|------|------|------|
| 월 | 알코올 물건닦기 | 청소: 신데, 타타 |
| 화 | 장치 확인 후 보고, 낙서 확인 | 청소: 장기, 로비 |
| 수 | 알코올 물건닦기 | 청소: 카운터 |
| 목 | 장치 확인 후 보고, 낙서 확인 | 잔돈교체, 청소: 타워링, 로비 |
| 금 | 알코올 물건닦기 | 비품재고파악+단톡고지, 물통비우기, 스피커/배터리점검 |
| 토 | 스케줄 단톡에 올리기 | 청소 |
| 일 | 알코올 물건닦기 | 테마별 장치점검 (타워링/장기/타타/신데/집착) |

### 일요일 미들 (5:30~7:00)
- 손님 퇴장 후 10분안에 미들과 인터폰 점검

---

## 12. 배포 방법

### EXE 빌드
```cmd
dotnet publish src\CubeManager\CubeManager.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

### 배포
1. 대상 PC에 [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0/runtime) 설치 (1회)
2. `publish\CubeManager.exe` 복사 → 실행

### DB 이식
```
기존 PC: %APPDATA%\Roaming\CubeManager\cubemanager.db 복사
새 PC:   앱 1회 실행 후 종료 → 같은 경로에 덮어쓰기 → 재실행
```

### DB 초기화
- 관리자 탭 → "⚠ DB 초기화" 버튼 (비밀번호: rlawoqja)
- PowerShell로 앱 종료 → DB 삭제 → 자동 재시작

---

## 13. GitHub Pages

- URL: `https://devtigeer.github.io/CubeManager/user-manual.html`
- 소스: `docs/user-manual.html`
- 리포지토리: Public 전환 완료

---

## 14. 보안 고려사항

| 항목 | 상태 |
|------|------|
| 관리자 비밀번호 | BCrypt 해시 저장 (workFactor: 12) |
| 웹 자격증명 | DPAPI 암호화 |
| SQL Injection | 파라미터 바인딩 필수 |
| DB 초기화 | 별도 비밀번호 (rlawoqja) + 2단계 확인 |
| GitHub Public | 코드에 평문 비밀번호/API키 없음 확인 완료 |

---

## 15. 알려진 제한사항

- Aptos 폰트: Windows 11에만 기본 설치 → Segoe UI 폴백
- MDL2 아이콘: macOS에서 미지원 → 이모지 폴백
- Dapper snake_case 매핑: `DefaultTypeMap.MatchNamesWithUnderscores = true` 필요
- FormBorderStyle.None: 모든 Dialog에 미리 설정 (핸들 재생성 방지)
