# 변경 기록 #005: 현금잔액 산식 정정 + 관리자 현금 보정 입력 버그

> 작성일: 2026-05-10
> 상태: 완료
> 적용 버전: v0.3.15 ~ v0.3.17

---

## 변경 요약

| # | 버전 | 변경 | 설명 |
|---|------|------|------|
| 1 | v0.3.15 | **현금잔액 carry-forward + 미래 행 연쇄 재계산** | 전일 잔액 조회를 "직전 가장 최근 행"으로 변경(거래 없는 날 자동 건너뛰기). 과거일 수정 시 미래 cash_balance 행을 트랜잭션 안에서 모두 cascade 재계산. `GetEffectiveCashBalanceAsync` 추가(조회 시 행이 없어도 합성 잔액 반환). V024 마이그레이션으로 기존 행 일괄 보정. |
| 2 | v0.3.16 | **현금잔액 추적 시작일을 2026-05-02로 클램프** | 기준일 이전 cash_balance 행 삭제. 기준일 행의 `opening_balance`는 DB 저장값을 그대로 보존(어떤 재계산 경로에서도 덮어쓰지 않음). 그 이후 날짜는 기준일 이상 범위에서만 carry-forward. V025 마이그레이션으로 기존 데이터 정렬. |
| 3 | v0.3.17 | **관리자 현금 보정 — 숫자 입력 안 되는 이슈 수정** | `_numCashAmount`에 `ImeMode = ImeMode.Disable` 명시(한글 IME 차단), `_txtCashNote`에 `ImeMode.Hangul`. 클릭 핸들러는 `Value` 대신 `Text`를 콤마 제거 후 직접 파싱(미커밋 레이스 회피) + Min/Max 클램프. |

---

## v0.3.15 — 현금잔액 carry-forward + cascade 상세

### 증상

- 5/1 현금매출 200,000 (closing=200,000) → 5/2 거래 없음 → 5/3 현금매출 50,000을 기록하면 5/3 closing이 250,000이어야 하는데 **50,000으로 표시**됨.
- 거래 없는 날을 화면에서 열면 현금잔액이 **₩ 0**으로 표시됨(누적 잔액 무시).
- 과거일 sale_item을 수정/삭제해도 그 이후 날짜의 잔액이 자동 갱신되지 않음(stale).

### 원인

`SalesRepository.UpdateCashBalanceAsync`가 전일 잔액을 **`balance_date = (date - 1일)`** 로 정확히 매칭하여 조회. 거래 없는 날엔 `cash_balance` 행 자체가 생성되지 않으므로 다음날 조회 시 NULL → 0으로 떨어져 누적이 끊김.

조회 시점에서도 `GetCashBalanceAsync`가 단순히 저장된 행만 반환했기 때문에 행 없는 날은 ₩ 0으로 표시.

또한 `UpdateCashBalanceAsync(date)` 호출 시 해당 일자만 갱신하고 그 이후 날짜는 손대지 않아, 과거일 수정 분이 미래 잔액에 반영되지 않음.

### 수정

**1) 전일 잔액 조회 — carry-forward**

```sql
-- Before
SELECT closing_balance FROM cash_balance WHERE balance_date = @prevDate

-- After
SELECT closing_balance FROM cash_balance
 WHERE balance_date < @date
 ORDER BY balance_date DESC LIMIT 1
```

거래 없는 날은 행이 없으므로 자연스럽게 건너뛰고 마지막 거래일의 closing이 carry-forward 됨.

**2) 미래 행 연쇄 재계산 (cascade)**

```csharp
public async Task UpdateCashBalanceAsync(string date)
{
    using var conn = _db.CreateConnection();
    using var tx = conn.BeginTransaction();

    await RecomputeOneAsync(conn, tx, date);

    var futureDates = (await conn.QueryAsync<string>(
        "SELECT balance_date FROM cash_balance WHERE balance_date > @date ORDER BY balance_date ASC",
        new { date }, transaction: tx)).ToList();

    foreach (var d in futureDates)
        await RecomputeOneAsync(conn, tx, d);

    tx.Commit();
}
```

`RecomputeOneAsync`는 `(prevClosing + cashIn - cashOut)` 산식으로 단일 일자 UPSERT.

**3) 조회 전용 합성 메서드 — `GetEffectiveCashBalanceAsync`**

행이 있으면 그대로 반환, 없으면 직전 carry-forward + 당일 sale_items 합으로 합성된 `CashBalance` 객체를 만들어 반환(DB 쓰기 없음).

`ReservationSalesTab.LoadSummaryAsync`는 이 메서드를 사용 → 거래 없는 날에도 정상적으로 누적 잔액과 `(전일 X + Y - Z)` 상세가 표시됨. "어제 잔돈" 라벨도 별도 조회 제거하고 `balance.OpeningBalance` 재사용.

**4) V024 마이그레이션**

기존 cash_balance 행을 날짜 오름차순 순회하며 `prevClosing = 0`에서 시작해 모두 재계산. 0으로 끊겨 저장된 과거 잔액을 한 번에 보정.

> ⚠️ 첫 실행 시 화면 잔액이 점프할 수 있음 — 누적 끊김으로 잘못 저장돼 있던 과거 행이 정상값으로 갱신되기 때문. 사용자에게 사전 안내 권장.

---

## v0.3.16 — 2026-05-02 기준일 클램프 상세

### 배경

V024가 모든 cash_balance를 `prevClosing=0`에서 시작해 재계산했지만, 운영상 "특정 일자 이전의 누적은 의미 없음, 그 날의 시재(opening)부터 새로 트래킹하고 싶다"는 요구가 있었음. 기준일은 **2026-05-02**, 시작 `opening_balance`는 **DB에 이미 저장된 값을 그대로 보존**.

### 동작 정의

- **date < 2026-05-02**: cash_balance 추적 대상 아님. 행이 있어도 무시(V025가 일괄 삭제). `GetEffectiveCashBalanceAsync`는 `opening=0`인 합성값 반환.
- **date == 2026-05-02 (기준일)**: `opening_balance`는 DB에 저장된 값을 보존. 어떤 재계산 경로(`RecomputeOneAsync`, V025 등)에서도 덮어쓰지 않음. `closing = opening + cashIn - cashOut`만 갱신.
- **date > 2026-05-02**: 직전 carry-forward(기준일 이상 범위에서 가장 최근 마감액)로 opening 계산. 정상 cascade 적용.

### 코드 위치

```csharp
// src/CubeManager.Data/Repositories/SalesRepository.cs
private const string CashRefDate = "2026-05-02";
```

> 운영상 "임시" 클램프로 도입. 향후 운영 정책이 바뀌면 이 상수와 V025 마이그레이션을 함께 수정/제거하면 됨. 상수가 코드에 박혀 있으니 변경 시 전수 검색 필요(`CashRefDate`).

### V025 마이그레이션

1. `DELETE FROM cash_balance WHERE balance_date < '2026-05-02'`
2. `balance_date >= '2026-05-02'` 행을 오름차순으로 순회
   - 기준일 본인은 `opening_balance`를 그대로 읽어 보존
   - 그 이후는 직전 closing을 prevClosing으로 사용
   - `cash_in / cash_out`은 sale_items에서 재집계, `closing`은 산식대로
3. `sale_items`, `daily_sales`는 손대지 않음(트랜잭션 이력 보존)

---

## v0.3.17 — 관리자 현금 보정 NumericUpDown 입력 안 되는 이슈

### 증상

관리자 탭 "현금 잔액 수기 보정" 섹션의 `_numCashAmount`(NumericUpDown)에 키보드로 숫자가 입력되지 않음. 또는 입력 직후 "보정 적용"을 누르면 "보정 금액을 입력하세요" 경고가 뜸.

### 원인 (한국어 환경 흔한 WinForms 버그 두 가지가 겹침)

1. **IME 컴포지션 버퍼 인터셉트** — `NumericUpDown`은 부모로부터 IME 모드를 상속한다. 부모가 한글 IME가 켜진 상태면 숫자 키 입력이 IME 컴포지션 버퍼로 들어가 화면에 표시되지 않음.
2. **`Value` 미커밋 레이스** — `NumericUpDown.Value`는 LostFocus / Enter 시점에 ParseEditText로 텍스트→Value 커밋. 입력 직후 빠르게 버튼을 클릭하면 클릭 핸들러가 실행되는 시점에 Value가 아직 0인 상태로 읽힐 수 있음.

### 수정 (`AdminTab.cs`)

```csharp
// 1) IME 차단 — 숫자 컨트롤은 항상 ImeMode.Disable
_numCashAmount = new NumericUpDown
{
    Minimum = -10_000_000, Maximum = 100_000_000,
    Increment = 1000, ThousandsSeparator = true,
    ImeMode = ImeMode.Disable
};

// 2) 한글 입력 명시 — 보정 사유 TextBox
_txtCashNote = new TextBox
{
    PlaceholderText = "보정 사유 (예: 잔돈 오차)",
    ImeMode = ImeMode.Hangul
};

// 3) Value 미커밋 레이스 회피 — Text 직접 파싱
private async void BtnApplyCash_Click(object? sender, EventArgs e)
{
    var raw = _numCashAmount.Text?.Replace(",", "").Replace(" ", "") ?? string.Empty;
    if (!int.TryParse(raw, out var amount))
        amount = (int)_numCashAmount.Value;       // 폴백
    amount = Math.Clamp(amount,
        (int)_numCashAmount.Minimum,
        (int)_numCashAmount.Maximum);
    if (amount == 0) { /* 경고 */ return; }
    // ...
}
```

---

## 재사용 가능한 규칙 (다른 화면에도 적용 권장)

> 본 작업으로 검증된 패턴. 새 화면 작성 시 같은 함정을 피하기 위한 체크리스트.

### 1. Korean WinForms ImeMode 기본값

| 컨트롤 용도 | ImeMode |
|---|---|
| 한글 텍스트 입력(이름, 메모) | `ImeMode.Hangul` |
| 영문/숫자만(전화번호, 코드) | `ImeMode.Alpha` |
| **NumericUpDown / 숫자 전용** | **`ImeMode.Disable`** |

부모 폼/패널의 IME 모드에 의존하지 말고 입력 컨트롤마다 명시할 것. 명시하지 않으면 한글 IME 활성 시 숫자 입력이 막혀 사용자에게 "입력이 안 됨"으로 보임.

### 2. NumericUpDown의 Value는 미커밋 상태일 수 있다

LostFocus/Enter 시점에만 텍스트→Value 커밋이 일어나므로, 입력 직후 즉시 클릭/탭 전환 등 빠른 액션이 일어나는 화면에서는 `Value` 대신 `Text`를 직접 파싱하는 편이 안전하다.

```csharp
var raw = num.Text?.Replace(",", "") ?? "";
if (!int.TryParse(raw, out var v)) v = (int)num.Value;
v = Math.Clamp(v, (int)num.Minimum, (int)num.Maximum);
```

### 3. 누적 잔액형 데이터의 stale 방지 패턴

특정 일자의 값이 이전 일자에 의존하는 누적 데이터(현금잔액 등)는:

- **carry-forward 조회**: 직전 행이 비어있어도 더 과거의 가장 최근 행을 가져오도록 `WHERE date < @d ORDER BY DESC LIMIT 1` 패턴 사용.
- **cascade 갱신**: 과거일을 수정했을 때 해당 일자만이 아니라 그 이후의 모든 누적 행을 트랜잭션 안에서 재계산.
- **조회 전용 합성**: 행이 없어도 산식으로 합성된 값을 반환하는 read-only 메서드를 별도로 두고 UI는 이쪽을 사용. 쓰기는 명시적 트리거에서만.

---

## 관련 파일

- `src/CubeManager.Data/Repositories/SalesRepository.cs` — carry-forward, cascade, `GetEffectiveCashBalanceAsync`, `CashRefDate` 상수
- `src/CubeManager.Core/Interfaces/Repositories/ISalesRepository.cs`
- `src/CubeManager.Core/Interfaces/Services/ISalesService.cs`
- `src/CubeManager.Core/Services/SalesService.cs`
- `src/CubeManager/Forms/ReservationSalesTab.cs` — `LoadSummaryAsync`가 effective 메서드 사용
- `src/CubeManager/Forms/AdminTab.cs` — `_numCashAmount` IME, `BtnApplyCash_Click` Text 파싱
- `src/CubeManager.Data/Migrations/V024_RecomputeCashBalance.cs`
- `src/CubeManager.Data/Migrations/V025_CashRefDateReset.cs`
