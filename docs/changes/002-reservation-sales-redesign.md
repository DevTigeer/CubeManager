# 변경 기록 #002: 예약/매출 탭 전면 개편

> 작성일: 2026-03-20
> 상태: 계획

---

## 변경 요약

| # | 변경 | 영향 범위 |
|---|------|----------|
| 1 | SideNavPanel 상시 표시 (hover → 고정) | SideNavPanel, MainForm |
| 2 | 웹 조회 2분 자동 갱신 | ReservationSalesTab |
| 3 | 시간순 정렬 버튼 | ReservationSalesTab |
| 4 | "방" → "테마" 용어 통일 | Model, Repository, Tab, 파서 |
| 5 | 예약 상태 전환 (confirmed → removed) | Model, Repository, Service, Tab |
| 6 | 예약+결제 통합 그리드 + 지출/요약 재배치 | ReservationSalesTab 전면 개편 |

---

## 1. SideNavPanel 상시 표시

### 현재 (AS-IS)

```
Collapsed (60px) ←→ Expanded (200px) on MouseEnter/Leave
아이콘만 보임 → 호버 시 텍스트 노출
```

### 변경 (TO-BE)

```
항상 200px 고정
아이콘 + 텍스트 상시 표시
MouseEnter/Leave 이벤트 제거
```

### 수정 파일

| 파일 | 변경 |
|------|------|
| `SideNavPanel.cs` | Width=200 고정, MouseEnter/Leave 제거, _isExpanded=true 고정 |
| `MainForm.cs` | SideNav 너비 상수 200으로 고정 |

---

## 2. 웹 조회 2분 자동 갱신

### 현재

- 수동 [웹 조회] 버튼 클릭 시에만 스크래핑

### 변경

```
System.Windows.Forms.Timer _autoRefreshTimer
  Interval = 120_000 (2분)
  Tick → FetchWebSilently()
    → 스크래핑 후 그리드 갱신
    → 변경 있으면 토스트 "예약 N건 갱신"
    → 실패 시 조용히 로그만 (토스트 없음)
    → 마지막 갱신 시간 표시: "마지막 조회: 15:30:22"
```

### UI 변경

```
[웹 조회 ▼]  [자동(2분) ✓]  마지막 조회: 15:30:22
```

- 자동 갱신 체크박스 → 토글 가능
- 앱 시작 시 기본 ON
- 탭 비활성(다른 탭) 시 타이머 정지 → 돌아오면 재시작

### 수정 파일

| 파일 | 변경 |
|------|------|
| `ReservationSalesTab.cs` | Timer 추가, FetchWebSilently(), 체크박스 UI |

---

## 3. 시간순 정렬 버튼

### 현재

- 예약 데이터가 웹 스크래핑 순서 그대로 표시

### 변경

```
[시간순 ↑] 토글 버튼
  클릭 1: 시간 오름차순 (10:00 → 23:00)
  클릭 2: 시간 내림차순 (23:00 → 10:00)
  클릭 3: 원래 순서

정렬 기준: TimeSlot 컬럼 (HH:MM-HH:MM 형식, 앞부분 기준)
```

### 수정 파일

| 파일 | 변경 |
|------|------|
| `ReservationSalesTab.cs` | 정렬 버튼 + 정렬 로직 |

---

## 4. "방" → "테마" 용어 통일

### 변경 대상

| 위치 | AS-IS | TO-BE |
|------|-------|-------|
| Reservation.RoomName | RoomName | ThemeName |
| 그리드 헤더 | "방" | "테마" |
| ParseReservations() | roomName 변수 | themeName |
| cubeescape-scraping.md | "방 이름" | "테마명" |

### 수정 파일

| 파일 | 변경 |
|------|------|
| `Core/Models/Reservation.cs` | RoomName → ThemeName |
| `Core/Services/ReservationScraperService.cs` | 파서 변수명 변경 |
| `Forms/ReservationSalesTab.cs` | 그리드 헤더 |
| `Data/Migrations/V004_Sales.cs` | 마이그레이션 검토 (room_name → theme_name) |

---

## 5. 예약 상태 전환 (confirmed → removed)

### 현재

- 스크래핑된 예약은 모두 status="confirmed" 고정
- 삭제/취소 기능 없음

### 변경

```
예약 행 우클릭 → 컨텍스트 메뉴
  ├─ "취소(삭제)" → status = "removed"
  │   → 행 스타일: 취소선 + 회색 텍스트 (#9CA3AF)
  │   → 결제 칸 비활성화
  └─ "복원" → status = "confirmed" (removed 상태에서만)

상태 표시:
  confirmed → 초록 태그 "확정"
  removed   → 빨강 태그 "취소"
```

### DB 변경

```sql
-- reservations 테이블 status 컬럼은 이미 TEXT
-- 값: 'confirmed', 'removed'
-- 추가 마이그레이션 불필요 (기존 스키마 호환)
```

### 수정 파일

| 파일 | 변경 |
|------|------|
| `ReservationSalesTab.cs` | 컨텍스트 메뉴 + 스타일 |
| `Core/Services/SalesService.cs` | 상태 변경 메서드 |
| `Data/Repositories/SalesRepository.cs` | UPDATE status SQL |
| `Core/Interfaces/` | 인터페이스에 메서드 추가 |

---

## 6. 예약+결제 통합 그리드 + 지출/요약 재배치

### 가장 큰 변경. 기존 2개 그리드(예약+매출)를 1개 통합 그리드로.

### 현재 레이아웃 (AS-IS)

```
┌─────────────────────────────────────┐
│ [날짜] [웹 조회] [+매출] [+지출]     │
├─────────────────────────────────────┤
│ ■ 오늘예약  ■ 총매출  ■ 카드  ■ 현금 │  SummaryCards
├─────────────────────────────────────┤
│ 예약 현황 (웹 조회)                   │
│ 시간 | 방 | 예약자 | 연락처 | 인원    │  _gridReservations (180px)
├─────────────────────────────────────┤
│ 매출 항목                            │
│ # | 항목 | 금액 | 결제 | 구분         │  _gridItems (Fill)
├─────────────────────────────────────┤
│ 카드: ₩X  현금: ₩X  계좌: ₩X        │  summaryPanel (100px)
│ 총매출: ₩X   현금잔액: ₩X           │
└─────────────────────────────────────┘
```

### 변경 레이아웃 (TO-BE)

```
┌──────────────────────────────────────────────────────┐
│ [날짜] [웹 조회] [자동 ✓] [시간순↑]  마지막: 15:30   │  toolBar
├──────────────────────────────────────────────────────┤
│ ■ 오늘예약  ■ 총매출  ■ 카드매출  ■ 현금잔액          │  SummaryCards
├──────────────────────────────────────────────────────┤
│ 예약 & 결제 현황                                      │
│ 시간 | 테마 | 예약자 | 연락처 | 인원 | 상태 |          │
│      | 카드금액 | 현금금액 | 계좌금액 |                 │  _gridMain (Fill)
│──────┼──────┼───────┼───────┼──────┼─────┤          │
│10:00 │ 집착 │ 김OO  │010... │  4   │ 확정 │          │
│      │ 50,000│      │       │      │      │          │
│10:00 │타이타닉│ 이OO │010... │  2   │ 취소 │          │
│      │       │      │       │ (취소선, 회색)│          │
├──────────────────────────────────────────────────────┤
│ 지출 내역                    │  카드   ₩ 150,000     │
│ # | 항목     | 금액  | 결제  │  현금   ₩  80,000     │  bottomPanel
│ 1 | 생수구입  | 5,000 | 현금  │  계좌   ₩  50,000     │
│ 2 | 영수증   | 3,000 | 카드  │  ─────────────────    │
│ [+ 지출 추가]                │  총매출  ₩ 280,000     │
│                              │  총지출  ₩   8,000     │
│                              │  현금잔액 ₩ 172,000    │
│                              │  (전일 100,000+80,000  │
│                              │   -8,000)              │
└──────────────────────────────────────────────────────┘
```

### 6-1. 통합 그리드 컬럼 (9열)

```
| 시간      | 테마    | 예약자 | 연락처    | 인원 | 상태 | 카드금액 | 현금금액 | 계좌금액 |
| 10%-fix  | 12%    | 10%   | 15%      | 6%  | 8%  | 13%     | 13%     | 13%     |
```

- 시간~상태: 스크래핑 데이터 (읽기 전용)
- 카드/현금/계좌금액: **편집 가능** (클릭하여 금액 직접 입력)
- 상태 "취소" 행은 금액 칸 비활성 + 회색 배경

### 6-2. 결제금액 입력 로직

```
카드금액 셀 클릭 → 숫자 입력 → Enter/Tab → 저장
  → SaleItem(category="revenue", paymentType="card", amount=입력값)
  → 해당 ReservationId에 연결

한 예약에 복합 결제 가능:
  카드 30,000 + 현금 20,000 = 총 50,000

금액이 0이거나 비어있으면 해당 결제수단 미사용으로 표시
  → 셀 배경 기본색 (흰색)
금액이 입력되면 결제 태그 색상 적용
  → 카드: #E3F2FD, 현금: #E8F5E9, 계좌: #FFF3E0
```

### 6-3. 결제수단 셀 표시 규칙

```
해당 결제수단에 금액이 있을 때만 색상+금액 표시
금액 0 → 빈 셀 (입력 대기 상태)
금액 > 0 → 배경색 + "₩ 50,000" 포맷

예시:
  카드 50,000 / 현금 0 / 계좌 0
  → [₩50,000(파랑배경)] [    ] [    ]
```

### 6-4. 하단 패널 (지출 + 요약)

```
┌─ 좌측: 지출 그리드 (60%) ───────┬─ 우측: 요약 패널 (40%) ─┐
│ _gridExpense                    │ _summaryPanel            │
│                                 │                          │
│ # | 항목    | 금액   | 결제     │  카드    ₩ 150,000       │
│ 1 | 생수    | 5,000  | 현금     │  현금    ₩  80,000       │
│ 2 | 테이프  | 3,000  | 카드     │  계좌    ₩  50,000       │
│                                 │  ────────────────        │
│ [+ 지출 추가]                    │  총매출   ₩ 280,000      │
│                                 │  총지출   ₩   8,000      │
│                                 │  ────────────────        │
│                                 │  현금잔액  ₩ 172,000     │
│                                 │  (전일100,000            │
│                                 │   +80,000 -8,000)        │
└─────────────────────────────────┴──────────────────────────┘
```

**우측 요약 항목:**
- 카드 합계 (파랑 태그)
- 현금 합계 (초록 태그)
- 계좌 합계 (주황 태그)
- 구분선
- 총매출 (Bold)
- 총지출 (빨강)
- 구분선
- **현금 잔액** (Bold, Primary색)
- 산식: (전일 현금잔액 + 오늘 현금수입 - 오늘 현금지출)

---

## 추가 검토 사항 (자동 식별)

### A. 스크래핑 데이터 ↔ 결제 데이터 연결

```
현재: Reservation과 SaleItem이 별개 (ReservationId 연결 없음)
변경: 통합 그리드에서 한 행이 Reservation + 연결된 SaleItems
연결 키: sale_items.reservation_id → reservations.id

한 예약에 여러 SaleItem 가능 (복합 결제)
→ 그리드 표시: 카드금액 = SUM(card items), 현금금액 = SUM(cash items), ...
```

### B. 스크래핑 갱신 시 기존 결제 데이터 보존

```
2분마다 스크래핑 → 새 데이터로 예약 목록 갱신
이때 기존에 입력한 결제 금액이 사라지면 안 됨

방법: UPSERT 방식
  → (date + time_slot + theme_name) 조합이 동일하면 UPDATE (결제 유지)
  → 새 예약이면 INSERT
  → 사라진 예약은 status='removed'로 변경 (삭제 X)
```

### C. 예약 없는 매출 처리

```
워크인(예약 없이 방문) 고객의 결제는?
→ 통합 그리드에 [+ 매출 추가] 버튼 → 수동 행 추가
→ reservation_id = NULL, 시간/테마는 직접 입력
→ 상태: "직접입력"으로 표시
```

### D. 하루 마감 플로우

```
현금 잔액은 전일 기준 자동 계산
→ 하루가 끝나면 cash_balance에 closing_balance 확정
→ 다음 날 opening_balance = 전날 closing_balance
```

---

## 구현 순서 (Phase)

### Phase A: 기반 변경 (의존 없음)

```
1. SideNavPanel 상시 표시 변경
2. "방" → "테마" 용어 통일 (Model + 파서)
```

### Phase B: 예약 기능 강화

```
3. 예약 상태 전환 (confirmed ↔ removed)
4. 시간순 정렬 버튼
5. 2분 자동 갱신 타이머
```

### Phase C: 통합 그리드 (핵심)

```
6. ReservationSalesTab 레이아웃 전면 재설계
7. 통합 그리드 (예약+결제)
8. 셀 직접 편집 → SaleItem 자동 생성/수정
9. 스크래핑 UPSERT 로직 (결제 데이터 보존)
```

### Phase D: 하단 패널

```
10. 지출 그리드 (좌측)
11. 요약 패널 (우측) — 카드/현금/계좌/총매출/지출/현금잔액
12. 현금 잔액 자동 계산
```

### Phase E: 폴리싱

```
13. 결제 태그 색상 적용
14. 취소 행 스타일 (취소선 + 회색)
15. 워크인 매출 수동 추가
16. 빌드 테스트 + 커밋
```

---

## 수정 파일 총괄

### 신규

| 파일 | 역할 |
|------|------|
| `Data/Migrations/V007_ThemeHints.cs` | theme_name 컬럼 변경 (필요 시) |

### 수정

| 파일 | 변경 |
|------|------|
| `Controls/SideNavPanel.cs` | 상시 200px 고정 |
| `MainForm.cs` | SideNav 너비 상수 |
| `Core/Models/Reservation.cs` | RoomName → ThemeName |
| `Core/Services/ReservationScraperService.cs` | 파서 변수 + UPSERT |
| `Core/Services/SalesService.cs` | 상태 변경 + 예약-결제 연결 |
| `Core/Interfaces/Services/ISalesService.cs` | 메서드 추가 |
| `Data/Repositories/SalesRepository.cs` | UPSERT + 상태 변경 SQL |
| `Forms/ReservationSalesTab.cs` | **전면 재설계** |
| `Helpers/GridTheme.cs` | 취소 행 스타일 메서드 |
