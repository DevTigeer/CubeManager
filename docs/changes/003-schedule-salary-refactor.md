# 변경 기록 #003: 스케줄/급여 탭 개선

> 작성일: 2026-03-20
> 상태: 구현 예정

---

## 변경 항목

### 1. 스케줄 탭 — 버튼명 변경

```
AS-IS: "+ 직원 추가"
TO-BE: "+ 스케줄 추가"
```

이유: 직원 자체를 추가하는 것이 아니라 스케줄을 추가하는 동작이므로 명칭 통일.

### 2. 타임테이블 — 같은 시간 겹침 시 이름 동시 표시

```
AS-IS: GroupBy(EmployeeId, WorkDate) → 같은 날짜에 한 직원당 하나의 블록만 표시
       다른 직원이 같은 시간대면 뒤에 블록이 가려짐

TO-BE: 같은 날짜+시간대에 여러 직원이 있으면:
       - 셀 폭을 N등분하여 나란히 표시
       - 또는 하나의 셀에 이름을 줄바꿈으로 표시
       → N등분 방식 채택 (시각적으로 명확)
```

구현:
- 같은 날짜의 스케줄을 시간 겹침 기준으로 그룹핑
- 겹치는 블록 수(overlapCount)와 블록 인덱스(overlapIndex)를 계산
- cellW를 overlapCount로 나눠서 x 오프셋 적용

### 3. 주간 월경계 처리 + 급여정산 기준일

```
AS-IS: GetWeekRange()에서 weekStart/weekEnd를 월 경계로 클램핑
       → 2.23(월)~3.1(일) 주가 2월에서는 2.23~2.28, 3월에서는 3.1로 분리

TO-BE: 주간 표시는 월 경계를 넘어서 실제 월~일 전체를 표시
       급여정산 기준:
       - 마지막 주의 수요일까지 해당 월 → 해당 월로 정산
       - 마지막 주의 목요일부터 해당 월 → 다음 달로 정산
```

#### 정산 기준 로직 (GetSalaryMonth)

```
주간: 2/23(월) ~ 3/1(일)
  - 수요일 = 2/25 → 2월 → 이 주는 2월 급여에 포함

주간: 3/30(월) ~ 4/5(일)
  - 수요일 = 4/1 → 4월 → 이 주는 4월 급여에 포함
```

구현:
- `TimeHelper.GetWeekRange()` 수정: 월 경계 클램핑 제거 → 실제 월~일 반환
- `TimeHelper.GetSalaryMonth()` 신규: 주의 수요일 날짜 기준으로 정산 월 결정
- `SalaryService.CalculateAllAsync()` 수정: 정산 기준으로 스케줄 재분배

### 4. 스케줄추가 다이얼로그에 월/주차 선택

```
AS-IS: 요일만 선택 → 해당 월 전체에 적용

TO-BE: 몇 월 / 몇 째주 체크박스 추가
       → 체크된 주차에만 스케줄 생성
       ex) 3월 / 1주차✓ 2주차✓ 3주차✓ → 해당 3주에만 등록
```

구현:
- ScheduleInputDialog에 월(콤보박스) + 주차(체크박스 1~5) 추가
- 다이얼로그 높이 확장 (320 → 400)
- AddScheduleAsync()에 주차 필터 파라미터 추가

### 5. 급여 탭 컬럼 재배치

```
AS-IS 컬럼:
  이름 | 시급 | 1주 | 2주 | 3주 | 4주 | 5주+ | 합계 | 식비 | 택시 | 공휴일 | 총급여 | 3.3% | 실수령

TO-BE 컬럼:
  이름 | 시급 | 1주 | 2주 | 3주 | 4주 | 5주+ | 합계 | 공휴일 | 총급여 | 3.3%(수령액) | ─간격─ | 식비 | 택시

변경사항:
  ① "실수령" 컬럼 삭제
  ② "3.3%" 컬럼에 세후 수령금액 표시 (기존 3.3%세액이 아닌, gross - tax)
  ③ 식비/택시비는 총급여에 미포함 (별도 참고용)
  ④ 식비/택시비 컬럼을 오른쪽 끝으로 이동
```

3.3% 계산 변경:
```
AS-IS: gross = base + holiday + meal + taxi → tax = gross * 0.033
TO-BE: gross = base + holiday             → tax = gross * 0.033 → net = gross - tax
       식비/택시비는 gross에 미포함, 별도 표시
```

---

## 수정 대상 파일

| 파일 | 변경 |
|------|------|
| `ScheduleTab.cs` | 버튼명 "+ 스케줄 추가" |
| `TimeTablePanel.cs` | 겹침 블록 N등분 렌더링 |
| `TimeHelper.cs` | GetWeekRange() 클램핑 제거, GetSalaryMonth() 추가 |
| `ScheduleInputDialog.cs` | 월/주차 선택 UI 추가 |
| `ScheduleService.cs` | AddScheduleAsync() 주차 필터 |
| `SalaryService.cs` | 3.3% 계산 변경 (식비/택시 분리) |
| `SalaryTab.cs` | 컬럼 재배치 |
| `IScheduleService.cs` | AddScheduleAsync() 시그니처 변경 |
