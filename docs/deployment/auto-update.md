# 설치형 배포 및 자동 업데이트

## 배포 원칙

CubeManager는 매장 PC에서 Git을 직접 사용하지 않는다. `main`에 반영된 코드를 GitHub Actions가 설치파일로 빌드하고, 앱은 GitHub Release의 `update.json`만 확인한다.

## 릴리스 생성

### ⚠️ 핵심: 커밋만으로는 릴리스되지 않는다

**워크플로 트리거는 `v*.*.*` 형식의 태그 푸시뿐**이다. 커밋 메시지에 `v0.3.1`을 적거나 main에 push해도 자동 빌드는 일어나지 않는다.

```bash
# 1) csproj <Version> 0.3.0 → 0.3.1 수정 + 코드 변경 작업
git add -A
git commit -m "chore: bump 0.3.1"   # 메시지 형식은 자유. v 표기 불필요
git push                              # ← 여기까지는 main 갱신만, 빌드 없음

# 2) 태그 생성 + 태그 푸시 ← 이 순간 GitHub Actions 시작
git tag v0.3.1
git push origin v0.3.1
```

| 동작 | 효과 |
|---|---|
| `git commit -m "v0.3.1"` | 아무 일도 안 일어남 |
| `git push` | main 코드만 업데이트 |
| `git tag v0.3.1 && git push origin v0.3.1` | **여기서 자동 빌드 + Release 생성** |

태그가 올라가면 `.github/workflows/release.yml`이 다음 순서로 실행된다.

1. Windows 러너에서 `dotnet publish` 실행 (태그 버전을 `-p:Version`으로 주입)
2. Inno Setup으로 `CubeManagerSetup-{version}.exe` 생성
3. 설치파일 SHA256 계산
4. `update.json` 생성 (version, downloadUrl, sha256, releasedAt 등)
5. GitHub Release에 설치파일과 `update.json` 업로드 + `latest` 마킹

### 진행 상황 / 실패 모니터링

`https://github.com/DevTigeer/CubeManager/actions`에서 워크플로 상태 확인. 실패 시 단계별 로그를 보고 재실행.

### 검증 (Release 완료 후)

브라우저로 매니페스트가 떨어지는지 확인.

```text
https://github.com/DevTigeer/CubeManager/releases/latest/download/update.json
```

JSON이 보이고 `version`이 새 태그와 일치하면 자동업데이트 인식 완료.

### 흔한 실수

- **태그 형식**: 반드시 `v0.3.1`처럼 `v` + `MAJOR.MINOR.PATCH`. `0.3.1`이나 `release-0.3.1`은 트리거 안 됨.
- **csproj `<Version>`과 태그 불일치**: 워크플로의 `-p:Version` 주입이 우선이라 빌드는 되지만, 혼동 방지를 위해 둘을 같게 유지할 것.
- **같은 태그 재사용 불가**: 이미 존재하면 `git tag -d v0.3.1 && git push origin :refs/tags/v0.3.1`로 삭제 후 재생성.
- **prerelease 체크**: 워크플로는 `prerelease: false`로 고정. 일부러 prerelease 만들고 싶으면 yml 수정 필요.
- **자동업데이트 코드가 없는 버전 사용자**: 첫 자동업데이트 빌드는 setup.exe를 한 번 직접 전달해야 함(앱이 자기 자신을 업데이트할 수 없음). AppId 동일하므로 DB·설정 보존.

## 앱 업데이트 흐름

```text
CubeManager 실행
→ update_check_enabled 확인
→ update.json 다운로드
→ 현재 Assembly 버전과 최신 버전 비교
→ 새 버전이면 업데이트 다이얼로그 표시
→ 설치파일 다운로드
→ SHA256 검증
→ 설치파일 실행
→ 앱 종료
→ Inno Setup이 프로그램 파일 교체
```

사용자 데이터와 DB는 `%APPDATA%/CubeManager`에 남기고, 설치 업데이트는 프로그램 파일만 교체한다.

## update.json 위치

기본값:

```text
https://github.com/DevTigeer/CubeManager/releases/latest/download/update.json
```

앱 설정값:

```text
update_manifest_url
update_check_enabled
update_last_check_at
```

## 수동 설치파일 생성

Windows PC에서 직접 만들 때:

```powershell
dotnet publish src/CubeManager/CubeManager.csproj `
  -c Release `
  -r win-x64 `
  --no-self-contained `
  -o publish

iscc installer/CubeManager.iss /DAppVersion=0.2.1 /DSourceDir="$pwd\publish"
```

생성물:

```text
dist/CubeManagerSetup-0.2.1.exe
```
