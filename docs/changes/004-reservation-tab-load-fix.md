# 변경 기록 #004: 예약/매출 탭 — 초기 로드 race fix + 결제요약 정정

> 작성일: 2026-05-05
> 상태: 완료
> 적용 버전: v0.3.10 ~ v0.3.13

---

## 변경 요약

| # | 버전 | 변경 | 설명 |
|---|------|------|------|
| 1 | v0.3.10 | Telegram 이미지 리포트 8종 | `/sales`, `/month`, `/schedule`, `/checklist`, `/attendance`, `/dashboard`, `/salary`, `/attendance_admin` 추가. 점주 DM 전용 명령 게이팅 (`IOwnerOnlyCommand` 마커) |
| 2 | v0.3.11 | 결제요약 총매출 산식 정정 | `총매출 = 카드 + 현금 + 계좌` (지출 차감 X). 이전엔 `daily.TotalRevenue`(=수입-지출)를 그대로 썼던 것을 수입 합산으로 변경 |
| 3 | v0.3.11 | 결제요약 가로 폭 확대 | "(전일 X+Y-Z)" 라인 잘림 방지. `_lblSumBalanceDetail` 너비 260→330, 하단 패널 분할 40/30/30 → 36/28/36 |
| 4 | v0.3.12 | 지출 패널 초기 너비 0px 버그 | `bottomPanel.Resize` 핸들러를 `Controls.Add` 이전에 부착 + `HandleCreated`에서 1회 강제 사이징. 이전엔 핸들러가 늦게 붙어 초기 Resize가 누락되면 expensePanel/statsPanel이 `Width=1` 상태로 남았음 |
| 5 | v0.3.13 | **초기 로드 race fix (핵심)** | CTOR 시점 `_ = LoadAllAsync()` fire-and-forget이 `SyncContext` 미설치 + 핸들 미생성 상태에서 `BeginInvoke` 호출 → silent 예외 → 지출 그리드가 영영 비어있던 버그 수정 |

---

## v0.3.13 — 초기 로드 race fix 상세

### 증상

- 지출 추가 → 화면에 표시 → 정상
- 프로그램 종료 후 재실행 → 지출 그리드 비어있음
- DB에는 지출 데이터가 남아있음

### 원인

```
MainForm CTOR
  └─ LoadTab(0)
       └─ new ReservationSalesTab(...)
            └─ CTOR 끝부분 _ = LoadAllAsync()      ← (A) fire-and-forget
                 └─ await DB                        ← (B) SyncContext 캡처 시점
                      ├─ 캡처 결과: null (Application.Run 이전)
                      └─ continuation → ThreadPool
                           └─ PopulateMainGrid()
                                └─ BeginInvoke()    ← (C) 핸들 미생성 → throw
                                     └─ 예외가 (A) _ = 로 묻힘
                                          └─ LoadExpenseGridAsync 미실행
```

핵심 두 가지가 동시에 깨져 있었음:
1. **SyncContext 미설치**: `Application.Run()` 이전엔 `WindowsFormsSynchronizationContext.Current == null`. await continuation이 ThreadPool로 감.
2. **핸들 미생성**: `MainForm`이 아직 Show되지 않아 `_gridMain`을 비롯한 모든 컨트롤의 핸들이 없음. `BeginInvoke`가 `InvalidOperationException` 발생.

추가로 `_dtpDate.Value = DateTime.Today`가 `ValueChanged` 핸들러 부착 *이후*에 실행되어 CTOR 도중에도 `LoadAllAsync`가 한 번 더 호출되었고, 이 때는 `_gridMain` 자체가 null이라 `CommitCurrentGridEditAsync`에서 NRE까지 발생 (역시 silent).

### 수정

**1) `_dtpDate.Value` 설정 순서 교정**

```csharp
// Before
_dtpDate.ValueChanged += (_, _) => { _ = LoadAllAsync(); ... };
_dtpDate.Value = DateTime.Today;   // ← 핸들러를 발화시켜 CTOR 도중 LoadAllAsync 호출

// After
_dtpDate.Value = DateTime.Today;   // 먼저 값 세팅 → 발화 안 됨
_dtpDate.ValueChanged += (_, _) => { _ = SafeLoadAllAsync(); ... };
```

**2) 초기 로드를 `HandleCreated`로 지연 (1회성)**

```csharp
// Before
_ = LoadAllAsync();   // CTOR 시점

// After
EventHandler? initialLoad = null;
initialLoad = (_, _) =>
{
    HandleCreated -= initialLoad!;
    _ = SafeLoadAllAsync();
};
HandleCreated += initialLoad;
```

핸들이 생성된 시점은 `Application.Run` 후 `MainForm`이 Show되어 컨트롤들이 차례로 부모에 부착될 때 → SyncContext 설치 보장 + `BeginInvoke` 호출 가능.

**3) `SafeLoadAllAsync` wrapper — silent 매장 방지**

```csharp
private async Task SafeLoadAllAsync()
{
    try { await LoadAllAsync(); }
    catch (Exception ex)
    {
        Log.Error(ex, "ReservationSalesTab LoadAllAsync 실패 (date={Date})", _currentDate);
        try { ToastNotification.Show($"매출 로드 실패: {ex.Message}", ToastType.Error); }
        catch { }
    }
}
```

---

## v0.3.11 — 총매출 산식 정정

### 의도된 정의

| 라벨 | 산식 |
|------|------|
| 카드 | `daily_sales.card_amount` |
| 현금 | `daily_sales.cash_amount - 현금보정매출` |
| 계좌 | `daily_sales.transfer_amount` |
| **총매출** | **카드 + 현금 + 계좌** (지출 차감 X) |
| 총지출 | `sale_items WHERE category='expense' AND !cashCorrection` 합 |
| 현금잔액 | `cash_balance.closing_balance` (별도 산식) |

`daily_sales.total_revenue` 컬럼은 SQL 식 `SUM(CASE WHEN category='revenue' THEN amount ELSE -amount END)`로 지출까지 차감된 값 → 결제요약 표시에는 사용 금지.

---

## v0.3.12 — 패널 초기 너비 0px 버그

### 패턴 (재발 방지용)

WinForms에서 `Dock=Left + Width=1 + Resize 핸들러로 비율 사이징`은 위험하다. Resize 이벤트 부착이 `Controls.Add` 이후라면 초기 dock 레이아웃에서 핸들러가 발화하지 못해 1px 상태가 고정될 수 있음.

**규칙**:
- Resize 핸들러는 반드시 `Controls.Add` *이전*에 부착
- `HandleCreated` 시점에 1회 강제 호출로 안전망 확보
- 더 안전한 대안: `TableLayoutPanel` + ColumnStyle Percent

---

## 영향 범위

- 영향 코드: `src/CubeManager/Forms/ReservationSalesTab.cs`만 수정
- DB 스키마/마이그레이션 변경 없음
- 기존 사용자가 v0.3.11 이전에 등록한 지출 데이터는 그대로 보존, v0.3.13으로 업데이트하면 다시 표시됨
- 텔레그램 봇 명령(v0.3.10)은 별개 모듈 (`CubeManager.Telegram`)
