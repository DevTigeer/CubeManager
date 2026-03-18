# 보안 정책

## 1. 관리자 비밀번호

### 1.1 저장

```
방식: BCrypt 해싱 (BCrypt.Net-Next)
저장: app_config 테이블 (key = 'admin_password_hash')
평문 저장 금지
```

```csharp
// 해싱
var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

// 검증
var isValid = BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
```

### 1.2 인증 캐시

```
인증 성공 후 5분간 재인증 불필요
5분 경과 또는 앱 비활성화 시 캐시 만료
편집 모드 진입 시마다 캐시 유효성 확인
```

### 1.3 적용 범위

| 기능 | 관리자 인증 필요 |
|------|----------------|
| 스케줄 편집 모드 | O |
| 급여 수기 수정 | O |
| 직원 삭제 | O |
| 설정 변경 | O |
| 다른 사람 글 삭제 (인수인계) | O |
| 일반 조회 | X |
| 매출 입력 | X |
| 출퇴근 버튼 | X |
| 인수인계 글 작성 | X |

---

## 2. 웹 로그인 자격증명

### 2.1 저장

```
cubeescape.co.kr 로그인 ID/PW
저장 위치: app_config 테이블
암호화: Windows DPAPI (ProtectedData 클래스)
```

```csharp
// 암호화
var encrypted = ProtectedData.Protect(
    Encoding.UTF8.GetBytes(plainText),
    null,
    DataProtectionScope.CurrentUser);
var base64 = Convert.ToBase64String(encrypted);

// 복호화
var bytes = ProtectedData.Unprotect(
    Convert.FromBase64String(base64),
    null,
    DataProtectionScope.CurrentUser);
var plainText = Encoding.UTF8.GetString(bytes);
```

- `DataProtectionScope.CurrentUser`: 현재 Windows 사용자만 복호화 가능
- 다른 PC/사용자에서 DB 파일을 가져가도 복호화 불가

### 2.2 전송

```
HTTP(평문) 전송이지만 사이트 자체가 HTTP이므로 불가피
AngleSharp 폼 서밋으로 전송 (수동 URL 조합 금지)
```

---

## 3. SQL Injection 방지

```
모든 SQL은 파라미터 바인딩 사용 (Dapper @param)
문자열 연결/보간으로 SQL 작성 절대 금지
사용자 입력이 SQL에 직접 들어가는 경로 없어야 함
```

```csharp
// ✅ 안전
"WHERE name LIKE @Search", new { Search = $"%{keyword}%" }

// ❌ 위험
$"WHERE name LIKE '%{keyword}%'"
```

---

## 4. 파일 접근

```
MD 업무자료: data/documents/ 하위만 접근 허용
경로 탈출 방지: Path.GetFullPath() 후 기준 디렉토리 내인지 확인
```

```csharp
var fullPath = Path.GetFullPath(requestedPath);
var baseDir = Path.GetFullPath("data/documents");
if (!fullPath.StartsWith(baseDir))
    throw new UnauthorizedAccessException("허용되지 않은 경로");
```

---

## 5. 앱 설정 보호

```
cubemanager.db 파일 자체는 암호화하지 않음 (SQLite 암호화는 유료)
대신:
- 민감 데이터(비밀번호 해시, 웹 PW)만 개별 암호화
- DB 파일 위치: %APPDATA% (일반 사용자 접근 가능하지만 숨김 폴더)
- 백업 파일도 동일한 보호 수준
```
