# cubeescape.co.kr 스크래핑 명세

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
POST http://www.cubeescape.co.kr/bbs/login.php
```

### 2.2 AngleSharp 로그인

```csharp
public async Task<IBrowsingContext> LoginAsync(string id, string password)
{
    var config = Configuration.Default
        .WithDefaultLoader()
        .WithCookies();
    var context = BrowsingContext.New(config);

    // 로그인 페이지 열기
    var loginPage = await context.OpenAsync(
        "http://www.cubeescape.co.kr/bbs/login.php");

    // 폼 찾기 (그누보드 표준 로그인 폼)
    var form = loginPage.QuerySelector<IHtmlFormElement>("form[name='flogin']")
            ?? loginPage.QuerySelector<IHtmlFormElement>("form");

    if (form == null)
        throw new ScrapingException("로그인 폼을 찾을 수 없습니다.");

    // 폼 서밋 (쿠키 자동 관리)
    await form.SubmitAsync(new
    {
        mb_id = id,
        mb_password = password
    });

    return context; // 이후 요청에서 세션 유지
}
```

### 2.3 로그인 실패 감지

```csharp
// 로그인 후 리다이렉트된 URL 또는 페이지 내용으로 판별
var testPage = await context.OpenAsync(
    "http://www.cubeescape.co.kr/adm/room_list.php");
var body = testPage.Body?.TextContent ?? "";

if (body.Contains("로그인") && !body.Contains("예약"))
    throw new AuthenticationException("로그인 실패: ID/PW 확인 필요");
```

---

## 3. 예약 테이블 조회

### 3.1 URL 패턴

```
GET http://www.cubeescape.co.kr/adm/room_list.php?sfl=r_date&stx={YY-MM-DD}

날짜 형식: 2자리 연도 (26-03-18 = 2026년 3월 18일)
```

### 3.2 HTML 구조 (예상)

> 실제 HTML 구조는 최초 스크래핑 시 확인 후 이 문서를 업데이트한다.
> 아래는 그누보드 관리자 테이블의 일반적 구조를 기반으로 한 예상이다.

```html
<!-- 예상 구조 (실제와 다를 수 있음) -->
<table class="table" or id="room_table">
  <thead>
    <tr>
      <th>시간</th>
      <th>방1 이름</th>
      <th>방2 이름</th>
      <th>방3 이름</th>
      ...
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>10:00</td>
      <td>홍길동 (4명) 010-1234-5678</td>
      <td></td>
      <td>김철수 (3명)</td>
      ...
    </tr>
    <tr>
      <td>11:00</td>
      ...
    </tr>
  </tbody>
</table>
```

### 3.3 파싱 전략

```csharp
public async Task<List<Reservation>> FetchReservationsAsync(
    IBrowsingContext context, DateTime date)
{
    var dateStr = date.ToString("yy-MM-dd");
    var url = $"http://www.cubeescape.co.kr/adm/room_list.php?sfl=r_date&stx={dateStr}";

    var page = await context.OpenAsync(url);

    // 1차 시도: 테이블 찾기 (여러 선택자 시도)
    var table = page.QuerySelector("table.room-table")
             ?? page.QuerySelector("table#room_table")
             ?? page.QuerySelector("table.table")
             ?? page.QuerySelectorAll("table").LastOrDefault();

    if (table == null)
    {
        Log.Warning("예약 테이블을 찾을 수 없습니다: {Url}", url);
        return new List<Reservation>();
    }

    var rows = table.QuerySelectorAll("tbody tr, tr");
    var reservations = new List<Reservation>();

    // 헤더에서 방 이름 추출
    var headers = table.QuerySelectorAll("thead th, th")
        .Select(th => th.TextContent.Trim())
        .ToList();

    foreach (var row in rows)
    {
        var cells = row.QuerySelectorAll("td").ToList();
        if (cells.Count < 2) continue;

        var timeSlot = cells[0].TextContent.Trim();

        for (int i = 1; i < cells.Count && i < headers.Count; i++)
        {
            var cellText = cells[i].TextContent.Trim();
            if (string.IsNullOrEmpty(cellText)) continue;

            reservations.Add(ParseCellToReservation(
                date, timeSlot, headers[i], cellText));
        }
    }

    return reservations;
}
```

### 3.4 셀 텍스트 파싱

```csharp
// 셀 내용 예: "홍길동 (4명) 010-1234-5678"
// 또는: "홍길동\n4명"
// 또는: "예약자: 홍길동 / 인원: 4"
// → 패턴이 불확실하므로 유연하게 파싱

private Reservation ParseCellToReservation(
    DateTime date, string timeSlot, string roomName, string cellText)
{
    // 이름: 첫 번째 단어 또는 괄호 이전
    // 인원: 숫자 + "명" 패턴
    // 전화번호: 010-XXXX-XXXX 패턴

    var nameMatch = Regex.Match(cellText, @"^([가-힣a-zA-Z]+)");
    var countMatch = Regex.Match(cellText, @"(\d+)\s*명");
    var phoneMatch = Regex.Match(cellText, @"(01[016789]-?\d{3,4}-?\d{4})");

    return new Reservation
    {
        ReservationDate = date,
        TimeSlot = timeSlot,
        RoomName = roomName,
        CustomerName = nameMatch.Success ? nameMatch.Groups[1].Value : cellText,
        Headcount = countMatch.Success ? int.Parse(countMatch.Groups[1].Value) : 0,
        CustomerPhone = phoneMatch.Success ? phoneMatch.Groups[1].Value : null,
        Status = "confirmed",
        SyncedAt = DateTime.Now
    };
}
```

---

## 4. 파싱 실패 대응

| 상황 | 대응 |
|------|------|
| 테이블 못 찾음 | 경고 로그 + 빈 목록 반환 + UI에 "파싱 실패" 표시 |
| 셀 형식 변경 | 원본 텍스트를 `raw_html`에 저장 + 기본 파싱 시도 |
| 사이트 다운 | 타임아웃(10초) 후 캐시 데이터 반환 |
| 세션 만료 | 자동 재로그인 1회 시도 → 실패 시 UI 알림 |
| HTML 구조 변경 | 로그에 원본 HTML 기록 → 개발자가 선택자 업데이트 |

---

## 5. 최초 개발 시 해야 할 것

```
1. 실제 사이트에 로그인하여 room_list.php의 HTML 소스 확보
2. 이 문서의 "3.2 HTML 구조" 섹션을 실제 구조로 업데이트
3. CSS 셀렉터 확정
4. 셀 텍스트 형식 확인 후 ParseCellToReservation 정규식 조정
5. 테스트: 실제 HTML 스냅샷을 파일로 저장 → 오프라인 파싱 테스트
```

---

## 6. 오프라인 테스트용 HTML 스냅샷

```
개발 시:
  1. 실제 사이트에서 예약 페이지 HTML을 저장
  2. tests/fixtures/room_list_sample.html 로 저장
  3. 단위 테스트에서 이 파일을 로드하여 파싱 검증

→ 사이트 없이도 파싱 로직 테스트 가능
→ HTML 구조 변경 시 스냅샷만 교체하면 회귀 테스트 가능
```
