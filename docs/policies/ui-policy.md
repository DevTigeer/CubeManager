# UI 정책

## 1. 색상 체계

### 1.1 기본 색상

| 이름 | HEX | 용도 |
|------|-----|------|
| Primary | `#1976D2` | 탭 선택, 주요 버튼, 활성 상태 |
| Success | `#4CAF50` | 정상 상태, 완료, 현금 결제 |
| Warning | `#FFC107` | 주의, 계좌이체 |
| Danger | `#F44336` | 지각/조퇴, 에러, 지출, 부족 |
| Info | `#2196F3` | 카드결제, 정보 안내 |
| Background | `#FAFAFA` | 메인 배경 |
| Card | `#FFFFFF` | 콘텐츠 카드/패널 배경 |
| Border | `#E0E0E0` | 테이블/카드 테두리 |
| Text | `#212121` | 주요 텍스트 |
| TextSecondary | `#757575` | 보조 텍스트, 힌트 |
| ManualEdit | `#FFF9C4` | 수기 수정 셀 배경 |

### 1.2 결제 태그 색상

| 결제 수단 | 배경색 | 글자색 | 표시 |
|-----------|--------|--------|------|
| 카드 | `#E3F2FD` | `#1565C0` | 카드 |
| 현금 | `#E8F5E9` | `#2E7D32` | 현금 |
| 계좌이체 | `#FFF8E1` | `#F57F17` | 계좌 |
| 지출 | `#FFEBEE` | `#C62828` | 지출 |

### 1.3 출퇴근 상태 색상

| 상태 | 글자색 | 의미 |
|------|--------|------|
| 정상 (on_time) | `#1565C0` (파란) | 출근 시 예정시간 이전, 퇴근 시 예정시간 이후 |
| 지각/조퇴 | `#C62828` (빨간) | 출근 시 예정시간 이후, 퇴근 시 예정시간 이전 |
| 미기록 | `#9E9E9E` (회색) | 아직 출/퇴근 미기록 |

### 1.4 직원 색상 팔레트 (타임테이블용)

```csharp
// ColorPalette.cs
public static readonly Color[] EmployeeColors = {
    Color.FromArgb(187, 222, 251),  // #BBDEFB 연파랑
    Color.FromArgb(200, 230, 201),  // #C8E6C9 연초록
    Color.FromArgb(248, 187, 208),  // #F8BBD0 연분홍
    Color.FromArgb(255, 224, 178),  // #FFE0B2 연주황
    Color.FromArgb(225, 190, 231),  // #E1BEE7 연보라
    Color.FromArgb(178, 235, 242),  // #B2EBF2 연하늘
    Color.FromArgb(255, 245, 157),  // #FFF59D 연노랑
    Color.FromArgb(188, 170, 164),  // #BCAAA4 연갈색
};
```

직원 인덱스를 배열 길이로 나눈 나머지로 색상 할당.

---

## 2. 폰트

```
기본 폰트: 맑은 고딕 (Malgun Gothic) — 시스템 폰트, 번들 불필요
기본 크기: 13px
제목: 16px Bold
소제목: 14px SemiBold
본문: 13px Regular
보조: 11px Regular (타임스탬프, 힌트)
```

커스텀 폰트 번들 금지 (배포 크기 절약).

---

## 3. 컨트롤 사용 규칙

### 3.1 테이블/그리드

| 용도 | 컨트롤 |
|------|--------|
| 스케줄 타임테이블 | **커스텀 Panel (GDI+)** — 블록형 렌더링 |
| 예약 테이블 | DataGridView (읽기전용) |
| 급여 테이블 | DataGridView (편집 모드 시 셀 편집) |
| 매출 항목 | DataGridView |
| 직원 목록 | DataGridView |
| 물품 관리 | DataGridView (현재수량 셀 편집) |
| 출퇴근 현황 | DataGridView (읽기전용) |
| 출퇴근 이력 | DataGridView (읽기전용) |

### 3.2 커스텀 Panel 규칙

```csharp
// 반드시 더블 버퍼링 활성화
public TimeTablePanel()
{
    DoubleBuffered = true;
    SetStyle(ControlStyles.AllPaintingInWmPaint |
             ControlStyles.UserPaint |
             ControlStyles.OptimizedDoubleBuffer, true);
}
```

- OnPaint에서만 그리기 (Invalidate → Paint 사이클)
- 직접 Graphics 객체 생성 금지
- 리사이즈 시 Invalidate() 호출

### 3.3 입력 컨트롤

- 날짜: AntdUI DatePicker 또는 DateTimePicker
- 시간: ComboBox (30분 간격 드롭다운) `10:00, 10:30, 11:00, ...`
- 금액: TextBox + 숫자만 입력 필터 + 천 단위 포맷 표시
- 체크박스: AntdUI Checkbox

---

## 4. 레이아웃 규칙

### 4.1 탭 구조

```
메인 폼 (MainForm)
└── TabControl (AntdUI Tabs)
    ├── 예약/매출 (ReservationSalesTab)
    ├── 스케줄 (ScheduleTab)
    ├── 급여 (SalaryTab)
    ├── 업무자료 (DocumentTab)
    ├── 인수인계 (HandoverTab)
    ├── 물품 (InventoryTab)
    ├── 출퇴근 (AttendanceTab)
    └── 설정 (SettingsTab)
```

### 4.2 지연 로딩

```csharp
// 탭 클릭 시 최초 1회만 UserControl 생성
private readonly Dictionary<int, UserControl> _tabCache = new();

private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
{
    var index = tabControl.SelectedIndex;
    if (!_tabCache.ContainsKey(index))
    {
        _tabCache[index] = CreateTabContent(index);
        tabPages[index].Controls.Add(_tabCache[index]);
    }
}
```

### 4.3 최소/권장 해상도

```
최소: 1280 × 768
권장: 1920 × 1080
DPI: 기본 100% 기준, High DPI aware 설정 필요
```

---

## 5. 다이얼로그

### 5.1 관리자 인증

```
모달 다이얼로그
비밀번호 입력 + 확인/취소
인증 성공: DialogResult.OK
인증 실패: 메시지 표시 + 재입력
인증 캐시: 5분
```

### 5.2 확인/취소

```
삭제, 덮어쓰기 등 파괴적 작업 전 반드시 확인 다이얼로그
기본 포커스: [취소] 버튼 (실수 방지)
```

### 5.3 토스트 알림

```
위치: 화면 하단 우측
지속: 3초 후 자동 사라짐
종류:
  Success (초록) — 성공
  Warning (노랑) — 경고, 검증 실패
  Error   (빨강) — 오류
겹침: 여러 개 동시 표시 시 위로 쌓임
```

---

## 6. 아이콘/리소스

```
형식: PNG (16×16, 24×24)
SVG 사용 금지 (WinForms 네이티브 미지원)
시스템 아이콘 우선 사용 (SystemIcons)
커스텀 아이콘은 resources/icons/ 에 저장
스플래시 스크린 없음 (빠른 시작 우선)
```
