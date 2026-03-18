# 데이터베이스 스키마

## 개요

SQLite 기반 로컬 데이터베이스. 파일명: `cubemanager.db`

---

## 1. employees (직원)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| name | TEXT NOT NULL | 직원 이름 |
| hourly_wage | INTEGER DEFAULT 0 | 시급 (원) |
| is_active | BOOLEAN DEFAULT 1 | 활성 상태 (스케줄 추가 가능 여부) |
| phone | TEXT | 연락처 |
| created_at | DATETIME | 생성일 |
| updated_at | DATETIME | 수정일 |

---

## 2. schedules (근무 스케줄)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| employee_id | INTEGER FK | employees.id |
| work_date | DATE NOT NULL | 근무 날짜 |
| start_time | TIME NOT NULL | 출근 시간 (HH:MM) |
| end_time | TIME NOT NULL | 퇴근 시간 (HH:MM) |
| is_holiday | BOOLEAN DEFAULT 0 | 공휴일 여부 |
| note | TEXT | 메모 |
| created_at | DATETIME | |
| updated_at | DATETIME | |

**인덱스**: `(employee_id, work_date)` UNIQUE

---

## 3. attendance (출퇴근 기록)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| employee_id | INTEGER FK | employees.id |
| work_date | DATE NOT NULL | 근무 날짜 |
| clock_in | DATETIME | 실제 출근 시각 |
| clock_out | DATETIME | 실제 퇴근 시각 |
| clock_in_status | TEXT | 'on_time' / 'late' |
| clock_out_status | TEXT | 'on_time' / 'early' |
| created_at | DATETIME | |

**인덱스**: `(employee_id, work_date)` UNIQUE

---

## 4. reservations (예약)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| reservation_date | DATE NOT NULL | 예약 날짜 |
| time_slot | TEXT | 예약 시간대 |
| room_name | TEXT | 방 이름 |
| customer_name | TEXT | 예약자 이름 |
| customer_phone | TEXT | 예약자 연락처 |
| headcount | INTEGER | 인원수 |
| status | TEXT | 'confirmed' / 'cancelled' / 'completed' |
| raw_html | TEXT | 원본 HTML (디버깅용) |
| synced_at | DATETIME | 마지막 동기화 시각 |

**인덱스**: `(reservation_date, time_slot, room_name)` UNIQUE

---

## 5. daily_sales (일 매출)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| sale_date | DATE NOT NULL UNIQUE | 매출 날짜 |
| card_amount | INTEGER DEFAULT 0 | 카드 결제 합계 |
| cash_amount | INTEGER DEFAULT 0 | 현금 결제 합계 |
| transfer_amount | INTEGER DEFAULT 0 | 계좌이체 합계 |
| total_revenue | INTEGER DEFAULT 0 | 총 매출 |
| note | TEXT | 비고 |
| created_at | DATETIME | |
| updated_at | DATETIME | |

---

## 6. sale_items (개별 결제 항목)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| daily_sales_id | INTEGER FK | daily_sales.id |
| reservation_id | INTEGER FK NULL | reservations.id (연결 시) |
| description | TEXT | 항목 설명 |
| amount | INTEGER NOT NULL | 금액 |
| payment_type | TEXT NOT NULL | 'card' / 'cash' / 'transfer' |
| category | TEXT DEFAULT 'revenue' | 'revenue' / 'expense' |
| created_at | DATETIME | |

**payment_type**: card=카드, cash=현금, transfer=계좌이체
**category**: revenue=매출, expense=지출(비품구매, 현금출금 등)

---

## 7. cash_balance (현금 잔액)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| balance_date | DATE NOT NULL UNIQUE | 날짜 |
| opening_balance | INTEGER DEFAULT 0 | 전일 이월 현금 |
| cash_in | INTEGER DEFAULT 0 | 현금 수입 |
| cash_out | INTEGER DEFAULT 0 | 현금 지출 |
| closing_balance | INTEGER DEFAULT 0 | 마감 현금 잔액 |
| note | TEXT | 비고 |

---

## 8. salary_records (급여 기록)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| employee_id | INTEGER FK | employees.id |
| year_month | TEXT NOT NULL | 'YYYY-MM' 형식 |
| week1_hours | REAL DEFAULT 0 | 1주차 근무시간 |
| week2_hours | REAL DEFAULT 0 | 2주차 근무시간 |
| week3_hours | REAL DEFAULT 0 | 3주차 근무시간 |
| week4_hours | REAL DEFAULT 0 | 4주차 근무시간 |
| week5_hours | REAL DEFAULT 0 | 5주차 근무시간 (있는 경우) |
| total_hours | REAL DEFAULT 0 | 총 근무시간 |
| holiday_hours | REAL DEFAULT 0 | 공휴일 근무시간 |
| holiday_bonus | INTEGER DEFAULT 0 | 공휴일 수당 (시간×3000) |
| base_salary | INTEGER DEFAULT 0 | 기본급 (총시간×시급) |
| meal_allowance | INTEGER DEFAULT 0 | 식비 |
| taxi_allowance | INTEGER DEFAULT 0 | 택시비 |
| gross_salary | INTEGER DEFAULT 0 | 총 급여 |
| tax_33 | INTEGER DEFAULT 0 | 3.3% 세금 |
| net_salary | INTEGER DEFAULT 0 | 실수령액 |
| is_manual_edit | BOOLEAN DEFAULT 0 | 수기 수정 여부 |
| created_at | DATETIME | |
| updated_at | DATETIME | |

**인덱스**: `(employee_id, year_month)` UNIQUE

---

## 9. handovers (인수인계)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| author_name | TEXT NOT NULL | 작성자 이름 |
| content | TEXT NOT NULL | 인수인계 내용 |
| created_at | DATETIME | 작성 시각 |
| updated_at | DATETIME | |

---

## 10. handover_comments (인수인계 댓글)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| handover_id | INTEGER FK | handovers.id |
| parent_comment_id | INTEGER FK NULL | 대댓글 시 부모 댓글 ID |
| author_name | TEXT NOT NULL | 작성자 |
| content | TEXT NOT NULL | 댓글 내용 |
| created_at | DATETIME | |

---

## 11. inventory (물품 관리)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| item_name | TEXT NOT NULL | 물품명 |
| required_qty | INTEGER DEFAULT 0 | 보유해야 하는 수량 |
| current_qty | INTEGER DEFAULT 0 | 현재 수량 |
| shortage_qty | INTEGER GENERATED | 부족 수량 (자동계산) |
| category | TEXT | 카테고리 |
| note | TEXT | 비고 |
| updated_at | DATETIME | |

---

## 12. holidays (공휴일 캐시)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동증가 |
| holiday_date | DATE NOT NULL UNIQUE | 공휴일 날짜 |
| holiday_name | TEXT | 공휴일 이름 |
| is_weekend | BOOLEAN DEFAULT 0 | 주말 여부 |
| year | INTEGER | 연도 |

---

## 13. app_config (앱 설정)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| key | TEXT PK | 설정 키 |
| value | TEXT | 설정 값 |
| updated_at | DATETIME | |

**기본 설정 키**:
- `admin_password_hash`: 관리자 비밀번호 해시
- `web_login_id`: cubeescape 로그인 ID (암호화)
- `web_login_pw`: cubeescape 로그인 PW (암호화)
- `default_meal_allowance`: 식비 기본금액
- `taxi_allowance`: 택시비 기본금액 (10000)
- `taxi_cutoff_time`: 택시비 기준 시간 (23:30)
- `holiday_bonus_per_hour`: 공휴일 추가수당 (3000)

---

## ER 다이어그램 (관계)

```
employees ──1:N── schedules
employees ──1:N── attendance
employees ──1:N── salary_records

daily_sales ──1:N── sale_items
reservations ──0:1── sale_items

handovers ──1:N── handover_comments
handover_comments ──0:N── handover_comments (self-ref: 대댓글)
```
