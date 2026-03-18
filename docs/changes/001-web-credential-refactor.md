# 변경 기록 #001: 웹 자격증명 입력 구조 전환

> 작성일: 2026-03-18
> 상태: 계획 (미구현)

---

## 변경 이유

### 문제

초기 명세에서 cubeescape.co.kr의 로그인 ID/PW가 문서에 하드코딩되어 있었다.

```
문제점:
1. 자격증명이 Git 히스토리에 평문으로 노출됨 (수정 완료)
2. ID/PW가 변경되면 코드를 수정해야 함 (유연성 없음)
3. 여러 업장이나 다른 사이트를 사용할 경우 대응 불가
4. 보안 정책(security-policy.md)에서 DPAPI 암호화 저장을 명시했으나
   실제로 입력받는 UI 흐름이 구현되지 않았음
```

### 목표

```
웹 관리자 ID/PW를 하드코딩하지 않고,
설정 탭에서 사용자가 직접 입력 → DPAPI 암호화 → app_config 저장
→ 스크래핑 시 복호화하여 사용하는 구조로 전환
```

---

## 현재 상태 (AS-IS)

### 코드 흐름

```
1. app_config 시드 데이터에 빈 값으로 초기화:
   web_login_id = ""
   web_login_pw = ""

2. 설정 탭 UI에 웹 연동 설정 영역이 아직 없음 (Step 8에서 예정)

3. ReservationScraperService가 아직 미구현
   (Step 5에서 SalesService만 구현, 스크래핑은 TODO)

4. docs에서 하드코딩된 ID/PW 제거 완료 (fix 커밋 1f465d2)
```

### 관련 파일

| 파일 | 현재 상태 |
|------|----------|
| `src/CubeManager.Data/Migrations/V001_InitBase.cs` | web_login_id/pw 빈 값 시드 |
| `src/CubeManager/Forms/SettingsTab.cs` | 직원 관리만 구현, 웹 설정 없음 |
| `docs/integration/cubeescape-scraping.md` | LoginAsync(id, pw) 파라미터로 받는 설계 |
| `docs/policies/security-policy.md` | DPAPI 암호화 정책 명시 |
| `docs/architecture/app-initialization.md` | 시드 데이터에 빈 값 정의 |

---

## 변경 계획 (TO-BE)

### 1. 설정 탭에 웹 연동 설정 UI 추가

```
[설정 탭]
├── 직원 관리 (기존)
└── 웹 연동 설정 (신규)
    ├── 사이트 URL:  [http://www.cubeescape.co.kr]
    ├── 아이디:      [________]  (TextBox)
    ├── 비밀번호:    [••••••••]  (PasswordChar)
    ├── [연결 테스트] 버튼
    └── [저장] 버튼
```

### 2. 저장 흐름

```
사용자가 ID/PW 입력 → [저장] 클릭
  → DPAPI 암호화 (ProtectedData.Protect)
  → Base64 인코딩
  → app_config 테이블에 저장:
      web_login_id = "암호화된_Base64_문자열"
      web_login_pw = "암호화된_Base64_문자열"
```

### 3. 사용 흐름 (스크래핑 시)

```
ReservationScraperService.LoginAsync()
  → app_config에서 web_login_id/pw 읽기
  → Base64 디코딩
  → DPAPI 복호화 (ProtectedData.Unprotect)
  → AngleSharp 폼 서밋에 평문 전달
  → 메모리에서 즉시 폐기 (변수 스코프 내에서만)
```

### 4. 연결 테스트

```
[연결 테스트] 버튼 클릭
  → 입력된 ID/PW로 로그인 시도 (저장 전)
  → 성공: "연결 성공" 토스트 (초록)
  → 실패: "로그인 실패: ID/PW 확인" 토스트 (빨강)
```

### 5. 최초 실행 시

```
앱 최초 실행 → 관리자 비밀번호 설정 후
  → 설정 탭에 웹 연동 설정이 비어있음
  → 예약/매출 탭 접근 시 "웹 연동 설정을 먼저 해주세요" 안내
  → ID/PW 미설정 상태에서도 매출 수동 입력은 가능
```

---

## 구현해야 할 파일

### 신규 생성

| 파일 | 역할 |
|------|------|
| `CubeManager.Core/Helpers/CredentialHelper.cs` | DPAPI 암호화/복호화 헬퍼 |
| `CubeManager.Core/Services/ReservationScraperService.cs` | AngleSharp 웹 스크래핑 |
| `CubeManager.Core/Interfaces/Services/IReservationScraperService.cs` | 인터페이스 |

### 수정

| 파일 | 변경 내용 |
|------|----------|
| `CubeManager/Forms/SettingsTab.cs` | 웹 연동 설정 UI 영역 추가 (하단) |
| `CubeManager/Forms/ReservationSalesTab.cs` | 스크래핑 연동 (조회 버튼) |
| `CubeManager/Program.cs` | ReservationScraperService DI 등록 |
| `CubeManager/CubeManager.csproj` | AngleSharp NuGet 추가 |

---

## CredentialHelper 설계

```csharp
// CubeManager.Core/Helpers/CredentialHelper.cs
using System.Security.Cryptography;
using System.Text;

public static class CredentialHelper
{
    public static string Encrypt(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string encryptedBase64)
    {
        var encrypted = Convert.FromBase64String(encryptedBase64);
        var bytes = ProtectedData.Unprotect(encrypted, null,
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
```

**제약**: DPAPI는 Windows 전용. 암호화한 PC + 사용자 계정에서만 복호화 가능.
→ DB 파일을 다른 PC로 옮기면 ID/PW 재입력 필요 (의도된 보안 동작).

---

## 보안 체크리스트

```
□ 평문 ID/PW가 코드/문서 어디에도 없음
□ app_config에 DPAPI 암호화된 값만 저장
□ 메모리에서 복호화된 평문은 사용 즉시 폐기
□ 연결 테스트는 저장 전 임시 값으로 수행
□ Git 히스토리에 자격증명 없음 (또는 히스토리 재작성 완료)
□ .gitignore에 *.db 포함 (DB 파일 커밋 방지)
```

---

## 우선순위

이 변경은 **Step 5의 웹 스크래핑 완성**에 해당하며,
현재 매출 수동 입력은 동작하므로 긴급도는 중간.

```
구현 시점: 예약 조회 기능이 필요할 때 (Windows 테스트 후)
선행 조건: AngleSharp NuGet 추가, Windows 환경에서 DPAPI 테스트
```
