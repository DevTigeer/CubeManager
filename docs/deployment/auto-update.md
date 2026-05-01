# 설치형 배포 및 자동 업데이트

## 배포 원칙

CubeManager는 매장 PC에서 Git을 직접 사용하지 않는다. `main`에 반영된 코드를 GitHub Actions가 설치파일로 빌드하고, 앱은 GitHub Release의 `update.json`만 확인한다.

## 릴리스 생성

```powershell
git tag v0.2.1
git push origin v0.2.1
```

태그가 올라가면 `.github/workflows/release.yml`이 실행된다.

1. Windows 환경에서 `dotnet publish` 실행
2. Inno Setup으로 `CubeManagerSetup-{version}.exe` 생성
3. 설치파일 SHA256 계산
4. `update.json` 생성
5. GitHub Release에 설치파일과 `update.json` 업로드

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
