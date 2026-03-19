# cubeescape.co.kr 스크래핑 명세

> 최종 확인일: 2026-03-19 (실제 사이트 HTML 검증 완료)

## 1. 사이트 정보

```
URL: http://www.cubeescape.co.kr
관리자: http://www.cubeescape.co.kr/adm
CMS: 그누보드 (PHP 기반)
인증: 세션 쿠키 (PHP SESSION)
프로토콜: HTTP (HTTPS 아님)
```

---

## 2. 로그인 프로세스

### 2.1 로그인 URL

```
POST http://www.cubeescape.co.kr/bbs/login_check.php
(폼 action이 login_check.php로 향함)
```

### 2.2 로그인 폼 구조 (실제 확인됨)

```html
<div id="mb_login" class="ms-confirm">
  <form name="flogin" action=".../bbs/login_check.php" method="post">
    <input type="hidden" name="url" value="...">
    <input type="text" name="mb_id" id="login_id" required maxLength="20">
    <input type="password" name="mb_password" id="login_pw" required maxLength="20">
    <input type="submit" value="로그인">
  </form>
</div>
```

### 2.3 AngleSharp 로그인

```csharp
var config = Configuration.Default
    .WithDefaultLoader(new LoaderOptions { IsResourceLoadingEnabled = false })
    .WithCookies();
var context = BrowsingContext.New(config);

var loginPage = await context.OpenAsync(
    "http://www.cubeescape.co.kr/bbs/login.php");

// 그누보드 표준 로그인 폼
var form = loginPage.QuerySelector<IHtmlFormElement>("form[name='flogin']")
        ?? loginPage.QuerySelector<IHtmlFormElement>("form");

await form.SubmitAsync(new { mb_id = id, mb_password = password });
```

### 2.4 로그인 성공 판별

```csharp
var testPage = await context.OpenAsync("http://www.cubeescape.co.kr/adm/");
var body = testPage.Body?.TextContent ?? "";

// 관리자 페이지에 "예약" 키워드가 있으면 성공
var success = !body.Contains("로그인") || body.Contains("예약");
```

---

## 3. 예약 테이블 조회

### 3.1 URL 패턴

```
GET http://www.cubeescape.co.kr/adm/room_list.php?sfl=r_date&stx={YY-MM-DD}

날짜 형식: 2자리 연도 (26-03-19 = 2026년 3월 19일)
```

### 3.2 HTML 구조 (실제 확인됨 - 2026-03-19)

```html
<table>
  <caption>예약문의 관리 목록</caption>
  <thead>
    <tr>
      <th><!-- 체크박스 --></th>
      <th>지점명</th>
      <th>예약번호</th>
      <th>예약일</th>
      <th>시간</th>        <!-- 정렬 링크 포함 -->
      <th>선택테마</th>     <!-- 정렬 링크 포함 -->
      <th>인원</th>
      <th>예약자</th>
      <th>연락처</th>
      <th>아이피</th>
      <th>등록일</th>
      <th>관리</th>
    </tr>
  </thead>
  <tbody>
    <tr class="bg0">
      <td class="td_chk"><!-- 체크박스 + hidden r_num --></td>
      <td class="txt_center">인천구월점</td>
      <td class="txt_center">55911880</td>
      <td class="txt_center">2026-03-19</td>
      <td class="txt_center">11:45-12:45</td>
      <td class="txt_center">집착</td>
      <td class="txt_center">2 명</td>
      <td class="txt_center">ㅁ</td>
      <td class="txt_center">010-1111-1111</td>
      <td class="txt_center">121.142.110.213</td>
      <td class="td_datetime">2026-03-06</td>
      <td class="td_mngsmall"><a href="...">보기</a></td>
    </tr>
    <!-- 한 행 = 한 예약건 (반복) -->
  </tbody>
</table>
```

### 3.3 핵심 포인트

```
구조: 한 행 = 한 예약건 (NOT 시간슬롯 × 방 매트릭스)
행 교차 배경: bg0, bg1 클래스 교번
헤더 정렬: <th> 안에 <a> 링크로 정렬 가능 (TextContent로 추출 가능)
인원 형식: "2 명" (숫자 + 공백 + 명)
시간 형식: "11:45-12:45" (시작-종료)
테마 = 방 이름: "집착", "Towering", "신데렐라", "타이타닉" 등
```

### 3.4 컬럼 매핑

| 헤더 (실제) | 인덱스 | → Reservation 모델 |
|-------------|--------|---------------------|
| (체크박스) | 0 | 무시 |
| 지점명 | 1 | (참조용, 현재 단일 지점) |
| 예약번호 | 2 | (향후 확장용) |
| 예약일 | 3 | `ReservationDate` |
| 시간 | 4 | `TimeSlot` |
| 선택테마 | 5 | `RoomName` |
| 인원 | 6 | `Headcount` |
| 예약자 | 7 | `CustomerName` |
| 연락처 | 8 | `CustomerPhone` |
| 아이피 | 9 | 무시 |
| 등록일 | 10 | 무시 |
| 관리 | 11 | 무시 |

### 3.5 파싱 전략

```csharp
// 헤더 기반 동적 인덱스 매핑 (컬럼 순서 변경에 강건)
var headers = table.QuerySelectorAll("thead th")
    .Select((h, i) => (text: h.TextContent.Trim(), index: i))
    .ToList();

int ColIndex(string keyword) =>
    headers.FirstOrDefault(h => h.text.Contains(keyword)).index;

var idxTime  = ColIndex("시간");
var idxTheme = ColIndex("테마");
var idxCount = ColIndex("인원");
var idxName  = ColIndex("예약자");
var idxPhone = ColIndex("연락처");
```

---

## 4. 파싱 실패 대응

| 상황 | 대응 |
|------|------|
| 테이블 못 찾음 | 경고 로그 + 빈 목록 반환 + UI에 "파싱 실패" 표시 |
| 컬럼 순서 변경 | 헤더 키워드 기반 동적 매핑으로 대응 |
| 사이트 다운 | 타임아웃(10초) 후 빈 목록 반환 |
| 세션 만료 | 자동 재로그인 1회 시도 → 실패 시 UI 알림 |
| HTML 구조 변경 | 로그에 원본 HTML 기록 → 개발자가 업데이트 |
| 데이터 없는 날 | `<tbody>` 비어있음 → 빈 목록 정상 반환 |

---

## 5. 스크래핑 데이터 → 앱 내 활용

```
[웹 조회] 버튼 클릭
  → FetchReservationsAsync(date)
  → List<Reservation> 반환
  → DataGridView에 표시
  → 사용자가 결제 정보 수동 추가 (카드/현금/계좌 태그)
  → Sales 테이블에 저장 (매출 집계용)
```

---

## 6. 오프라인 테스트

```
테스트 시:
  1. 실제 사이트에서 예약 페이지 HTML을 저장
  2. tests/fixtures/room_list_sample.html 로 저장
  3. 단위 테스트에서 이 파일을 로드하여 파싱 검증

→ 사이트 없이도 파싱 로직 테스트 가능
→ HTML 구조 변경 시 스냅샷만 교체하면 회귀 테스트 가능
```
