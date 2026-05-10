# 변경 기록 #006: 텔레그램 봇 `/명령어` alias 추가

> 작성일: 2026-05-10
> 상태: 완료
> 적용 버전: v0.3.18

---

## 변경 요약

| # | 버전 | 변경 | 설명 |
|---|------|------|------|
| 1 | v0.3.18 | **`/명령어` alias 추가** | `HelpCommandHandler.Aliases` 에 `"명령어"` 추가. 기존 `/help`, `/start`, `/도움말` 과 동일하게 등록된 모든 봇 명령을 알파벳순으로 출력. |

---

## 배경

사용자가 한국어 환경에서 직관적으로 `/명령어` 를 입력했을 때 명령 목록이 조회되지 않고 "지원하지 않는 명령" 메시지만 떴다. 기존에 같은 역할의 한국어 alias `/도움말` 이 있었지만 인지도 낮음.

## 수정

`src/CubeManager.Telegram/Commands/HelpCommandHandler.cs:14`

```csharp
// Before
public IReadOnlyList<string> Aliases => new[] { "start", "도움말" };

// After
public IReadOnlyList<string> Aliases => new[] { "start", "도움말", "명령어" };
```

`CommandRouter` 가 생성자에서 `Command` 와 모든 `Aliases` 를 `_byName` Dictionary 에 등록하므로 코드 추가 없이 즉시 동작.

## 영향 범위

- 등록 가능 입력: `/help`, `/start`, `/도움말`, `/명령어` (모두 동일 핸들러)
- 출력 내용: `IServiceProvider.GetServices<ICommandHandler>()` 로 조회된 모든 핸들러를 `Command` 알파벳순 정렬 후 `/{command} — {description}` 형식으로 한 메시지에 출력
