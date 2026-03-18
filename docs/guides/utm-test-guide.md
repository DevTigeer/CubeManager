# UTM Windows VM 테스트 가이드 (Mac)

## 준비물

- Mac (Apple Silicon M1/M2/M3/M4)
- 디스크 여유공간 약 40GB
- 인터넷 연결 (다운로드용)
- 소요시간: 약 40분~1시간

---

## Step 1: UTM 설치

1. https://mac.getutm.app/ 접속
2. **[Download]** 클릭 → UTM.dmg 다운로드
3. DMG 열어서 Applications로 드래그 설치
   - 또는 Mac App Store에서 $9.99로 구매 가능 (기능 동일, 개발자 후원)

---

## Step 2: Windows 11 ARM ISO 다운로드

1. https://www.microsoft.com/software-download/windows11arm64 접속
2. **Windows 11 ARM64** ISO 다운로드 (약 5~6GB)
3. 다운로드 완료까지 대기 (파일명 예: `Win11_24H2_Korean_Arm64.iso`)

> 제품 키 없이 설치 가능 (바탕화면에 워터마크만 표시됨)

---

## Step 3: UTM에서 VM 생성

1. UTM 실행
2. **[+ 새 가상 머신 만들기]** 클릭
3. **[가상화(Virtualize)]** 선택 (에뮬레이션 아님)
4. **[Windows]** 선택
5. 설정:

| 항목 | 설정값 |
|------|--------|
| ISO 이미지 | 다운로드한 Windows 11 ARM ISO 선택 |
| RAM | **4096 MB** (4GB) |
| CPU 코어 | **4** |
| 저장 공간 | **40 GB** |
| 공유 폴더 | 활성화 → Cube/src 폴더 선택 |

6. **[저장]** 클릭

---

## Step 4: Windows 11 설치

1. VM 시작 (▶ 버튼)
2. "Press any key to boot from CD or DVD" → **아무 키나 누르기**
3. 언어: **한국어** 선택 → 다음
4. **[지금 설치]**
5. "제품 키가 없습니다" 클릭
6. **Windows 11 Pro** 선택 → 다음
7. 사용 조건 동의 → 다음
8. **[사용자 지정: Windows만 설치]** → 할당되지 않은 공간 선택 → 다음
9. 설치 진행 (약 10~15분) → 자동 재부팅

### 초기 설정 (OOBE)

10. 국가: 한국 → 다음
11. 키보드: 한국어 → 다음
12. **네트워크 연결 스킵** 방법:
    - `Shift + F10` → 명령 프롬프트 열기
    - `oobe\bypassnro` 입력 → Enter → 재부팅
    - "인터넷에 연결되어 있지 않습니다" → **[제한된 설정으로 계속]**
13. 사용자 이름 입력 (예: `cube`) → 비밀번호는 빈칸으로 건너뛰기 가능
14. 개인 정보 설정 → 모두 거부 → 완료

---

## Step 5: SPICE Guest Tools 설치

1. Windows 바탕화면이 보이면
2. UTM 상단 메뉴 → **CD/DVD** → **SPICE Guest Tools 설치** 팝업이 나타남
3. 또는 수동: VM 설정 → CD/DVD → `spice-guest-tools.iso` 마운트
4. 파일 탐색기에서 CD 드라이브 열기 → `spice-guest-tools-xxx.exe` 실행
5. 설치 완료 후 **재부팅**

> SPICE Tools 설치 후: 해상도 자동 조절, 클립보드 공유, 공유 폴더 접근 가능

---

## Step 6: .NET 8 SDK 설치 (Windows VM 내부)

1. Windows VM에서 Edge 브라우저 열기
2. https://dotnet.microsoft.com/download/dotnet/8.0 접속
3. **SDK** → **Windows ARM64** 다운로드 → 설치
4. 설치 후 **PowerShell** 열기:

```powershell
dotnet --version
# 출력: 8.0.xxx 이면 성공
```

---

## Step 7: 프로젝트 복사 & 실행

### 방법 A: 공유 폴더 사용 (권장)

1. UTM VM 설정에서 공유 폴더를 `Cube/src`로 설정했으면
2. Windows 파일 탐색기에서 **네트워크 드라이브**로 접근 가능
3. 또는 공유 폴더 내용을 `C:\CubeManager\` 로 복사

### 방법 B: 수동 복사

1. Mac에서 `src/` 폴더를 ZIP으로 압축
2. USB 또는 클라우드(Google Drive 등)로 VM에 전달
3. `C:\CubeManager\` 에 압축 해제

### 빌드 & 실행

```powershell
cd C:\CubeManager
dotnet restore CubeManager.sln
dotnet run --project CubeManager
```

---

## Step 8: 테스트 체크리스트

```
□ 앱 시작 → 관리자 비밀번호 설정 다이얼로그
□ 비밀번호 입력 → 메인 화면 표시
□ 8개 탭 전환 정상
□ 상태바 시계 갱신

[설정 탭]
□ 직원 추가 (이름/시급/연락처)
□ 직원 수정 (더블클릭)
□ 활성/비활성 체크박스 토글

[스케줄 탭]
□ [+ 직원 추가] → 직원 선택 → 시간/요일 설정 → 적용
□ 타임테이블에 색상 블록 표시
□ 주차 이동 (◀ ▶)
□ 블록 더블클릭 → 삭제 확인

[예약/매출 탭]
□ 날짜 변경 → 데이터 변경
□ [+ 매출] → 항목/금액/결제수단 입력 → 추가
□ [+ 지출] → 지출 항목 추가
□ 결제 태그 색상 (카드=파랑, 현금=초록, 계좌=노랑)
□ 합계 + 현금 잔액 표시

[급여 탭]
□ [재계산] → 급여 테이블 표시
□ 주차별 시간, 식비, 택시비, 공휴일 수당
□ 3.3% 세금, 실수령액

[출퇴근 탭]
□ 직원 선택 → [출근] 버튼 → 시각 기록
□ 정상=파란색, 지각=빨간색
□ [퇴근] 버튼 → 색상 판정

[업무자료 탭]
□ [+ 새 문서] → MD 파일 생성
□ 편집/저장
□ 삭제 → 휴지통 이동

[인수인계 탭]
□ [새 글 작성] → 작성자/내용 입력
□ 댓글 달기
□ 검색

[물품 탭]
□ [+ 물품 추가]
□ 현재수량 직접 편집 → 부족수량 자동 계산
□ 부족=빨간, 충분=초록

[재시작 테스트]
□ 앱 종료 → 재시작
□ 비밀번호 설정 다이얼로그 안 뜸
□ 기존 데이터 유지 (직원, 스케줄, 매출 등)
□ DB 파일 확인: %APPDATA%\CubeManager\cubemanager.db
```

---

## 문제 해결

### "dotnet을 찾을 수 없습니다"
```powershell
# 환경변수 수동 추가
$env:PATH += ";C:\Program Files\dotnet"
dotnet --version
```

### 앱 실행 시 AntdUI 관련 오류
```powershell
# NuGet 패키지 복원
dotnet restore CubeManager.sln
```

### 공유 폴더가 보이지 않음
- UTM VM 설정 → 공유 → VirtFS 활성화 확인
- SPICE Guest Tools 재설치
- 대안: ZIP으로 수동 복사

### VM이 느림
- UTM 설정 → RAM 6144MB, CPU 4코어로 올리기
- Windows 설정 → 시각 효과 → "최적 성능으로 조정"
