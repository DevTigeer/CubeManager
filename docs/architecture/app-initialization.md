# 앱 초기화 & 시드 데이터

## 1. 앱 시작 흐름

```
Program.Main()
  │
  ├── 1. DI 컨테이너 구성
  ├── 2. Database.Initialize()         ← PRAGMA 설정
  ├── 3. MigrationRunner.RunAll()      ← 테이블 생성 + 시드 데이터
  ├── 4. 최초 실행 판별
  │      └── app_config에 'admin_password_hash' 없으면 → 최초 실행
  ├── 5. [최초 실행] 관리자 비밀번호 설정 다이얼로그
  ├── 6. [최초 실행] 웹 연동 설정 안내 (선택)
  └── 7. MainForm 실행
```

---

## 2. 마이그레이션별 시드 데이터

### V001_InitBase

```sql
-- employees: 시드 데이터 없음 (사용자가 직접 추가)

-- app_config: 기본값 설정
INSERT OR IGNORE INTO app_config (key, value, updated_at) VALUES
  ('default_meal_allowance', '5000', datetime('now','localtime')),
  ('taxi_allowance', '10000', datetime('now','localtime')),
  ('taxi_cutoff_time', '23:30', datetime('now','localtime')),
  ('holiday_bonus_per_hour', '3000', datetime('now','localtime')),
  ('meal_min_hours', '6', datetime('now','localtime')),
  ('web_base_url', 'http://www.cubeescape.co.kr', datetime('now','localtime')),
  ('web_login_id', '', datetime('now','localtime')),
  ('web_login_pw', '', datetime('now','localtime')),
  ('auto_refresh_enabled', '0', datetime('now','localtime')),
  ('auto_refresh_interval_min', '30', datetime('now','localtime'));

-- admin_password_hash는 최초 실행 시 사용자가 설정
-- web_login_id/pw는 설정 탭에서 사용자가 입력 (DPAPI 암호화)
```

### V002_Schedule — 공휴일 시드

```sql
-- 2026년 한국 공휴일 (고정 공휴일)
INSERT OR IGNORE INTO holidays (holiday_date, holiday_name, is_weekend, year) VALUES
  ('2026-01-01', '신정', 0, 2026),           -- 목요일
  ('2026-03-01', '삼일절', 1, 2026),         -- 일요일 (주말)
  ('2026-03-02', '삼일절 대체휴일', 0, 2026), -- 월요일
  ('2026-05-05', '어린이날', 0, 2026),       -- 화요일
  ('2026-06-06', '현충일', 1, 2026),         -- 토요일 (주말)
  ('2026-08-15', '광복절', 1, 2026),         -- 토요일 (주말)
  ('2026-10-03', '개천절', 1, 2026),         -- 토요일 (주말)
  ('2026-10-05', '개천절 대체휴일', 0, 2026), -- 월요일
  ('2026-10-09', '한글날', 0, 2026),         -- 금요일
  ('2026-12-25', '크리스마스', 0, 2026),     -- 금요일

  -- 2026년 음력 공휴일 (매년 변동, API로 갱신 필요)
  ('2026-02-16', '설날 전날', 0, 2026),      -- 월요일
  ('2026-02-17', '설날', 0, 2026),           -- 화요일
  ('2026-02-18', '설날 다음날', 0, 2026),    -- 수요일
  ('2026-05-24', '부처님오신날', 1, 2026),   -- 일요일 (주말)
  ('2026-05-25', '부처님오신날 대체', 0, 2026), -- 월요일
  ('2026-10-04', '추석 전날', 1, 2026),      -- 일요일 (주말)
  ('2026-10-05', '추석', 0, 2026),           -- 월요일 (개천절 대체와 겹침)
  ('2026-10-06', '추석 다음날', 0, 2026);    -- 화요일

-- is_weekend: 해당 공휴일이 토/일에 해당하면 1
-- 대체휴일은 별도 행으로 등록
```

> 음력 공휴일 날짜는 연도마다 다르므로, 공공데이터포털 API로 자동 갱신한다.
> 오프라인 대비: 위 시드 데이터를 내장.
> 매년 1월에 HolidayService가 해당 연도 공휴일을 API에서 조회하여 DB 갱신.

---

## 3. ConfigRepository

```csharp
// Core/Interfaces/Repositories/IConfigRepository.cs
public interface IConfigRepository
{
    Task<string?> GetAsync(string key);
    Task<int> GetIntAsync(string key, int defaultValue);
    Task SetAsync(string key, string value);
}

// 사용 예시:
var mealAllowance = await _configRepo.GetIntAsync("default_meal_allowance", 5000);
var taxiCutoff = await _configRepo.GetAsync("taxi_cutoff_time") ?? "23:30";
```

---

## 4. 공공데이터포털 API 연동

### 4.1 API 정보

```
서비스명: 한국천문연구원 특일 정보
URL: http://apis.data.go.kr/B090041/openapi/service/SpcdeInfoService
엔드포인트: /getRestDeInfo (공휴일)
인증: API 키 (app_config에 저장)
형식: XML 또는 JSON
```

### 4.2 호출 시점

```
1. 앱 시작 시: 현재 연도 holidays 테이블이 비어있으면 자동 호출
2. 매년 1월 1일 이후 최초 실행 시: 새 연도 공휴일 갱신
3. 설정 탭에서 수동 [공휴일 갱신] 버튼
4. API 실패 시: 내장 시드 데이터 사용 (경고 토스트 표시)
```

### 4.3 API 키 미등록 시

```
API 키가 없으면 API 호출 건너뛰고 내장 데이터만 사용
설정 탭에서 API 키 등록 가능 (선택 사항)
```

---

## 5. 앱 종료 흐름

```
MainForm.FormClosing()
  │
  ├── 1. 편집 모드 활성화 상태면 → 저장 확인
  ├── 2. PRAGMA wal_checkpoint(TRUNCATE)    ← WAL 정리
  ├── 3. PRAGMA optimize                    ← 쿼리 최적화
  ├── 4. 자동 백업 실행
  │      └── SQLite Backup API로 backups/ 에 복사
  │      └── 3일 초과 백업 파일 삭제
  ├── 5. 업무자료 휴지통 정리 (30일 초과 파일 삭제)
  └── 6. 연결 닫기 + 앱 종료
```

---

## 6. 로깅

### 6.1 프레임워크

```
Serilog (NuGet: Serilog, Serilog.Sinks.File)
→ CubeManager 앱 프로젝트에만 설치

출력: 파일 (%APPDATA%/CubeManager/logs/cube-YYYYMMDD.log)
보관: 7일 (자동 롤링)
형식: [시각] [레벨] [소스] 메시지
```

### 6.2 로그 레벨

| 레벨 | 용도 | 예시 |
|------|------|------|
| Error | 예외, 실패 | DB 연결 실패, 웹 스크래핑 타임아웃 |
| Warning | 비정상이지만 복구 가능 | 공휴일 API 실패(캐시 사용), 파싱 일부 실패 |
| Information | 주요 동작 | 앱 시작/종료, 마이그레이션 실행, 급여 계산 완료 |
| Debug | 개발용 상세 | SQL 쿼리, 파라미터 값 (릴리스에서 비활성화) |

### 6.3 감사 로그 (급여/스케줄 수정)

```
급여 수기 수정, 스케줄 수동 변경 등 중요 변경은 별도 감사 로그:
[시각] [AUDIT] 사용자가 {직원명}의 {월} 급여 {필드}를 {이전값}→{새값}으로 수정

저장: 같은 로그 파일에 AUDIT 태그로 구분
```

### 6.4 Program.cs 설정

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        Path.Combine(appDataPath, "logs", "cube-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```
