# 09. 테마 힌트 관리

> 매장 방탈출 테마의 단서/풀이/정답을 관리하고 원본 템플릿 JSON 형식으로 Import/Export

---

## 개요

매장에서 운영하는 방탈출 테마(5개)마다 단서·풀이·정답 데이터를 관리.
DB에 저장하고, `data/escape_rooms_full.json` 원본 템플릿 형식으로 다시 Export하여 외부 힌트 표시 앱에서 사용.

---

## 데이터 구조

### 테마 (themes)

| 필드 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동 증가 |
| theme_key | TEXT | JSON 루트 키/테마 키 (예: obsession) |
| theme_name | TEXT | 테마 이름 (예: 집착, 타이타닉) |
| description | TEXT | 테마 설명 (선택) |
| bg_color | TEXT | JSON `bg` 배경색 |
| accent_color | TEXT | JSON `accent` 강조색 |
| icon | TEXT | JSON `icon` |
| code_prefix | TEXT | 힌트 코드 접두사 (예: a, c, t) |
| sort_order | INTEGER | 표시 순서 |
| is_active | INTEGER | 활성 여부 (0/1) |

### 힌트 (theme_hints)

| 필드 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동 증가 |
| theme_id | INTEGER FK | → themes.id (CASCADE 삭제) |
| hint_code | INTEGER | 1~9999 숫자 코드, 표시/Export 시 4자리로 채움 |
| question | TEXT | JSON `idea` (단서) |
| hint1 | TEXT | JSON `solution` (풀이) |
| hint2 | TEXT | 추가 힌트 (수기 입력용, 원본 JSON Export에서는 제외) |
| answer | TEXT | 정답 |
| sort_order | INTEGER | 표시 순서 |

---

## UI 레이아웃

```
┌────────────────────────────────────────────────────────┐
│ 테마 힌트 관리   [JSON 불러오기] [전체 Export] [선택 Export]│
├──────────────┬─────────────────────────────────────────┤
│ [+ 테마 추가] │  테마: {선택된 테마명}      [+ 힌트 추가] │
│              │                                         │
│ ┌──────────┐ │  ┌─────┬──────┬──────┬──────┬────┬───┐ │
│ │ ■ 집착   │ │  │코드 │ 단서 │풀이  │추가  │정답│삭제│ │
│ │  Towering │ │  ├─────┼──────┼──────┼──────┼────┼───┤ │
│ │  장기밀매  │ │  │3847 │ ... │ ... │ ... │ ...│ 🗑│ │
│ │  신데렐라  │ │  │2159 │ ... │ ... │ ... │ ...│ 🗑│ │
│ │  타이타닉  │ │  └─────┴──────┴──────┴──────┴────┴───┘ │
│ └──────────┘ │  [단서 미리보기] [풀이 미리보기] [정답] │
└──────────────┴─────────────────────────────────────────┘
```

### 좌측 (250px): 테마 목록
- [+ 테마 추가] 버튼
- 카드 형태 목록, 클릭으로 선택 (선택 시 Primary 강조)
- 우클릭: 수정 / 삭제 컨텍스트 메뉴

### 우측: 힌트 그리드
- 선택된 테마의 힌트 목록 (DataGridView)
- 더블클릭 → 힌트 수정 다이얼로그
- 삭제 버튼 컬럼

---

## 힌트코드 규칙

- **범위**: 1 ~ 9999
- **표시/Export**: 4자리로 패딩 후 테마 접두사와 조합 (`1` → `a0001`)
- **생성**: `Random.Shared.Next(1, 10000)`
- **유니크**: 같은 테마 내 중복 불가 (`UNIQUE INDEX(theme_id, hint_code)`)
- **자동 재생성**: 추가 시 중복이면 새 난수 자동 할당
- **수동 변경**: 다이얼로그에서 직접 수정 가능

---

## JSON Export

### JSON 불러오기

`escape_rooms_full.json` 같은 원본 템플릿 JSON을 불러온다. 테마는 `theme_key` 또는 이름으로 매칭해 갱신하고, 힌트는 접두사를 제외한 숫자 코드로 매칭해 갱신한다.

### 전체 Export
활성 테마 전체 + 소속 힌트를 원본 템플릿 구조의 하나의 JSON 파일로 저장.

### 선택 Export
현재 선택된 테마만 Export.

### 형식
```json
{
  "obsession": {
    "key": "obsession",
    "name": "집착",
    "bg": "#111111",
    "accent": "#d43f3a",
    "icon": "...",
    "codePrefix": "a",
    "hintMap": {
      "a0001": {
        "idea": "단서 내용",
        "solution": "풀이 내용",
        "answer": "정답"
      }
    }
  }
}
```

---

## 파일 구조

```
Core/Models/Theme.cs
Core/Models/ThemeHint.cs
Core/Interfaces/Repositories/IThemeRepository.cs
Core/Interfaces/Services/IThemeExportService.cs
Core/Services/ThemeExportService.cs
Data/Repositories/ThemeRepository.cs
Data/Migrations/V007_ThemeHints.cs
CubeManager/Forms/ThemeHintTab.cs
CubeManager/Dialogs/ThemeEditDialog.cs
CubeManager/Dialogs/HintEditDialog.cs
```
