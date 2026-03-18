# 저사양 환경 구현 검토 및 DB 관리 방안

## 1. 대상 환경 가정

| 항목 | 스펙 |
|------|------|
| CPU | Intel i3 / Celeron 급 |
| RAM | 4GB |
| 저장장치 | HDD (SSD 아님) |
| OS | Windows 10/11 |
| 해상도 | 1280×768 이상 |
| 네트워크 | 유선/WiFi (간헐적 불안정 가능) |

---

## 2. 프레임워크별 저사양 적합성 비교

### 2.1 RAM 사용량 비교

```
                      유휴 RAM 사용량 (빈 앱 기준)
 ────────────────────────────────────────────────────
 Electron        ████████████████████████████████████  200~400 MB  ✗ 제외
 WPF (.NET 8)    ████████████████                       80~90 MB  △ 무거움
 Avalonia        ██████████████                         60~80 MB  △
 CustomTkinter   ██████                                 25~35 MB  ○
 Tauri           █████                                  20~40 MB  ◎
 Tkinter         ████                                   15~20 MB  ◎
 WinForms        ██                                      6~10 MB  ◎ 최경량
```

### 2.2 종합 평가표

| 프레임워크 | RAM | 콜드스타트 | 디스크 | UI 품질 | 개발생산성 | 저사양 적합도 |
|-----------|-----|-----------|--------|---------|-----------|-------------|
| **WinForms** | ◎ 6~10MB | ◎ <1초 | ○ 60MB | △ 구식 | ○ | **★★★★★** |
| **Tauri** | ◎ 20~40MB | ◎ <0.5초 | ◎ <10MB | ◎ 모던 | △ Rust | **★★★★☆** |
| **Python+CTk** | ○ 25~35MB | ○ ~1초 | ○ 40MB | ○ 깔끔 | ◎ 빠른개발 | **★★★★☆** |
| **WPF** | △ 80~90MB | ✗ 2~3초(HDD) | △ 150MB | ◎ 세련 | ○ | **★★☆☆☆** |
| **Electron** | ✗ 200MB+ | △ 1~2초 | ✗ 120MB+ | ◎ | ◎ | **★☆☆☆☆** |

---

## 3. 기술스택 재검토 및 권장안

### 3.1 기존 설계: WPF + .NET 8

**문제점 (4GB RAM + HDD 환경)**:
- 유휴 상태에서만 **80~90MB** → 실제 기능 로드 시 **150~250MB** 예상
- 4GB 중 OS가 ~1.5GB 사용 → 앱에 할당 가능한 건 ~2.5GB
- HDD에서 콜드 스타트 **2~3초** (JIT 컴파일 + 어셈블리 로딩)
- .NET 8 런타임 설치 필요 (자체포함 배포 시 디스크 150MB+)

### 3.2 권장안 A: WinForms + .NET 8 (가장 실용적)

```
적합 이유:
✓ 유휴 RAM 6~10MB → 전체 기능 로드해도 50~80MB 수준
✓ 콜드 스타트 1초 이내
✓ C# 그대로 사용 (기존 설계의 Service/Model 레이어 재활용)
✓ DataGridView로 테이블 기반 UI 구현에 최적
✓ 학습곡선 낮음, 레퍼런스 풍부

단점:
△ WPF 대비 UI가 투박함 → MetroFramework/MaterialSkin 라이브러리로 보완
△ 데이터바인딩 수동 → BindingSource로 일부 해결
```

### 3.3 권장안 B: Python + CustomTkinter (빠른 개발)

```
적합 이유:
✓ 유휴 RAM 25~35MB
✓ 빠른 프로토타이핑, 코드량 적음
✓ requests + BeautifulSoup으로 웹 스크래핑 간편
✓ tkcalendar, tktimepicker 등 위젯 풍부

단점:
△ GIL 문제 → 웹 스크래핑 중 UI 멈춤 가능 (threading 필요)
△ PyInstaller 배포 패키지 40~100MB + 백신 오탐 이슈
△ 복잡한 타임테이블 UI에서 성능 저하 가능
```

### 3.4 권장안 C: Tauri (모던 UI가 필요할 때)

```
적합 이유:
✓ 유휴 RAM 20~40MB, 설치파일 <10MB
✓ HTML/CSS로 UI → 타임테이블/테이블 구현 자유도 높음
✓ WebView2(Windows 내장) 사용, 별도 번들 불필요

단점:
△ Rust 백엔드 학습곡선 높음
△ WebView2 프로세스 분리로 실제 메모리 측정치보다 높을 수 있음
△ 디버깅 환경이 WPF/WinForms 대비 불편
```

### 3.5 최종 권장

```
┌────────────────────────────────────────────────────────┐
│  ★ 1순위 권장: WinForms + .NET 8                       │
│                                                        │
│  이유:                                                 │
│  1. 기존 설계(C#, Service 레이어)를 그대로 활용        │
│  2. 저사양에서 가장 안정적인 성능                      │
│  3. 테이블/그리드 기반 UI에 최적 (DataGridView)        │
│  4. WPF의 MVVM → MVP 또는 코드비하인드로 단순화       │
│  5. MetroFramework 적용 시 충분히 현대적인 UI          │
│                                                        │
│  변경 범위:                                            │
│  - View 레이어만 WPF → WinForms 교체                  │
│  - Service/Model/Data 레이어는 100% 동일               │
│  - XAML 바인딩 → DataGridView.DataSource 바인딩        │
└────────────────────────────────────────────────────────┘
```

---

## 4. 저사양 최적화 설계 변경

### 4.1 아키텍처 변경점

```
[기존 설계]                    [저사양 최적화 설계]
WPF + MVVM                →   WinForms + MVP (or CodeBehind)
EF Core ORM               →   Dapper (경량 ORM) 또는 Raw SQL
자동갱신 10분 타이머       →   수동 조회 기본, 자동은 선택
Markdig + WPF 렌더러      →   WebBrowser 컨트롤 + Markdig HTML
전체 데이터 메모리 로드    →   페이징 + Lazy Loading
```

### 4.2 메모리 최적화 전략

| 영역 | 기존 | 최적화 |
|------|------|--------|
| 탭 로딩 | 모든 탭 동시 로드 | **지연 로딩** (탭 클릭 시 생성) |
| 예약 데이터 | 전체 캐시 | **당일 데이터만** 메모리 유지 |
| 스케줄 | 월간 전체 | **현재 주차만** 로드 |
| 급여 | 전직원 전체 | **현재 월만** 로드 |
| 인수인계 | 전체 | **10건씩 페이징** |
| MD 렌더링 | 항상 렌더링 | **선택 시만** 렌더링 |

### 4.3 예상 메모리 사용량 (WinForms 기준)

```
구성요소별 RAM 추정:
───────────────────────────────
WinForms 프레임워크       ~10 MB
활성 탭 UI                ~5~10 MB
SQLite 연결 + 캐시        ~10 MB
현재 조회 데이터          ~5~15 MB
HTTP 클라이언트           ~3 MB
MD 렌더링 (WebBrowser)    ~15~20 MB (필요 시만)
───────────────────────────────
합계                     ~50~70 MB (최대)
───────────────────────────────

비교:
WPF 예상    : 150~250 MB
WinForms 예상: 50~70 MB  ← 3~4배 절약
```

---

## 5. DB 관리 방안 (SQLite 저사양 최적화)

### 5.1 핵심 PRAGMA 설정

```sql
-- 앱 시작 시 1회 실행
PRAGMA journal_mode = WAL;         -- HDD에서 필수: 랜덤 쓰기 → 순차 쓰기
PRAGMA synchronous = NORMAL;       -- WAL 모드에서 안전하면서 빠름
PRAGMA temp_store = MEMORY;        -- 임시 테이블 메모리에 (HDD I/O 절약)
PRAGMA mmap_size = 67108864;       -- 64MB 메모리 맵 (4GB RAM에서 적절)
PRAGMA cache_size = -8000;         -- 8MB 페이지 캐시
PRAGMA page_size = 4096;           -- OS 페이지 크기와 일치
PRAGMA foreign_keys = ON;          -- FK 제약 활성화
```

### 5.2 WAL 모드가 중요한 이유 (HDD 환경)

```
[기본 DELETE 모드 - HDD에서 느림]
쓰기 → 저널 파일 쓰기 → fsync → DB 쓰기 → fsync → 저널 삭제
       (랜덤 I/O 3~4회)

[WAL 모드 - HDD에서 빠름]
쓰기 → WAL 파일에 순차 추가 → 체크포인트(나중에 일괄)
       (순차 I/O 1회)

성능 차이: 개별 INSERT 기준 10~50배 빠름
```

### 5.3 트랜잭션 배치 처리

```
❌ 나쁜 예 (HDD에서 치명적):
for each item in list:
    INSERT INTO sale_items VALUES (...);
→ 건당 트랜잭션 = 건당 fsync = HDD에서 초당 ~50건

✅ 좋은 예:
BEGIN TRANSACTION;
for each item in list:
    INSERT INTO sale_items VALUES (...);
COMMIT;
→ 1회 트랜잭션 = 1회 fsync = 수천 건도 1초 이내
```

### 5.4 인덱스 전략

```sql
-- 필수 인덱스 (조회 빈도 높은 컬럼)
CREATE INDEX idx_schedules_date ON schedules(work_date);
CREATE INDEX idx_schedules_emp_date ON schedules(employee_id, work_date);
CREATE INDEX idx_attendance_emp_date ON attendance(employee_id, work_date);
CREATE INDEX idx_reservations_date ON reservations(reservation_date);
CREATE INDEX idx_sale_items_daily ON sale_items(daily_sales_id);
CREATE INDEX idx_handovers_created ON handovers(created_at DESC);
CREATE INDEX idx_holidays_date ON holidays(holiday_date);

-- 불필요한 인덱스는 만들지 않음 (쓰기 성능 저하)
-- inventory, app_config 등 소량 테이블은 인덱스 불필요
```

### 5.5 DB 파일 관리

| 항목 | 방안 |
|------|------|
| **DB 파일 위치** | `%APPDATA%/CubeManager/cubemanager.db` |
| **백업 주기** | 앱 종료 시 자동 백업 (이전 3일치 보관) |
| **백업 방식** | SQLite Online Backup API (락 없이 복사) |
| **VACUUM** | 월 1회 자동 (DB 파편화 해소) |
| **WAL 체크포인트** | 앱 종료 시 `PRAGMA wal_checkpoint(TRUNCATE)` |
| **최대 DB 크기 예상** | ~10~50MB (1년 운영 기준) |

### 5.6 데이터 보관 정책

```
┌─────────────────────────────────────────────────────────┐
│  데이터 유형        │ 보관 기간    │ 이후 처리          │
├─────────────────────┼──────────────┼────────────────────┤
│  예약 데이터        │ 6개월        │ 아카이브 DB로 이동 │
│  매출 데이터        │ 1년          │ 아카이브 DB로 이동 │
│  스케줄             │ 6개월        │ 삭제               │
│  출퇴근 기록        │ 1년          │ 아카이브 DB로 이동 │
│  급여 기록          │ 3년          │ 영구 보관          │
│  인수인계           │ 1년          │ 삭제               │
│  물품 관리          │ 현재 상태만  │ 이력 불필요        │
│  공휴일 캐시        │ 2년          │ 갱신               │
└─────────────────────┴──────────────┴────────────────────┘

아카이브: cubemanager_archive_2026.db (별도 파일)
→ DB 크기를 항상 50MB 이하로 유지
```

### 5.7 DB 크기 추정

```
테이블별 연간 예상 레코드 수 및 크기:

reservations:  365일 × 20건/일     = ~7,300건    ~1.5MB
sale_items:    365일 × 15건/일     = ~5,475건    ~1.0MB
schedules:     365일 × 5명         = ~1,825건    ~0.3MB
attendance:    365일 × 5명         = ~1,825건    ~0.3MB
salary_records: 12개월 × 5명       =     ~60건   ~0.01MB
handovers:     ~500건/년           =    ~500건    ~0.2MB
comments:      ~1,500건/년         =  ~1,500건   ~0.3MB
inventory:     ~50건 (고정)        =     ~50건   ~0.01MB
holidays:      ~20건/년            =     ~20건   ~0.01MB
─────────────────────────────────────────────────────────
합계: ~18,555건/년    약 3.6MB/년

WAL 파일: ~1~5MB (운영 중)
인덱스: ~1MB
─────────────────────────────────────────────────────────
총 DB 크기 (1년): ~6~10MB → 매우 가벼움
```

---

## 6. 저사양 대응 추가 설계

### 6.1 비동기 처리 (UI 멈춤 방지)

```csharp
// 웹 스크래핑을 비동기로 처리 (UI 스레드 블로킹 방지)
private async void BtnFetch_Click(object sender, EventArgs e)
{
    btnFetch.Enabled = false;
    statusLabel.Text = "예약 데이터 조회 중...";

    var data = await Task.Run(() =>
        reservationService.FetchReservations(selectedDate));

    dataGridView.DataSource = data;
    btnFetch.Enabled = true;
}
```

### 6.2 지연 로딩 (탭)

```
앱 시작 시:
- 메인 폼 + 탭 컨테이너만 로드 (~10MB)
- 각 탭 UserControl은 처음 클릭 시 생성 (Lazy)
- 다른 탭 전환 시 이전 탭의 대용량 데이터 해제

효과: 초기 로딩 시간 50% 단축, 피크 메모리 30% 절감
```

### 6.3 이미지/리소스 최적화

```
- 아이콘: SVG 대신 16×16, 24×24 PNG (용량 절약)
- 폰트: 시스템 폰트(맑은 고딕) 사용, 커스텀 폰트 번들 안 함
- 스플래시 스크린: 없음 (빠른 시작 우선)
```

### 6.4 웹 스크래핑 최적화

```
기존: 10분마다 자동 갱신 → HTTP 연결 유지 부담
변경:
- 기본: 수동 조회 (버튼 클릭)
- 선택: 자동 갱신 ON/OFF (설정에서)
- 자동 갱신 시에도 30분 간격으로 완화
- 연결 타임아웃: 10초 (저속 네트워크 대응)
- 실패 시 자동 재시도 없음 (수동 재시도)
```

---

## 7. 변경 사항 요약 (기존 설계 대비)

| 항목 | 기존 설계 | 저사양 최적화 |
|------|----------|-------------|
| UI 프레임워크 | WPF | **WinForms** (+MetroFramework) |
| 아키텍처 패턴 | MVVM | **MVP 또는 CodeBehind** |
| ORM | EF Core | **Dapper** (경량) |
| 예상 RAM | 150~250MB | **50~70MB** |
| 콜드 스타트 | 2~3초 | **<1초** |
| DB journal | 기본(DELETE) | **WAL 모드** |
| DB 캐시 | 기본 | **8MB + 64MB mmap** |
| 탭 로딩 | 동시 로드 | **지연 로딩(Lazy)** |
| 자동 갱신 | 10분 | **수동 기본 / 30분 선택** |
| 데이터 로딩 | 전체 | **페이징 + 현재 범위만** |
| MD 렌더링 | 전용 렌더러 | **WebBrowser + HTML 변환** |
| 배포 크기 | ~150MB | **~60~70MB** |

---

## 8. 리스크 및 대응

| 리스크 | 영향 | 대응 |
|--------|------|------|
| HDD에서 DB 쓰기 지연 | 매출 입력 시 느림 | WAL + 트랜잭션 배치 |
| 4GB RAM 부족 | 앱 스왑 발생 | 지연 로딩 + 데이터 해제 |
| WebBrowser 컨트롤 무거움 | MD 탭에서 메모리 증가 | 탭 전환 시 해제, 필요 시만 로드 |
| 웹 스크래핑 타임아웃 | 예약 조회 실패 | 캐시 데이터 표시 + 재시도 안내 |
| 안티바이러스 오탐 | 배포 시 차단 | 코드서명 인증서 적용 |
| DB 파일 손상 | 데이터 유실 | 자동 백업(3일치) + 복원 기능 |
