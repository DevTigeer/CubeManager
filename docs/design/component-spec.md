# CubeManager 컴포넌트 명세

> 각 UI 컴포넌트의 정확한 크기, 색상, 위치 명세
> design-system.md의 구체적 구현 사양

---

## 1. SideNavPanel (사이드 네비게이션)

### 1.1 속성

```
위치: MainForm 좌측
접힘 폭: 60px
펼침 폭: 200px (hover)
높이: Form 전체
배경: #FFFFFF
우측 border: 1px #E8ECF1
```

### 1.2 네비게이션 아이템

```
구조:
  ┌──────────────────┐
  │ [icon 24x24]     │  접힘
  │ [icon] 텍스트     │  펼침
  └──────────────────┘

높이: 48px
아이콘 위치: 좌측 18px 중앙
텍스트 위치: 좌측 54px 중앙 (펼침 시)
폰트: 맑은 고딕 11px Regular

상태별 스타일:
  기본:     아이콘 #9CA3AF, 배경 투명
  hover:    아이콘 #6B7280, 배경 #F5F7FA
  선택:     아이콘 #1976D2, 배경 #E3F2FD, 좌측 3px #1976D2 바
  설정 버튼: 하단 고정 (Dock.Bottom)
```

### 1.3 로고 영역

```
높이: 56px
내용: "C" 또는 로고 아이콘 (접힘) / "CubeManager" (펼침)
폰트: 16px Bold, #1976D2
하단 border: 1px #E8ECF1
```

---

## 2. SummaryCard (통계 카드)

### 2.1 구조

```
┌─────────────────────────────┐
│ [●] 라벨                     │  ← 12px Regular #6B7280
│                              │
│ ₩ 1,250,000                 │  ← 22px Bold #1A1A2E
│                              │
│ ▲ 12% vs 어제                │  ← 11px Regular (Success/Danger)
└─────────────────────────────┘
```

### 2.2 속성

```
크기: 가로 FlexGrow (4등분), 세로 100px
배경: #FFFFFF
모서리: 8px (AntdUI Panel.Radius)
border: 1px #E8ECF1
패딩: 16px
간격: 카드 간 16px

아이콘 원형:
  크기: 36x36
  배경: AccentColor의 Light 변형
  아이콘: 20x20, AccentColor

값 포맷:
  금액: ₩{N:N0} (천단위 콤마)
  건수: {N}건
  시간: {N:F1}h
  인원: {N}명
```

### 2.3 변동 표시 (SubText)

```
증가: ▲ {N}% — 글자색 #4CAF50 (Success)
감소: ▼ {N}% — 글자색 #F44336 (Danger)
동일: — {text} — 글자색 #9CA3AF
```

---

## 3. DataGridView 테마

### 3.1 공통 설정

```csharp
BorderStyle = BorderStyle.None;
BackgroundColor = Color.White;
GridColor = Color.FromArgb(240, 240, 240);
CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
RowHeadersVisible = false;
EnableHeadersVisualStyles = false;
AllowUserToResizeRows = false;
SelectionMode = DataGridViewSelectionMode.FullRowSelect;
```

### 3.2 헤더 행

```
배경: #F8FAFC
글자: 10px Bold, #6B7280
높이: 36px
패딩: 좌우 8px
정렬: 기본 MiddleLeft (금액은 MiddleRight)
```

### 3.3 데이터 행

```
배경 (짝수): #FFFFFF
배경 (홀수): #FAFBFC
글자: 10px Regular, #1A1A2E
높이: 40px
패딩: 좌우 8px, 상하 4px

선택 행:
  배경: #E3F2FD
  글자: #1A1A2E (변경 없음)

hover 행:
  배경: #F0F7FF (구현 시 CellMouseEnter 이벤트)

수기 수정 셀:
  배경: #FFF9C4
```

### 3.4 결제 태그 셀 (커스텀 렌더링)

```
DataGridView.CellPainting 이벤트에서 처리

태그 크기: TextWidth + 좌우 12px, 높이 22px
태그 모서리: 11px (pill)
태그 배경/글자: PaymentTag 색상 참조
태그 위치: 셀 중앙
```

---

## 4. 버튼

### 4.1 Primary Button

```
배경: #1976D2
글자: #FFFFFF, 10px SemiBold
크기: 높이 36px, 최소 폭 80px, 패딩 좌우 24px
모서리: 6px
hover: #1565C0
pressed: #0D47A1
disabled: #B0BEC5, 글자 #FFFFFF
```

### 4.2 Secondary Button (Outline)

```
배경: #FFFFFF
글자: #1976D2, 10px SemiBold
크기: 높이 36px, 최소 폭 80px
border: 1px #1976D2
모서리: 6px
hover: 배경 #E3F2FD
```

### 4.3 Danger Button

```
배경: #FFFFFF
글자: #F44336, 10px SemiBold
border: 1px #F44336
hover: 배경 #FFEBEE
```

### 4.4 Ghost Button (텍스트만)

```
배경: 투명
글자: #6B7280, 10px
hover: 배경 #F5F5F5
크기: 높이 32px
```

### 4.5 날짜 네비게이션 버튼

```
[◀]  [오늘]  [▶]

◀ ▶: 32x32, Ghost 스타일, 10px
오늘: Primary Button, 높이 32px
날짜 표시: 14px Bold, #1A1A2E ("2026-03-19 (수)")
```

---

## 5. 토스트 알림 (기존 유지, 스타일 미세 조정)

```
위치: 화면 우하단 (margin 16px)
크기: 300 x 50px
모서리: 8px
그림자: 없음 (저사양)

Success: 배경 #E8F5E9, 좌측 바 4px #4CAF50, 글자 #2E7D32
Warning: 배경 #FFF3E0, 좌측 바 4px #FF9800, 글자 #E65100
Error:   배경 #FFEBEE, 좌측 바 4px #F44336, 글자 #C62828

지속: 3초
복수: 위로 스택 (60px 간격)
```

---

## 6. 다이얼로그

### 6.1 공통 속성

```
FormBorderStyle: FixedDialog
StartPosition: CenterParent
MaximizeBox: false
MinimizeBox: false
폰트: 맑은 고딕 10f
배경: #FFFFFF
```

### 6.2 크기 기준

```
소형 (인증, 확인):        380 x 220
중형 (직원 편집):         400 x 280
대형 (스케줄 입력):       500 x 450
```

---

## 7. 섹션 헤더 (탭 내부)

```
┌──────────────────────────────────────────────┐
│  예약 현황 (5건)                  [웹 조회]    │
│  ─────────                       ─────────    │
│  제목                              액션 버튼   │
└──────────────────────────────────────────────┘

제목: 14px Bold, #1A1A2E
건수: 14px Regular, #6B7280
버튼: Primary 또는 Secondary
간격: 제목과 콘텐츠 사이 12px
```

---

## 8. 적용 체크리스트

```
□ ColorPalette.cs에 확장 색상 추가
□ GridTheme.ApplyTheme() 유틸리티 메서드 생성
□ SummaryCard 컴포넌트 생성
□ SideNavPanel 컴포넌트 생성
□ 각 탭에 통계 카드 + 섹션 헤더 패턴 적용
□ 토스트 스타일 좌측 바 패턴으로 변경
□ 버튼 스타일 공통화 (ButtonFactory 또는 확장 메서드)
```
