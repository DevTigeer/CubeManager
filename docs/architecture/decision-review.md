# 기술 선택 재검토 보고서

> 기존 결정을 의심하는 관점에서 모든 기술 선택을 재검토한 결과입니다.

---

## 검토 결과 요약

| # | 항목 | 기존 결정 | 재검토 결과 | 판정 |
|---|------|----------|------------|------|
| 1 | UI 프레임워크 | WinForms | WinForms **유지** | ✅ 유지 |
| 2 | UI 스타일 라이브러리 | MetroFramework | **AntdUI로 변경** | ⚠️ 변경 |
| 3 | 타임테이블 UI | DataGridView | **커스텀 Panel(GDI+)로 변경** | ⚠️ 변경 |
| 4 | 런타임 | .NET 8 | .NET 8 **유지** | ✅ 유지 |
| 5 | DB | SQLite | SQLite **유지** | ✅ 유지 |
| 6 | ORM | Dapper | Dapper **유지** | ✅ 유지 |
| 7 | 웹 스크래핑 | HttpClient + HtmlAgilityPack | **AngleSharp로 변경** | ⚠️ 변경 |
| 8 | MD 렌더링 | WebBrowser 컨트롤 | **Markdig + RichTextBox로 변경** | ⚠️ 변경 |
| 9 | 아키텍처 패턴 | MVP | MVP **유지** | ✅ 유지 |
| 10 | 배포 방식 | 자체포함 | Framework-dependent **유지** | ✅ 유지 |

**변경 4건, 유지 6건**

---

## 상세 검토

---

### 1. UI 프레임워크: WinForms ✅ 유지

**의문**: Python CustomTkinter가 개발 속도면에서 더 낫지 않은가?

**결론: WinForms이 맞다.**

| 비교 항목 | WinForms | CustomTkinter |
|-----------|----------|---------------|
| 테이블/그리드 | DataGridView (네이티브) | 내장 없음. CTkTable은 미성숙 |
| 타임테이블 | GDI+ 커스텀 드로잉 가능 | Canvas로 전부 직접 그려야 함 |
| 데이터 바인딩 | BindingSource 지원 | 없음 (수동 연결) |
| 비동기 처리 | async/await 완전 지원 | GIL 문제, threading 복잡 |
| 배포 | 단일 .exe 가능 | PyInstaller 번들 불안정 + 백신 오탐 |

이 프로젝트는 **테이블 중심 데이터 앱**이다. CustomTkinter에는 쓸 만한 Grid 컨트롤이 없어서
스케줄 타임테이블, 급여 테이블, 예약 테이블 등 핵심 UI를 전부 직접 그려야 한다.
WinForms에서는 DataGridView + 커스텀 Panel 조합으로 훨씬 적은 코드로 구현 가능하다.

---

### 2. UI 스타일: MetroFramework → ⚠️ AntdUI로 변경

**문제 발견**: MetroFramework는 **사실상 죽은 프로젝트**다.
- 마지막 의미 있는 업데이트: 수년 전
- .NET 8 미지원 (NuGet v1.4.0에서 멈춤)
- .NET Core 포크(dameng324)도 견인력 미미

**대안 비교**:

| 라이브러리 | GitHub Stars | .NET 8 | 활성(2026) | 라이선스 | 특징 |
|-----------|-------------|--------|-----------|---------|------|
| **AntdUI** | ~1,400 | ✅ | ✅ (2026.02 업데이트) | MIT | Ant Design 포트, GDI+ 커스텀 드로잉, 모던 |
| ReaLTaiizor | ~2,200 | ✅ | ✅ | MIT | 50+ 컨트롤, 다양한 테마 |
| Krypton | 커뮤니티 | ✅ (.NET 8-10) | ✅ (2026.01 NuGet) | MIT | 49 컨트롤, Office 스타일, 매우 안정적 |
| MaterialSkin2 | ~2,800 | ⚠️ 불완전 | △ 반활성 | MIT | .NET 8 완전 포팅 안 됨 |

**선택: AntdUI**

```
선택 이유:
1. .NET 8 완전 지원 + 최근까지 활발한 업데이트
2. GDI+ 기반 → WPF/DirectX 의존 없이 경량
3. Ant Design 디자인 시스템 → 깔끔한 모던 UI
4. MIT 라이선스 → 상용 제약 없음
5. Table, Tabs, Modal, Input, DatePicker 등 필요한 컴포넌트 대부분 내장

대안:
- Krypton도 좋은 선택 (더 안정적이지만 Office 스타일이라 디자인이 약간 구식)
- 두 라이브러리를 혼용할 수도 있음
```

---

### 3. 타임테이블: DataGridView → ⚠️ 커스텀 Panel로 변경

**문제 발견**: DataGridView는 타임테이블에 **부적합**하다.

```
DataGridView의 한계:
✗ 셀 병합(merge) 불가능 — 같은 직원이 연속된 시간대를 하나의 블록으로 표시 불가
✗ 드래그 앤 드롭 미지원 — 직원 블록을 끌어서 이동/복사 불가
✗ 색상 셀 커스터마이징 — CellPainting 이벤트로 가능하지만 코드가 지저분
✗ 여러 직원이 같은 셀에 겹치는 경우 표현 어려움
```

**변경: 커스텀 Panel (GDI+ 직접 드로잉)**

```
┌──────────────────────────────────────────────────────────────┐
│  시간    │ 월(16)  │ 화(17)  │ 수(18)  │ ...               │
├──────────┼─────────┼─────────┼─────────┤                    │
│ 10:00    │         │         │         │                    │
│ 10:30    │         │         │         │                    │
│ 11:00    │┌───────┐│         │         │                    │
│ 11:30    ││홍길동 ││         │         │  ← 연속 블록으로  │
│ 12:00    ││       ││         │         │     하나의 막대    │
│ 12:30    │└───────┘│         │         │                    │
│ 13:00    │         │┌───────┐│┌───────┐│                    │
│ 13:30    │         ││김철수 │││김철수 ││                    │
│ 14:00    │         ││       │││       ││                    │
│ 14:30    │         │└───────┘│└───────┘│                    │
└──────────┴─────────┴─────────┴─────────┘
```

```
커스텀 Panel 장점:
✓ 연속 시간대를 하나의 색상 블록으로 병합 표시
✓ 블록 클릭/드래그 앤 드롭으로 스케줄 이동
✓ 직원별 고유 색상 블록
✓ 출퇴근 상태 표시 (블록 테두리 파랑/빨강)
✓ 메모리 최소 (컨트롤 인스턴스 없음, 픽셀 단위 드로잉)
✓ 성능 우수 (GDI+ 더블 버퍼링)
```

**구현 전략**:

| 용도 | 컨트롤 |
|------|--------|
| 스케줄 타임테이블 | **커스텀 Panel (GDI+)** |
| 예약 테이블 | DataGridView (단순 표 형태라 적합) |
| 급여 테이블 | DataGridView (일반 표) |
| 물품/출퇴근 이력 | DataGridView (일반 표) |

→ DataGridView를 완전히 버리는 것이 아니라, **단순 표는 DataGridView, 복잡한 타임테이블만 커스텀 Panel**로 분리.

---

### 4. .NET 런타임: .NET 8 ✅ 유지

**의문**: .NET Framework 4.8이 Windows 10/11에 기본 설치되어 있으므로 더 나은 선택 아닌가?

**결론: .NET 8 유지.**

| 비교 | .NET Framework 4.8 | .NET 8 |
|------|--------------------|--------|
| 런타임 설치 | 불필요 (Windows 내장) | 필요 (또는 자체포함) |
| 성능 | 기준선 | **15~30% 향상** (GC, JIT 최적화) |
| C# 버전 | C# 7.3 (구식) | **C# 12** (최신) |
| async/await | 지원하지만 구형 | **개선된 비동기 (ValueTask 등)** |
| LTS | 보안패치만 | **2026.11까지 LTS** |
| AntdUI 호환 | ✗ 미지원 | ✅ 지원 |
| Dapper 최신 | 지원 | ✅ 지원 |

**결정적 이유**: AntdUI가 .NET 8+만 지원한다. .NET Fx 4.8을 선택하면 UI 라이브러리를 Krypton으로 바꿔야 한다. .NET 8의 성능 이점 + 최신 C# 문법이 개발 생산성에 직접적인 영향을 준다.

**배포 전략**: Framework-dependent로 배포하고, 설치 프로그램에 .NET 8 런타임 설치를 포함시킨다 (Inno Setup 또는 NSIS).

---

### 5. DB: SQLite ✅ 유지

**의문**: LiteDB(NoSQL)나 파일 기반 JSON이 더 단순하지 않은가?

**결론: SQLite 유지.**

| | SQLite | LiteDB | JSON 파일 |
|---|--------|--------|----------|
| 동시 읽기/쓰기 | WAL로 해결 | 단일 프로세스 락 | 파일 락 필요 |
| 쿼리 | SQL (복잡한 집계 가능) | LINQ (제한적) | 불가능 |
| 급여 계산 쿼리 | `SUM`, `GROUP BY` 가능 | 코드에서 계산 | 코드에서 계산 |
| 인덱스 | 지원 | 지원 | 없음 |
| 데이터 크기 | 10MB/년 | 비슷 | 비슷 |
| 백업 | Online Backup API | 파일 복사 | 파일 복사 |

급여 계산에서 주차별 근무시간 집계, 공휴일 조인 등 **관계형 쿼리가 필수**이므로 SQLite가 정답이다.

---

### 6. ORM: Dapper ✅ 유지

**의문**: 13개 테이블이면 Raw ADO.NET으로 충분하지 않은가?

**결론: Dapper 유지.**

```
Dapper 사용 시:
var employees = conn.Query<Employee>("SELECT * FROM employees WHERE is_active = 1");

Raw ADO.NET 사용 시:
var cmd = new SqliteCommand("SELECT * FROM employees WHERE is_active = 1", conn);
using var reader = cmd.ExecuteReader();
var list = new List<Employee>();
while (reader.Read()) {
    list.Add(new Employee {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        HourlyWage = reader.GetInt32(2),
        // ... 7개 컬럼 더
    });
}
```

- Dapper는 NuGet 하나 추가로 **수백 줄의 매핑 코드를 제거**한다
- 성능 차이: **5~10% 이내** (체감 불가)
- 메모리 차이: 사실상 없음 (Dapper 자체는 ~200KB DLL)

---

### 7. 웹 스크래핑: HtmlAgilityPack → ⚠️ AngleSharp로 변경

**문제 발견**: HtmlAgilityPack은 **유지보수가 느려진 레거시 라이브러리**다.

| 비교 | HtmlAgilityPack | AngleSharp |
|------|----------------|------------|
| 파싱 방식 | XPath | **CSS 셀렉터** (직관적) |
| 로그인 처리 | 별도 HttpClient + 수동 쿠키 관리 | **내장 브라우징 컨텍스트 + 자동 쿠키** |
| 비동기 | 제한적 | **완전한 async/await** |
| 유지보수 | 느림 | **활발** |
| .NET 8 | 지원 | **네이티브 지원** |

**AngleSharp 로그인 예시**:

```csharp
// AngleSharp: 쿠키 자동 관리, 폼 서밋 내장
var config = Configuration.Default.WithDefaultLoader().WithCookies();
var context = BrowsingContext.New(config);

// 로그인
var loginPage = await context.OpenAsync("http://www.cubeescape.co.kr/bbs/login.php");
var form = loginPage.QuerySelector<IHtmlFormElement>("form");
await form.SubmitAsync(new { mb_id = id, mb_password = password });

// 예약 조회 (쿠키 자동 유지)
var page = await context.OpenAsync("http://www.cubeescape.co.kr/adm/room_list.php?sfl=r_date&stx=26-03-18");
var rows = page.QuerySelectorAll("table tr");  // CSS 셀렉터!
```

**만약 JavaScript 실행이 필요한 경우**: Playwright(.NET) 폴백 준비.
단, cubeescape.co.kr은 단순 PHP 사이트로 보이므로 AngleSharp만으로 충분할 가능성이 높다.

---

### 8. MD 렌더링: WebBrowser → ⚠️ Markdig + RichTextBox로 변경

**문제 발견**: WebBrowser 컨트롤은 **심각한 문제**가 있다.

```
WebBrowser 컨트롤 문제점:
✗ IE11(Trident) 엔진 기반 → 2025년 기준 완전 구식
✗ 메모리 30~50MB 추가 소비
✗ 메모리 누수 이슈 (문서화된 known issue)
✗ CSS3/HTML5 렌더링 불완전
✗ Microsoft 공식 지원 종료
```

**대안 비교**:

| 방식 | 메모리 | 렌더링 품질 | 적합성 |
|------|--------|-----------|--------|
| WebBrowser (IE) | 30~50MB | ✗ 구식 | ✗ 비권장 |
| WebView2 (Chromium) | 100~300MB | ◎ 완벽 | ✗ 저사양 부적합 |
| **Markdig → RTF → RichTextBox** | **2~5MB** | ○ 충분 | **◎ 최적** |

```
Markdig + RichTextBox:
✓ 메모리 추가 2~5MB (WebBrowser 대비 1/10)
✓ 제목, 굵게, 이탤릭, 목록, 코드블록, 링크 지원
✓ RichTextBox는 WinForms 내장 컨트롤 (추가 의존성 없음)
✓ 편집 모드에서 MD 원문 직접 편집 가능

제한사항:
△ 인라인 이미지 표시 어려움 (필요 시 별도 PictureBox)
△ 복잡한 HTML 테이블 렌더링 제한
→ 업무자료 용도로는 충분 (주로 텍스트 기반)
```

---

## 최종 확정 기술 스택

```
┌─────────────────────────────────────────────────────────┐
│                  CubeManager 최종 기술 스택              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  프레임워크:   WinForms + .NET 8 (C# 12)               │
│  UI 스타일:    AntdUI (MIT, .NET 8, 모던 디자인)  ←변경│
│  타임테이블:   커스텀 Panel + GDI+ 드로잉         ←변경│
│  일반 테이블:  DataGridView (예약/급여/물품/출퇴근)     │
│  DB:          SQLite (WAL 모드)                         │
│  ORM:         Dapper + Microsoft.Data.Sqlite            │
│  웹 스크래핑:  AngleSharp (.WithCookies())         ←변경│
│  HTML 파싱:    AngleSharp CSS 셀렉터              ←변경│
│  MD 렌더링:    Markdig → RTF → RichTextBox        ←변경│
│  공휴일:       공공데이터포털 API + 내장 데이터         │
│  배포:         Framework-dependent + .NET 8 런타임 번들 │
│  설치:         Inno Setup                               │
│                                                         │
│  예상 RAM:     50~70MB (피크)                           │
│  배포 크기:    ~10MB (런타임 미포함)                    │
│                ~80MB (런타임 포함 설치파일)              │
│  콜드 스타트:  <1초                                     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## NuGet 패키지 목록 (확정)

| 패키지 | 버전 | 용도 | 크기 |
|--------|------|------|------|
| AntdUI | latest | UI 스타일 컴포넌트 | ~2MB |
| Microsoft.Data.Sqlite | 8.x | SQLite 연결 | ~200KB |
| Dapper | 2.x | 경량 ORM | ~200KB |
| AngleSharp | 1.x | 웹 스크래핑 + HTML 파싱 | ~1MB |
| Markdig | 0.3x | Markdown → HTML/RTF 변환 | ~500KB |
| BCrypt.Net-Next | 4.x | 관리자 비밀번호 해싱 | ~100KB |

**총 외부 의존성: ~4MB** → 매우 가벼움

---

## 변경에 따른 영향 분석

### 영향 없는 부분 (기존 설계 그대로)
- DB 스키마 (database-schema.md) → 변경 없음
- 비즈니스 로직 (Service 레이어) → 변경 없음
- 데이터 모델 (Model) → 변경 없음
- 기능 명세 (01~08 feature 문서) → 변경 없음

### 영향 있는 부분
| 문서 | 변경 내용 |
|------|----------|
| system-architecture.md | 기술 스택 표 업데이트 |
| low-spec-review.md | MetroFramework → AntdUI 반영 |
| ui-specification.md | 타임테이블 → 커스텀 블록 UI로 업데이트 |
| 02-work-schedule.md | 타임테이블 구현 방식 주석 추가 |
| 05-work-documents.md | MD 렌더링 방식 변경 반영 |
