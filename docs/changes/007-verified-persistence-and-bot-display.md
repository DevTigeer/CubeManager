# 변경 기록 #007: 정산체크 DB 영속화 + 텔레그램 봇 명령 한글 표시

> 작성일: 2026-05-10
> 상태: 완료
> 적용 버전: v0.3.19

---

## 변경 요약

| # | 변경 | 설명 |
|---|------|------|
| 1 | **예약 정산체크 DB 영속화** | `reservations.is_verified` 컬럼 추가(V026). 체크 즉시 저장, 웹조회 후에도 유지. |
| 2 | **텔레그램 `/명령어` 출력 한글 표시** | `HelpCommandHandler` 가 한글 alias 가 있으면 그걸 우선 표시. 영어 명령은 그대로 동작. |

---

## 1. 예약 정산체크 DB 영속화

### 증상

예약/매출 탭의 "정산체크" 체크박스를 클릭해도 **웹조회 후 다시 사라짐**. 다른 날짜 갔다가 돌아와도 사라짐.

### 원인

`Verified` 컬럼은 `DataGridViewCheckBoxColumn` 으로 정의만 되어 있고 **DB 와 미연결**.

- 클릭 시 저장 핸들러 없음
- `PopulateMainGrid` 에서 셀에 값을 채우지 않음 → 매번 `null` (uncheck) 로 그려짐
- `_gridMain.Rows.Clear()` 후 재생성되면 UI 상태 휘발

`SaleItem.IsVerified` / `sale_items.is_verified` (V012) 가 모델·DB 에 있긴 했지만 사용처가 없었고, 한 예약에 카드/현금/계좌가 섞이면 단일 체크박스로 표현이 모호하여 **예약 단위로 통일**.

### 수정

**1) V026 마이그레이션**

```sql
ALTER TABLE reservations ADD COLUMN is_verified INTEGER NOT NULL DEFAULT 0
```

**2) 모델/Repository**

- `Reservation.IsVerified : bool` 프로퍼티 추가
- `IReservationRepository.UpdateVerifiedAsync(int id, bool isVerified)` 인터페이스 추가
- `GetByDateAsync` / `GetByIdAsync` SELECT 절에 `is_verified` 포함
- `UpsertAsync` 의 UPDATE 절은 **기존 패턴대로 status/note/is_verified 미변경** — 웹조회 upsert 가 사용자 수기 입력을 덮어쓰지 않도록

**3) UI 이벤트 배선 (`ReservationSalesTab.cs`)**

체크박스 셀은 클릭 시 `IsCurrentCellDirty` 만 즉시 set 되고 `Value` 는 `EndEdit` 까지 commit 되지 않아 `CellValueChanged` 가 늦게 발화한다. 표준 패턴:

```csharp
_gridMain.CurrentCellDirtyStateChanged += (_, _) =>
{
    if (_gridMain.CurrentCell is DataGridViewCheckBoxCell && _gridMain.IsCurrentCellDirty)
        _gridMain.CommitEdit(DataGridViewDataErrorContexts.Commit);
};
_gridMain.CellValueChanged += async (_, e) =>
{
    if (_isPopulating) return;
    if (_gridMain.Columns[e.ColumnIndex].Name == "Verified")
        await SaveReservationVerifiedAsync(e.RowIndex);
};
```

**4) `_isPopulating` 가드**

`PopulateMainGrid` 가 셀에 `r.IsVerified` 를 세팅하면 `CellValueChanged` 가 발화 → `SaveReservationVerifiedAsync` 가 N 번 호출되는 saved-back 루프 발생. `try/finally` 로 플래그 둘러싸 그동안 발화 무시.

```csharp
private void PopulateMainGrid()
{
    _isPopulating = true;
    try { PopulateMainGridCore(); }
    finally { _isPopulating = false; }
}
```

### 영향

- 새 사용자: V026 자동 적용, 모든 기존 예약 `is_verified = 0`
- 기존 사용자: 컬럼 추가만, 데이터 손실 없음
- 웹조회 후 정산체크 유지 ✅

---

## 2. 텔레그램 `/명령어` 출력 한글 표시

### 증상

`/명령어` 입력 시 출력되는 명령 목록이 모두 영어로 표시 (`/sales`, `/schedule`, `/help` …) — 한국어 환경에서 인식성 낮음.

### 원인

`HelpCommandHandler.HandleAsync` 가 `h.Command`(영어 정식명) 만 표시. `Aliases` 에 등록된 한글명(`/매출`, `/스케줄` 등) 은 라우팅만 되고 표시되지 않음.

### 수정

`src/CubeManager.Telegram/Commands/HelpCommandHandler.cs`

```csharp
private static string DisplayName(ICommandHandler h)
    => h.Aliases.FirstOrDefault(ContainsHangul) ?? h.Command;

private static bool ContainsHangul(string s)
{
    foreach (var c in s)
        if (c >= 0xAC00 && c <= 0xD7A3) return true;
    return false;
}
```

`HandleAsync` 에서 `h.Command` 대신 `DisplayName(h)` 사용. 정렬 키도 동일.

### 영향

- 출력 예시: `/급여`, `/대시보드`, `/도움말`, `/스케줄`, `/오늘매출`, `/이번달매출`, `/체크리스트`, `/출퇴근`, `/출퇴근관리`, `/핑`
- 영어 명령(`/sales`, `/help` 등) 입력 → 그대로 동작 (alias 등록 자체는 변경 없음)
- 핸들러 추가 시 `Aliases` 첫 한글 항목이 자동으로 표시되므로 추가 작업 불필요
