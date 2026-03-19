# 변경 기록 #001: 웹 자격증명 입력 구조 전환

> 작성일: 2026-03-18
> 구현일: 2026-03-19
> 상태: **구현 완료**

---

## 변경 이유

### 문제

1. cubeescape.co.kr 로그인 ID/PW가 초기 명세 문서에 하드코딩됨 → Git 히스토리 노출 위험
2. 최초 실행 시 웹 자격증명 입력 흐름이 없어 사용자가 설정 탭을 직접 찾아가야 함
3. 웹 자격증명 없이 예약 조회 시도 시 빈 결과만 반환 → UX 혼란

### 목표

```
최초 실행 시: 관리자 비밀번호 설정 → 웹 자격증명 입력 (연결 테스트 포함)
이후 실행 시: 설정 탭에서 변경 가능
스크래핑 시: app_config에서 DPAPI 복호화하여 사용
```

---

## 변경 전 (AS-IS)

```
앱 시작 → DB 초기화 → 관리자 비밀번호 설정 → MainForm
                                              └─ 설정 탭에서 웹 설정 (사용자가 직접 찾아야 함)
                                              └─ 예약 탭에서 웹 조회 → 자격증명 없으면 빈 결과
```

## 변경 후 (TO-BE)

```
앱 시작 → DB 초기화 → 관리자 비밀번호 설정 → ★ 웹 자격증명 설정 → MainForm
                                              ├─ ID/PW 입력
                                              ├─ [연결 테스트] → 성공/실패 표시
                                              ├─ [저장] → DPAPI 암호화 → app_config 저장
                                              └─ [건너뛰기] → 나중에 설정 탭에서 가능
```

---

## 구현 내용

### 1. WebCredentialSetupDialog (신규)

| 항목 | 내용 |
|------|------|
| 파일 | `src/CubeManager/Dialogs/WebCredentialSetupDialog.cs` |
| 역할 | 최초 실행 시 cubeescape.co.kr 로그인 자격증명 입력 |
| 필드 | 아이디(TextBox), 비밀번호(PasswordChar) |
| 버튼 | [연결 테스트], [저장], [건너뛰기] |
| 결과 | DialogResult.OK → 저장 완료, Cancel → 건너뛰기 |

```
┌─────────────────────────────────────────┐
│  웹 연동 설정 (cubeescape.co.kr)         │
│                                          │
│  예약 데이터를 조회하려면                  │
│  cubeescape.co.kr 관리자 계정이 필요합니다.│
│                                          │
│  아이디:   [________________]            │
│  비밀번호: [••••••••••••••••]            │
│                                          │
│  [연결 테스트]    [저장]  [건너뛰기]       │
└─────────────────────────────────────────┘
```

### 2. Program.cs 변경

```csharp
// 최초 실행 흐름
EnsureAdminPassword();       // ① 관리자 비밀번호 (기존)
EnsureWebCredentials();      // ② 웹 자격증명 (신규)
Application.Run(MainForm);   // ③ 메인 화면
```

`EnsureWebCredentials()` 로직:
- `web_login_id`가 비어있으면 → WebCredentialSetupDialog 표시
- 이미 설정되어 있으면 → 건너뜀
- 사용자가 [건너뛰기] 하면 → 앱은 정상 실행 (예약 조회만 비활성)

### 3. 자격증명 흐름 (전체)

```
[입력]
  사용자가 평문 ID/PW 입력
    ↓
[연결 테스트] (선택)
  ReservationScraperService.TestConnectionAsync(id, pw)
  → AngleSharp로 cubeescape.co.kr/bbs/login.php POST
  → /adm/ 접근 가능 여부로 성공 판별
    ↓
[저장]
  CredentialHelper.Encrypt(id) → DPAPI → Base64 → app_config['web_login_id']
  CredentialHelper.Encrypt(pw) → DPAPI → Base64 → app_config['web_login_pw']
    ↓
[사용] (예약 조회 시)
  app_config에서 읽기 → Base64 → DPAPI 복호화 → 평문
  → AngleSharp 로그인 → 예약 테이블 파싱
  → 메모리에서 평문 폐기 (변수 스코프 종료)
```

---

## 보안 체크리스트

```
✅ 평문 ID/PW가 코드/문서 어디에도 없음
✅ app_config에 DPAPI 암호화된 값만 저장
✅ 메모리에서 복호화된 평문은 사용 즉시 폐기
✅ 연결 테스트는 저장 전 임시 값으로 수행
✅ Git 히스토리에 자격증명 없음 (커밋 1f465d2에서 제거)
✅ .gitignore에 *.db 포함
✅ DPAPI는 Windows CurrentUser 스코프 → 다른 PC/계정에서 복호화 불가
```

---

## 관련 파일

| 파일 | 변경 유형 |
|------|----------|
| `src/CubeManager/Dialogs/WebCredentialSetupDialog.cs` | **신규** |
| `src/CubeManager/Program.cs` | 수정 (EnsureWebCredentials 추가) |
| `docs/changes/001-web-credential-refactor.md` | 업데이트 (이 문서) |
