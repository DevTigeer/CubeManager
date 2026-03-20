# CubeManager UI 트렌드 가이드 2025-2026

> WinUI 3 / Fluent Design System / Windows 11 감성을
> WinForms + GDI+ 환경에서 성능 저하 없이 구현하는 디자인 가이드
>
> 최종 갱신: 2026-03-20

---

## 1. 디자인 컨셉

### 1.1 한 줄 정의

> **"Windows 11 네이티브 감성의 생산성 데스크톱 앱"**
> — Fluent Design의 절제된 깊이감과 카드형 레이아웃을 WinForms GDI+로 재현

### 1.2 핵심 키워드

```
① Clean Density     — 정보 밀도 높되 시각적으로 깨끗
② Soft Depth        — 1~2px 그림자 + 미세한 레이어 분리
③ Rounded Cards     — 8px 라운드 카드가 기본 단위
④ Restrained Color  — 무채색 기반 + 포인트 컬러 최소 사용
⑤ Adaptive Layout   — 창 크기에 따라 사이드바/콘텐츠 비율 조정
⑥ Dark-Ready        — 라이트/다크 즉시 전환 가능한 색상 구조
```

### 1.3 레퍼런스 앱

```
Microsoft Teams 2.0    — 카드 레이아웃, NavigationView
Windows 11 Settings    — Mica 배경, 둥근 카드
Notion Desktop         — 깨끗한 정보 계층, 미니멀 사이드바
Arc Browser            — 글래스모피즘 포인트, 절제된 색상
VS Code                — 고밀도 정보, 다크 모드, 생산성
```

---

## 2. WinForms에서의 Fluent Design 적용 전략

### 2.1 적용 가능 vs 불가능 판단표

| WinUI 3 / Fluent 요소 | WinForms 적용 | 방법 | 성능 영향 |
|----------------------|:---:|------|:---:|
| 둥근 모서리 (8px) | ✅ | GraphicsPath + Region | 없음 |
| 카드형 레이아웃 | ✅ | Panel + OnPaint border | 없음 |
| 1px 미세 그림자 | ✅ | OnPaint DrawLine (1px 회색) | 없음 |
| 다크 모드 전환 | ✅ | ColorPalette.IsDark 분기 | 없음 |
| 호버 색상 전환 | ✅ | MouseEnter/Leave + BackColor | 없음 |
| NavigationView 스타일 | ✅ | 기존 SideNavPanel 개선 | 없음 |
| Segoe Fluent Icons | ✅ | 시스템 폰트 (Win11 기본 탑재) | 없음 |
| 반응형 레이아웃 | ✅ | Resize 이벤트 + 비율 계산 | 없음 |
| Mica 재질 | ⚠️ | 단색 근사 (#F3F3F3 / #202020) | 없음 |
| Acrylic 블러 | ❌ | **제외** — DWM API 필요, 저사양 불가 | 높음 |
| Reveal Highlight | ❌ | **제외** — 마우스 위치 추적 렌더링 | 중간 |
| 실시간 애니메이션 | ❌ | **제외** — Timer 기반 프레임 드로잉 | 높음 |
| 3D 요소 / Depth | ⚠️ | 1~2px 오프셋 그림자로 대체 | 없음 |
| 글래스모피즘 | ⚠️ | 반투명 아닌 Light 컬러 배경으로 근사 | 없음 |

### 2.2 우리의 접근: "Fluent Lite"

```
실제 Fluent Design     →   CubeManager "Fluent Lite"
───────────────────────────────────────────────────
Mica 배경              →   Mica 근사 단색 (#F5F7FA / #1A1D29)
Acrylic 블러           →   반투명 없이 Light 색상 배경
Reveal Highlight       →   단순 hover 배경색 변경
둥근 모서리 8px        →   GraphicsPath Region (동일)
NavigationView         →   SideNavPanel GDI+ (동일 구조)
InfoBar                →   ToastNotification (동일 패턴)
```

---

## 3. 색상 시스템 업데이트

### 3.1 라이트 모드 (현행 유지 + 미세 조정)

```
카테고리          토큰명              HEX        변경사항
─────────────────────────────────────────────────────
배경 Layer 0     Mica               #F5F7FA    유지 (Windows 11 Settings 느낌)
배경 Layer 1     Surface            #FFFFFF    유지
배경 Layer 2     Card               #FFFFFF    유지
테두리           Border             #E8ECF1    유지
구분선           Divider            #F0F0F0    유지

Primary          Primary            #1976D2    유지
                 PrimaryHover       #1565C0    유지 (Primary700)

텍스트           Text               #1A1A2E    유지
                 TextSecondary      #6B7280    유지
                 TextTertiary       #9CA3AF    유지

⚡ 신규 추가
깊이 그림자       ShadowLight        #00000008  (alpha 3%) — 카드 하단 1px
카드 호버        CardHover          #F8F9FB    — 카드 마우스오버
포커스 링        FocusRing          #1976D233  (alpha 20%) — 입력 포커스
서브틀 배경      SubtleBg           #F0F2F5    — 비활성 섹션
```

### 3.2 다크 모드 (신규)

```
카테고리          토큰명              HEX        설명
─────────────────────────────────────────────────────
배경 Layer 0     Mica               #202020    Windows 11 다크 Mica
배경 Layer 1     Surface            #2D2D2D    카드/패널 배경
배경 Layer 2     Card               #383838    Elevated 카드
테두리           Border             #404040    미세한 구분
구분선           Divider            #333333    얇은 분리선

Primary          Primary            #60A5FA    밝은 파랑 (다크에서 가시성)
                 PrimaryHover       #93C5FD    더 밝은 호버

텍스트           Text               #E4E4E7    메인 텍스트
                 TextSecondary      #A1A1AA    보조 텍스트
                 TextTertiary       #71717A    비활성 텍스트

Semantic         Success            #4ADE80    밝은 초록
                 SuccessLight       #052E16    어두운 초록 배경
                 Warning            #FBBF24    밝은 노랑
                 WarningLight       #422006    어두운 노랑 배경
                 Danger             #F87171    밝은 빨강
                 DangerLight        #450A0A    어두운 빨강 배경
                 Info               #60A5FA    밝은 파랑
                 InfoLight          #172554    어두운 파랑 배경

Nav              NavDefault         #71717A    비활성 아이콘
                 NavHover           #A1A1AA    호버 아이콘
                 NavHoverBg         #383838    호버 배경
                 NavActive          #60A5FA    선택됨
                 NavActiveBg        #1E3A5F    선택 배경

Grid             HeaderBg           #2D2D2D    테이블 헤더
                 RowAlt             #333333    교차 행
                 HoverBg            #3B3B3B    행 호버
                 SelectedBg         #1E3A5F    행 선택

그림자           ShadowLight        #00000020  (alpha 12%)
```

### 3.3 다크 모드 구현 방법 (성능 0 영향)

```csharp
// ColorPalette.cs 에 추가
public static bool IsDark { get; set; } = false;

// 모든 색상을 프로퍼티로 전환
public static Color Background => IsDark
    ? ColorTranslator.FromHtml("#202020")
    : ColorTranslator.FromHtml("#F5F7FA");

public static Color Surface => IsDark
    ? ColorTranslator.FromHtml("#2D2D2D")
    : Color.White;

// ... 각 색상마다 Light/Dark 분기
```

**전환 시**: `ColorPalette.IsDark = true` → 모든 컨트롤 `Invalidate()` → 즉시 반영
**성능**: 색상값만 바뀌므로 CPU/메모리 추가 부하 0

---

## 4. 레이아웃 구조 업데이트

### 4.1 메인 구조 (변경 없음, 확인)

```
┌──────────────────────────────────────────────────────────────────┐
│ HeaderPanel (50px) — Mica 근사 배경, 하단 1px Border              │
├───────┬──────────────────────────────────────────────────────────┤
│       │                                                          │
│ Side  │  ContentArea — Background (#F5F7FA)                      │
│ Nav   │                                                          │
│ 60px  │  ┌───────────────────────────────────────────────────┐   │
│  ↕    │  │ RoundedCard (8px radius)                          │   │
│ 200px │  │  콘텐츠...                                         │   │
│       │  └───────────────────────────────────────────────────┘   │
│       │                                                          │
├───────┴──────────────────────────────────────────────────────────┤
│ StatusBar (28px) — Primary900 배경                                │
└──────────────────────────────────────────────────────────────────┘
```

### 4.2 반응형 전략 (신규)

```
창 너비 기준:

≥ 1280px (기본)
  사이드바 60px ↔ 200px hover
  콘텐츠 패딩 16px
  하단 3분할: 40% / 30% / 30%

≥ 1024px ~ 1279px
  사이드바 60px (hover 확장 비활성)
  콘텐츠 패딩 12px
  하단 3분할: 45% / 25% / 30%

< 1024px (최소)
  사이드바 50px
  콘텐츠 패딩 8px
  하단: 스택형 (세로 배치)
```

구현:
```csharp
// MainForm.Resize 이벤트에서 처리
var w = ClientSize.Width;
_sideNav.EnableHoverExpand = w >= 1280;
_sideNav.CollapsedWidth = w < 1024 ? 50 : 60;
```

---

## 5. 주요 컴포넌트 디자인 업데이트

### 5.1 RoundedCard (신규 — 핵심 컴포넌트)

```
2025 트렌드: 모든 콘텐츠가 둥근 카드 안에 위치

┌─────────────────────────────────────┐  ← 8px rounded corner
│                                     │     1px Border (#E8ECF1)
│   콘텐츠                             │     하단 1px ShadowLight
│                                     │     배경 Surface (#FFFFFF)
│                                     │     패딩 16px
└─────────────────────────────────────┘
         ↑ 하단 1px #00000008 (깊이감)
```

구현 (GDI+):
```csharp
public class RoundedCard : Panel
{
    public int Radius { get; set; } = 8;

    public RoundedCard()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = ColorPalette.Surface;
        Padding = new Padding(16);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 2);
        using var path = CreateRoundedPath(rect, Radius);

        // 하단 1px 깊이 그림자 (성능 무영향)
        using var shadowPen = new Pen(Color.FromArgb(8, 0, 0, 0));
        g.DrawLine(shadowPen,
            Radius, Height - 1,
            Width - Radius, Height - 1);

        // 카드 배경
        using var bg = new SolidBrush(ColorPalette.Surface);
        g.FillPath(bg, path);

        // 테두리
        using var border = new Pen(ColorPalette.Border);
        g.DrawPath(border, path);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int r)
    {
        var path = new GraphicsPath();
        var d = r * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
```

**성능**: `GraphicsPath`는 정수 좌표 연산만 하므로 GDI+ 내에서 가장 가벼운 작업.
CPU 0.01ms 미만, 메모리 추가 할당 무시 수준 (Path 객체 ~200바이트).

### 5.2 SummaryCard 업데이트

```
현행:
┌────────────────────────────────┐
│ ◉ (아이콘원)  제목              │  ← 사각형, 직선 테두리
│               ₩1,250,000      │
│               ▲ 12% vs 어제    │
└────────────────────────────────┘

2025 업데이트:
╭────────────────────────────────╮
│                                 │  ← 8px 둥근 모서리
│ ◉  제목                         │     하단 1px 미세 그림자
│    ₩1,250,000                  │     호버 시 CardHover 배경
│    ▲ 12% vs 어제                │
╰────────────────────────────────╯
```

변경점:
- `CreateRoundedPath`로 둥근 모서리 적용
- 하단 1px 미세 그림자 추가
- 마우스 호버 시 배경 `#F8F9FB` 전환 (MouseEnter/Leave)

### 5.3 SideNavPanel 업데이트

```
현행 선택 상태:
┌──────────────────┐
│▐ 📅 예약/매출    │  ← 좌측 3px 바 + 배경 하이라이트
└──────────────────┘

2025 업데이트 (Windows 11 NavigationView 스타일):
┌──────────────────┐
│  ╭──────────────╮│  ← 선택 항목에 Pill 형태 하이라이트
│  │📅 예약/매출   ││     6px rounded, NavActiveBg 배경
│  ╰──────────────╯│     좌측 바 3px → Pill 좌측 3px 둥근 바
└──────────────────┘
```

변경점:
- 선택 인디케이터: 직선 바 → **둥근 Pill 바** (height 16px, radius 2px)
- 배경: 전체 너비 → **양쪽 4px 마진**의 Pill 배경
- 아이콘: 이모지 → **Segoe Fluent Icons** (Win11 기본 폰트)

```
Segoe Fluent Icons 매핑:
📅 → \uE787 (Calendar)
📋 → \uE9D5 (ClipboardList)
💰 → \uE7BF (Money)
📄 → \uE8A5 (Document)
📝 → \uE70B (Edit)
📦 → \uE7B8 (Package)
⏰ → \uE823 (Clock)
🔑 → \uE8D7 (Permissions)
⚙️ → \uE713 (Settings)
🛡️ → \uE83D (Shield)
```

**성능**: `DrawString` 1회 호출의 폰트만 바뀌므로 영향 0.
단, Segoe Fluent Icons는 **Windows 11 전용**이므로 Win10 폴백 필요:

```csharp
private static readonly Font NavIconFont =
    IsFontInstalled("Segoe Fluent Icons")
        ? new Font("Segoe Fluent Icons", 16f)
        : new Font("Segoe MDL2 Assets", 16f);  // Win10 폴백
```

### 5.4 HeaderPanel 업데이트

```
현행:
┌──────────────────────────────────────────────────────────┐
│  CubeManager                           인천구월점  14:32  │
└──────────────────────────────────────────────────────────┘

2025 업데이트 (Mica 근사):
╭──────────────────────────────────────────────────────────╮
│  CubeManager                           인천구월점  14:32  │  ← Surface 배경
│                                                          │     하단 1px Border
╰──────────────────────────────────────────────────────────╯

변경 최소:
- 헤더 배경을 Surface (#FFFFFF / Dark: #2D2D2D)로 유지
- Windows 11의 Mica는 단색으로 근사 (실제 Mica는 DWM 의존)
- 하단 테두리 1px 유지
```

### 5.5 DataGridView 업데이트

```
현행: 직각 테두리, 단순 배경

2025 업데이트:
- 그리드 자체는 변경 최소 (DataGridView는 커스텀 렌더링 한계)
- 그리드를 RoundedCard 안에 배치하여 카드 느낌 추가
- 헤더 행: 글자 크기 9.5f → 10f Bold 유지
- 선택 행: SelectedBg + 좌측 2px Primary 바 (RowPostPaint)
```

```csharp
// GridTheme에 추가: 선택 행 좌측 바
grid.RowPostPaint += (s, e) =>
{
    if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Selected)
    {
        using var brush = new SolidBrush(ColorPalette.Primary);
        e.Graphics.FillRectangle(brush, e.RowBounds.X, e.RowBounds.Y,
            2, e.RowBounds.Height);
    }
};
```

### 5.6 ToastNotification 업데이트

```
현행:
┌─┬────────────────────────────────┐
│▐│  메시지 텍스트                   │  ← 직각, 좌측 4px 바
└─┴────────────────────────────────┘

2025 업데이트:
╭────────────────────────────────────╮
│ ● 메시지 텍스트                     │  ← 8px 둥근 모서리
│                                    │     좌측 바 제거, 아이콘으로 대체
╰────────────────────────────────────╯     하단 2px 미세 그림자
```

변경점:
- 직각 → 8px 둥근 모서리 (`Region` + `GraphicsPath`)
- 좌측 컬러 바 → **아이콘** (✓ ⚠ ✕ ℹ)
- 그림자: 하단 2px `Color.FromArgb(15, 0, 0, 0)`

---

## 6. 다크 모드 전략

### 6.1 원칙

```
① 단순 반전이 아닌 재설계된 색상 팔레트
② 배경은 순수 검정(#000) 사용 금지 — #202020 (Windows 11 기준)
③ 텍스트는 순수 흰색(#FFF) 사용 금지 — #E4E4E7 (눈 피로 감소)
④ Semantic 색상은 채도를 낮추고 밝기를 높임 (다크 위 가독성)
⑤ 카드 배경은 Surface (#2D2D2D), 올림 카드는 Card (#383838)
```

### 6.2 전환 메커니즘

```
사용자 선택 위치: 설정 탭 > 테마 > [라이트 / 다크 / 시스템]

시스템 연동 (Windows 10/11):
  Registry: HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
  Key: AppsUseLightTheme (0 = Dark, 1 = Light)
```

```csharp
// 시스템 테마 감지 (성능 영향 0)
public static bool IsSystemDark()
{
    try
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is 0;
    }
    catch { return false; }
}
```

### 6.3 다크 모드 시각 비교

```
라이트:
╭─────────────────────╮
│ ■ 예약/매출          │  ← Surface #FFF, Text #1A1A2E
│                     │
│  ₩1,250,000        │
╰─────────────────────╯
  배경 #F5F7FA

다크:
╭─────────────────────╮
│ ■ 예약/매출          │  ← Surface #2D2D2D, Text #E4E4E7
│                     │
│  ₩1,250,000        │
╰─────────────────────╯
  배경 #202020
```

### 6.4 다크 모드에서 절대 하지 말 것

```
❌ 순수 검정 배경 (#000000) — 눈 피로, OLED 스미어링
❌ 순수 흰색 텍스트 (#FFFFFF) — 대비 과다
❌ 라이트 모드 Semantic 색상 그대로 사용 — 다크 배경에서 눈 피로
❌ 그림자 강화 — 다크에서 그림자 거의 안 보임, 대신 밝은 테두리로
❌ 투명도 남용 — 다크에서 투명 레이어는 예측 불가 색상 생성
```

---

## 7. AI 통합 방식 (자연스러운 보조)

> 현재 프로젝트에 AI 기능을 강제 삽입하지 않음.
> 단, 아래 패턴은 기존 데이터 기반으로 구현 가능하며 UX를 향상시킴.

### 7.1 스마트 추천 (규칙 기반, ML 불필요)

```
위치: 예약/매출 탭 > 인라인 힌트

패턴:
┌────────────────────────────────────────────────┐
│ 💡 "어제 금요일 대비 예약이 2팀 적습니다"         │  ← SubtleBg 배경
│    "지난 3주 같은 요일 평균: 7팀"                 │     TextTertiary 색상
└────────────────────────────────────────────────┘

구현: 단순 SQL 집계 (지난 4주 동요일 평균 vs 오늘)
성능: SELECT 1회, 앱 시작 시 1번
```

### 7.2 자동 정리 (배치 작업)

```
- 30일 이전 출퇴근 이력 → 월별 요약으로 압축
- 90일 이전 인수인계 → 아카이브 플래그
- 이미 구현된 DB 백업 정리 (14개 유지)
- 오래된 로그 자동 삭제 (7일, Serilog 내장)

※ ML 모델 로딩 없음, 단순 SQL + 파일 정리
```

### 7.3 스마트 보조 (계산 자동화)

```
이미 구현됨:
✅ 급여 자동 계산 (스케줄 → 시간 → 금액)
✅ 공휴일 자동 동기화 (API → DB)
✅ 현금 잔액 자동 산출 (전일 잔액 + 현금수입 - 현금지출)
✅ 예약 자동 동기화 (웹 스크래핑 → DB)
✅ 취소 예약 자동 감지 (웹 비교)

추가 가능:
- 월말 급여 자동 재계산 알림
- 물품 재고 부족 경고 (설정 임계값 이하 시)
```

---

## 8. 3D / 깊이감 표현 (최소한)

### 8.1 깊이 레벨 시스템

```
Level 0  — 메인 배경 (Mica)
           색상: Background (#F5F7FA)
           깊이: 없음

Level 1  — 카드/패널 (Surface)
           색상: Surface (#FFFFFF)
           깊이: 하단 1px Color.FromArgb(8, 0, 0, 0)

Level 2  — 올림 요소 (토스트, 드롭다운, 다이얼로그)
           색상: Surface (#FFFFFF)
           깊이: 하단+우측 2px Color.FromArgb(15, 0, 0, 0)

Level 3  — 없음 (과한 3D 금지)
```

### 8.2 미세 그림자 구현 (성능 안전)

```csharp
// Level 1: 카드 하단 1px (DrawLine 1회)
using var shadow = new Pen(Color.FromArgb(8, 0, 0, 0));
g.DrawLine(shadow, rect.Left + r, rect.Bottom, rect.Right - r, rect.Bottom);

// Level 2: 토스트/다이얼로그 2px (DrawLine 2회)
using var shadow1 = new Pen(Color.FromArgb(12, 0, 0, 0));
using var shadow2 = new Pen(Color.FromArgb(6, 0, 0, 0));
g.DrawLine(shadow1, rect.Left + r, rect.Bottom, rect.Right - r, rect.Bottom);
g.DrawLine(shadow2, rect.Left + r, rect.Bottom + 1, rect.Right - r, rect.Bottom + 1);
```

**성능**: `DrawLine`은 GDI+ 최경량 연산. 1000회 호출해도 1ms 미만.

### 8.3 글래스모피즘 포인트 (절제)

```
사용 위치: SummaryCard 아이콘 원형 배경에만

구현: 반투명이 아닌 Light 색상의 원형 배경
  라이트: AccentBlue.Light (#E3F2FD) — 이미 구현됨
  다크: Color.FromArgb(30, 96, 165, 250) — 반투명 원형

※ 실제 글래스 효과(블러)는 사용하지 않음.
  "글래스처럼 보이는" 밝은 반투명 색상으로 근사.
```

---

## 9. WinForms 적합 컨트롤 매핑

### 9.1 WinUI 3 → WinForms 대응표

| WinUI 3 컨트롤 | WinForms 대체 | 비고 |
|----------------|--------------|------|
| NavigationView | **SideNavPanel** (GDI+) | 이미 구현, Pill 스타일 업그레이드 |
| InfoBar | **ToastNotification** | 이미 구현, 둥근 모서리 추가 |
| Card | **RoundedCard** (Panel) | 신규 — 8px radius |
| NumberBox | NumericUpDown + 스타일링 | WinForms 네이티브 |
| DatePicker | DateTimePicker + 스타일링 | WinForms 네이티브 |
| DataGrid | DataGridView + GridTheme | 이미 구현 |
| CommandBar | FlowLayoutPanel + 버튼 | 이미 사용 중 |
| ContentDialog | Form + DialogResult | 이미 사용 중 |
| ToggleSwitch | CheckBox + Owner Draw | 필요 시 구현 |
| ProgressRing | 사용 안 함 | 저사양에서 불필요 |
| TeachingTip | 사용 안 함 | 복잡도 대비 가치 낮음 |
| TreeView | TreeView (네이티브) | 필요 시 사용 |

### 9.2 입력 컨트롤 스타일링

```
2025 트렌드: 밑줄형 입력 (Underline Input)

현행 TextBox:
┌──────────────────┐
│ 입력값            │  ← 4방향 테두리
└──────────────────┘

2025 스타일:
  입력값
──────────────────   ← 하단 1px만 (Border 색상)
                     포커스 시 하단 2px Primary

구현:
- TextBox.BorderStyle = None
- 부모 Panel에서 OnPaint로 하단 선만 그리기
- 또는 TextBox를 그대로 쓰되, FlatStyle 적용
```

---

## 10. 피해야 할 구식 요소

### 10.1 확실히 제거할 것

| 구식 요소 | 이유 | 대체 |
|----------|------|------|
| 그래디언트 배경 | 2010년대, Windows Vista 느낌 | 단색 배경 |
| 3D 돌출 버튼 | Windows XP 스타일 | FlatStyle.Flat |
| 굵은 테두리 (2px+) | 무거운 인상 | 1px 미세 테두리 |
| 탭 컨트롤 상단 탭 | 구식 MDI 느낌 | 사이드 네비게이션 ✅ |
| 아이콘 + 텍스트 밑줄 메뉴바 | 전통 Win32 | 플랫 버튼 |
| 화려한 색상 조합 (5색+) | 산만함 | Primary + 회색 + Semantic 포인트 |
| 과도한 이모지 아이콘 | 비전문적 인상 | Segoe Fluent Icons 또는 최소 이모지 |
| StatusBar에 다수 패널 | 정보 과잉 | 앱 이름 + 버전만 |
| 그림자 3px+ | 부자연스러움, 성능 | 1~2px 미세 그림자 |
| 풀 컬러 배경 행 | 눈 피로 | 좌측 컬러 바 or 태그 |

### 10.2 주의해서 사용할 것

| 요소 | 조건 | 사용 범위 |
|------|------|----------|
| 이모지 | 네비게이션 아이콘에만 | Win10 폴백 용 |
| Bold 텍스트 | 제목/헤더/강조 값만 | 본문에 Bold 금지 |
| 색상 배경 셀 | Payment 태그/상태만 | 전체 행 색칠 금지 |
| Divider 선 | 논리적 섹션 구분만 | 모든 행 사이 금지 (Grid 제외) |
| 모달 다이얼로그 | 삭제 확인/입력 폼만 | 안내 메시지는 Toast |

---

## 11. 타이포그래피 스케일 (업데이트)

```
역할              폰트                크기    굵기      색상
────────────────────────────────────────────────────────
페이지 제목        맑은 고딕            16f    Bold      Text
섹션 제목          맑은 고딕            12f    Bold      TextSecondary
본문               맑은 고딕            10f    Regular   Text
보조 텍스트        맑은 고딕            9.5f   Regular   TextSecondary
캡션/힌트          맑은 고딕            9f     Regular   TextTertiary
통계 카드 값       맑은 고딕            18f    Bold      Text
통계 카드 라벨     맑은 고딕            9f     Regular   TextSecondary
네비게이션 라벨    맑은 고딕            10.5f  Regular   NavDefault
네비게이션 선택    맑은 고딕            10.5f  Bold      NavActive
버튼 텍스트        맑은 고딕            10f    Regular   (컨텍스트별)
그리드 헤더        맑은 고딕            10f    Bold      TextSecondary
그리드 데이터      맑은 고딕            10f    Regular   Text

⚡ 신규: 아이콘 폰트
네비 아이콘         Segoe Fluent Icons  16f    Regular   NavDefault/Active
(Win10 폴백)       Segoe MDL2 Assets   16f    Regular   NavDefault/Active
```

---

## 12. 적용 우선순위

### Phase A: 즉시 적용 (성능 영향 0)

```
□ ColorPalette에 다크 모드 색상 추가 + IsDark 스위치
□ RoundedCard 컴포넌트 생성
□ SummaryCard에 둥근 모서리 + 미세 그림자 적용
□ GridTheme에 선택 행 좌측 바 추가 (RowPostPaint)
□ SideNavPanel 선택 인디케이터 Pill 스타일 변경
□ 설정 탭에 테마 선택 (라이트/다크) 추가
```

### Phase B: 점진적 개선

```
□ ToastNotification 둥근 모서리 적용
□ SideNavPanel 아이콘을 Segoe Fluent Icons로 전환 (Win10 폴백 포함)
□ 반응형 레이아웃 Resize 핸들러 추가
□ HeaderPanel Mica 근사 배경 적용
□ 입력 컨트롤 밑줄형 스타일링 검토
```

### Phase C: 완성도

```
□ 다크 모드 전체 테스트 + 미세 조정
□ 시스템 테마 연동 (Registry 감지)
□ 규칙 기반 스마트 추천 힌트 UI
□ 최종 아이콘/색상 폴리싱
```

---

## 13. 성능 보장 규칙 (절대 위반 금지)

```
① GDI+ 렌더링은 반드시 DoubleBuffered = true
② OnPaint 안에서 new Font() / new Brush() 반복 생성 금지
   → static readonly 또는 클래스 필드로 캐싱
③ 그림자는 DrawLine (1~2px)만 — DrawImage/Blur 금지
④ 둥근 모서리는 GraphicsPath — WS_EX_COMPOSITED 금지
⑤ 색상 전환(다크 모드)은 Invalidate()만 — 컨트롤 재생성 금지
⑥ 애니메이션 없음 — 즉시 전환만 (Timer 기반 프레임 금지)
⑦ 투명도는 Color.FromArgb만 — TransparencyKey / Opacity 금지
⑧ 커스텀 폰트 로딩 금지 — 시스템 폰트만 (맑은 고딕, Segoe)
```

---

## 14. 요약: Before → After

```
Before (현행):
  ✅ 색상 시스템 잘 구축됨
  ✅ 카드 기반 레이아웃
  ✅ 사이드 네비게이션
  ⬜ 직각 모서리
  ⬜ 깊이감 없음 (완전 플랫)
  ⬜ 라이트 모드만
  ⬜ 이모지 아이콘
  ⬜ 정적 레이아웃

After (2025 업데이트):
  ✅ 색상 시스템 유지 + 다크 모드
  ✅ 카드 기반 레이아웃 → 둥근 카드
  ✅ 사이드 네비게이션 → Pill 스타일
  ✅ 8px 둥근 모서리
  ✅ 미세 깊이감 (1~2px 그림자)
  ✅ 라이트 + 다크 모드
  ✅ Segoe Fluent Icons (이모지 폴백)
  ✅ 반응형 레이아웃
  ✅ 생산성 앱 인상 (정돈, 절제)

성능 변화: 0 (동일)
```
