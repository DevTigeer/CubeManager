# 09. 테마 힌트 관리

> 매장 방탈출 테마의 문제/힌트/정답을 관리하고 JSON Export

---

## 개요

매장에서 운영하는 방탈출 테마(5개)마다 문제·힌트·정답 데이터를 관리.
DB에 저장하고, JSON 파일로 Export하여 외부 힌트 표시 앱에서 사용.

---

## 데이터 구조

### 테마 (themes)

| 필드 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동 증가 |
| theme_name | TEXT | 테마 이름 (예: 집착, 타이타닉) |
| description | TEXT | 테마 설명 (선택) |
| sort_order | INTEGER | 표시 순서 |
| is_active | INTEGER | 활성 여부 (0/1) |

### 힌트 (theme_hints)

| 필드 | 타입 | 설명 |
|------|------|------|
| id | INTEGER PK | 자동 증가 |
| theme_id | INTEGER FK | → themes.id (CASCADE 삭제) |
| hint_code | INTEGER | 4자리 난수 (1000~9999), 테마 내 유니크 |
| question | TEXT | 문제 |
| hint1 | TEXT | 힌트 1 (필수) |
| hint2 | TEXT | 힌트 2 (선택) |
| answer | TEXT | 정답 |
| sort_order | INTEGER | 표시 순서 |

---

## UI 레이아웃

```
┌────────────────────────────────────────────────────────┐
│ 테마 힌트 관리                  [전체 Export] [선택 Export]│
├──────────────┬─────────────────────────────────────────┤
│ [+ 테마 추가] │  테마: {선택된 테마명}      [+ 힌트 추가] │
│              │                                         │
│ ┌──────────┐ │  ┌─────┬──────┬──────┬──────┬────┬───┐ │
│ │ ■ 집착   │ │  │코드 │ 문제 │힌트1 │힌트2 │정답│삭제│ │
│ │  Towering │ │  ├─────┼──────┼──────┼──────┼────┼───┤ │
│ │  장기밀매  │ │  │3847 │ ... │ ... │ ... │ ...│ 🗑│ │
│ │  신데렐라  │ │  │2159 │ ... │ ... │ ... │ ...│ 🗑│ │
│ │  타이타닉  │ │  └─────┴──────┴──────┴──────┴────┴───┘ │
│ └──────────┘ │                                         │
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

- **범위**: 1000 ~ 9999 (4자리)
- **생성**: `Random.Shared.Next(1000, 10000)`
- **유니크**: 같은 테마 내 중복 불가 (`UNIQUE INDEX(theme_id, hint_code)`)
- **자동 재생성**: 추가 시 중복이면 새 난수 자동 할당
- **수동 변경**: 다이얼로그에서 직접 수정 가능

---

## JSON Export

### 전체 Export
활성 테마 전체 + 소속 힌트를 하나의 JSON 파일로 저장.

### 선택 Export
현재 선택된 테마만 Export.

### 형식
```json
{
  "exportedAt": "2026-03-19T15:30:00",
  "themes": [
    {
      "themeName": "집착",
      "description": "공포 테마",
      "hints": [
        {
          "hintCode": 3847,
          "question": "첫 번째 문제",
          "hint1": "힌트 1",
          "hint2": "힌트 2",
          "answer": "정답"
        }
      ]
    }
  ]
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
