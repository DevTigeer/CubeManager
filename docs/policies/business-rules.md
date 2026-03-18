# 비즈니스 규칙 상세

> 명세에서 모호했던 계산 로직, 경계 조건, 반올림 규칙을 확정한다.

---

## 1. 주차(Week) 계산 알고리즘

### 1.1 정의

```
주(Week)의 기준: 월요일 시작 ~ 일요일 종료
1주차: 해당 월 1일이 속한 주
월 경계: 해당 월에 속한 날짜만 포함 (이전/다음 월 날짜 제외)
```

### 1.2 알고리즘

```csharp
/// 주어진 날짜가 해당 월의 몇 주차인지 반환 (1-based)
public static int GetWeekOfMonth(DateTime date)
{
    var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);

    // 1일이 속한 주의 월요일 찾기
    var firstMonday = firstDayOfMonth;
    while (firstMonday.DayOfWeek != DayOfWeek.Monday)
        firstMonday = firstMonday.AddDays(-1);

    // 해당 날짜가 속한 주의 월요일 찾기
    var targetMonday = date;
    while (targetMonday.DayOfWeek != DayOfWeek.Monday)
        targetMonday = targetMonday.AddDays(-1);

    // 주 차이 + 1
    var weekNum = ((targetMonday - firstMonday).Days / 7) + 1;
    return weekNum;
}

/// 주어진 월의 N주차에 해당하는 날짜 범위 반환 (해당 월 날짜만)
public static (DateTime start, DateTime end) GetWeekRange(
    int year, int month, int weekNum)
{
    var firstDay = new DateTime(year, month, 1);
    var lastDay = new DateTime(year, month,
        DateTime.DaysInMonth(year, month));

    var firstMonday = firstDay;
    while (firstMonday.DayOfWeek != DayOfWeek.Monday)
        firstMonday = firstMonday.AddDays(-1);

    var weekStart = firstMonday.AddDays((weekNum - 1) * 7);
    var weekEnd = weekStart.AddDays(6);

    // 월 경계 클램핑
    if (weekStart < firstDay) weekStart = firstDay;
    if (weekEnd > lastDay) weekEnd = lastDay;

    return (weekStart, weekEnd);
}
```

### 1.3 예시 (2026년 3월)

```
2026-03-01 = 일요일

1주차: 3/1(일)            → 1일간 (월경계: 3/1만)
2주차: 3/2(월) ~ 3/8(일)  → 7일간
3주차: 3/9(월) ~ 3/15(일) → 7일간
4주차: 3/16(월) ~ 3/22(일)→ 7일간
5주차: 3/23(월) ~ 3/29(일)→ 7일간
6주차: 3/30(월) ~ 3/31(화)→ 2일간 (월경계: 31일까지)
```

### 1.4 급여 테이블 반영

- salary_records에 `week1_hours` ~ `week5_hours` + `week6_hours`(추가 컬럼 필요 시)
- 실제로 6주차가 발생할 수 있으므로 **week5_hours에 5주차+6주차를 합산**
- 또는 week5_hours 컬럼을 "5주차 이후 전체"로 정의

```
확정: week5_hours = 5주차 이후 모든 남은 날의 합산
     (5주차가 없으면 0, 6주차까지 있으면 5+6주차 합산)
```

---

## 2. 자정(Midnight) 경계 처리

### 2.1 야간 근무 시간 비교

```
운영 시간: 10:00 ~ 익일 01:00
01:00은 "25:00"으로 내부 변환하여 비교

변환 규칙:
  00:00 → 24:00
  00:30 → 24:30
  01:00 → 25:00

DB 저장은 원래 형식 ("01:00"), 비교 시에만 변환
```

```csharp
/// 시간 문자열을 분(minutes) 단위 정수로 변환 (자정 이후 보정)
public static int TimeToMinutes(string time)
{
    var parts = time.Split(':');
    var hours = int.Parse(parts[0]);
    var minutes = int.Parse(parts[1]);

    // 자정 이후 시간은 24시간 더함 (01:00 → 25:00)
    if (hours < 10) // 운영 시작(10시) 이전이면 익일로 판단
        hours += 24;

    return hours * 60 + minutes;
}

// 사용 예:
// "14:00" → 840분
// "23:30" → 1410분
// "00:30" → 1470분 (24:30)
// "01:00" → 1500분 (25:00)
```

### 2.2 출퇴근 판정 (자정 포함)

```
예정 출근: 22:00, 예정 퇴근: 01:00 (익일)

출근 판정:
  실제 21:55 → TimeToMinutes("21:55")=1315 <= TimeToMinutes("22:00")=1320 → 정상
  실제 22:05 → 1325 > 1320 → 지각

퇴근 판정:
  실제 01:05 → TimeToMinutes("01:05")=1505 >= TimeToMinutes("01:00")=1500 → 정상
  실제 00:45 → 1485 < 1500 → 조퇴
```

### 2.3 출퇴근 기록 날짜

```
출퇴근 기록의 work_date = 출근 기준 날짜

예: 3/18 22:00 출근, 3/19 01:00 퇴근
  → work_date = 2026-03-18
  → clock_in = 2026-03-18 22:00:00
  → clock_out = 2026-03-19 01:00:00

비교 시: clock_out의 날짜는 무시하고 시간만 TimeToMinutes로 변환
```

---

## 3. 금액 계산 & 반올림 규칙

### 3.1 기본 원칙

```
모든 금액은 정수(int, 원 단위)
소수점 발생 시: 내림(truncate, Math.Floor 방향)

이유: 세금 계산에서 근로자에게 유리한 방향 = 세금은 내림
```

### 3.2 3.3% 세금 계산

```csharp
public static int CalculateTax33(int grossSalary)
{
    // 내림 (소수점 이하 버림)
    return (int)(grossSalary * 0.033);
}

// 예시:
// 1,754,000 × 0.033 = 57,882.0 → 57,882
// 1,000,001 × 0.033 = 33,000.033 → 33,000 (내림)
// 999,999 × 0.033 = 32,999.967 → 32,999 (내림)
```

### 3.3 공휴일 수당

```csharp
public static int CalculateHolidayBonus(double holidayHours, int bonusPerHour)
{
    // 시간 × 단가, 내림
    return (int)(holidayHours * bonusPerHour);
}

// 예시:
// 4.5h × 3,000 = 13,500 → 13,500 (정확)
// 4.333h × 3,000 = 12,999.9 → 12,999 (내림)
```

### 3.4 근무시간 계산

```
근무시간 = (퇴근 분 - 출근 분) / 60.0

단위: double (소수점 1자리까지 의미)
반올림: 없음 (정확한 분 단위 계산 후 시간 변환)

예시:
  14:00 ~ 19:30 = 330분 / 60 = 5.5h
  10:00 ~ 18:00 = 480분 / 60 = 8.0h
  22:00 ~ 01:00 = 180분 / 60 = 3.0h (자정 보정 후)
```

---

## 4. 식비 판정

```
조건: 하루 스케줄 상 근무시간 >= 6.0시간 (6시간 이상)
판정 기준: 스케줄 시간 (실제 출퇴근 시간이 아님)

5.5h → 미지급
6.0h → 지급
6.5h → 지급

"이상" = greater than or equal (>=)
분 단위까지 정확 계산: 5시간 59분 = 5.983h → 미지급
```

---

## 5. 택시비 판정

```
조건: 스케줄 상 퇴근시간 >= 23:30
판정 기준: 스케줄 퇴근 시간 (실제 퇴근 시간이 아님)
비교 단위: 분 단위 (HH:MM 기준, 초 무시)

23:29 → 미지급
23:30 → 지급 (이상 = 포함)
00:00 → 지급 (자정 보정: 24:00 >= 23:30)
01:00 → 지급 (자정 보정: 25:00 >= 23:30)
```

---

## 6. 현금 잔액 경계 처리

```
음수 잔액 허용:
  현금 잔액이 음수가 될 수 있다 (비품 구매로 현금 초과 지출 시)
  UI에서 빨간색으로 경고 표시하되, 입력은 차단하지 않는다

전일 잔액이 없는 경우:
  최초 사용일 또는 누락된 날: opening_balance = 0
  이전 날짜 데이터가 없으면 0원에서 시작
```

---

## 7. 인수인계 댓글 깊이 제한

```
최대 깊이: 2단계 (글 → 댓글 → 대댓글)
3단계 이상: UI에서 [답글] 버튼을 숨김
DB 제약: 없음 (parent_comment_id는 구조적으로 무한 가능)
제한 강제: UI 레이어에서만 (대댓글에는 [답글] 버튼 비표시)
```

---

## 8. 업무자료 휴지통

```
삭제 시: data/documents/.trash/ 폴더로 이동
파일명: {원래파일명}_{삭제시각}.md (충돌 방지)
보관: 30일
자동 정리: 앱 시작 시 30일 초과 파일 영구 삭제
복원: .trash에서 원래 위치로 이동 (수동)
```

---

## 9. 공휴일 판정 우선순위

```
1순위: holidays 테이블 (DB에 저장된 데이터)
2순위: 공공데이터포털 API (연 1회 자동 갱신 시도)
3순위: 내장 고정 공휴일 (설, 추석 등은 연도별 하드코딩)

판정:
  해당 날짜가 holidays 테이블에 있고 + 요일이 월~금 → 평일 공휴일 → 수당 적용
  해당 날짜가 holidays 테이블에 있고 + 요일이 토/일 → 주말 공휴일 → 수당 미적용
  대체 공휴일(대체휴일)은 holidays 테이블에 별도 등록 필요
```
