using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// 디자인 토큰 시스템 — 4px 그리드, 타이포 계층, Elevation, 아이콘.
/// 모든 UI 코드에서 하드코딩 대신 이 토큰을 참조할 것.
/// </summary>
public static class DesignTokens
{
    // ═══════════════════════════════════════════
    // 4px Spacing Grid
    // ═══════════════════════════════════════════
    public const int SpaceXS = 4;    // 아이콘-텍스트 간격
    public const int SpaceSM = 8;    // 요소 내부 패딩
    public const int SpaceMD = 12;   // 카드 내부 패딩
    public const int SpaceLG = 16;   // 섹션 간 간격
    public const int SpaceXL = 24;   // 영역 간 간격
    public const int SpaceXXL = 32;  // 페이지 여백

    // ═══════════════════════════════════════════
    // Typography — 모든 텍스트 Bold, 크기로 계층 구분
    // ═══════════════════════════════════════════
    // 제목/탭/메뉴: Aptos Bold (큰 사이즈)
    // 본문/데이터: 맑은 고딕 Bold (기본 사이즈)
    // 숫자/통계: Segoe UI Bold
    // 기존 Bold/Regular 구분 → 사이즈 구분으로 전환

    private static readonly string HeadingFont = IsAptoAvailable() ? "Aptos" : "Segoe UI";

    /// <summary>페이지 제목: Aptos 16px Bold</summary>
    public static Font FontPageTitle => new(HeadingFont, 16f, FontStyle.Bold);
    /// <summary>섹션 제목: Aptos 13px Bold</summary>
    public static Font FontSectionTitle => new(HeadingFont, 13f, FontStyle.Bold);
    /// <summary>탭/메뉴: Aptos 10.5px Bold</summary>
    public static Font FontTabMenu => new(HeadingFont, 10.5f, FontStyle.Bold);
    /// <summary>본문: 맑은 고딕 10px Bold (모든 텍스트 Bold)</summary>
    public static Font FontBody => new("맑은 고딕", 10f, FontStyle.Bold);
    /// <summary>본문 큰: 맑은 고딕 11px Bold (기존 Bold 구분용)</summary>
    public static Font FontBodyLarge => new("맑은 고딕", 11f, FontStyle.Bold);
    /// <summary>본문 작은: 맑은 고딕 9px Bold (기존 Regular 구분용)</summary>
    public static Font FontBodySmall => new("맑은 고딕", 9f, FontStyle.Bold);
    /// <summary>캡션/힌트: 맑은 고딕 8.5px Bold</summary>
    public static Font FontCaption => new("맑은 고딕", 8.5f, FontStyle.Bold);
    /// <summary>통계 메인값: Segoe UI 24px Bold</summary>
    public static Font FontStatValue => new("Segoe UI", 24f, FontStyle.Bold);
    /// <summary>통계 서브값: Segoe UI 18px Bold</summary>
    public static Font FontStatSub => new("Segoe UI", 18f, FontStyle.Bold);
    /// <summary>통계 단위: Segoe UI 12px Bold</summary>
    public static Font FontStatUnit => new("Segoe UI", 12f, FontStyle.Bold);
    /// <summary>버튼 텍스트: Aptos 10px Bold</summary>
    public static Font FontButton => new(HeadingFont, 10f, FontStyle.Bold);

    private static bool IsAptoAvailable()
    {
        try { using var f = new Font("Aptos", 10f); return f.Name == "Aptos"; }
        catch { return false; }
    }

    // ═══════════════════════════════════════════
    // Elevation (깊이) — 3단계
    // ═══════════════════════════════════════════

    /// <summary>Level 0: 배경 (그림자 없음)</summary>
    public static readonly ElevationStyle Elevation0 = new(0, 0);

    /// <summary>Level 1: 카드/패널 — 하단 1px</summary>
    public static readonly ElevationStyle Elevation1 = new(8, 1);

    /// <summary>Level 2: 드롭다운/팝업 — 하단+우측 2px</summary>
    public static readonly ElevationStyle Elevation2 = new(15, 2);

    /// <summary>Level 3: 다이얼로그/토스트 — 3면 2px</summary>
    public static readonly ElevationStyle Elevation3 = new(20, 2);

    /// <summary>Elevation 그림자를 GDI+로 그리기</summary>
    public static void DrawElevation(Graphics g, Rectangle rect, ElevationStyle elev)
    {
        if (elev.Alpha <= 0) return;
        using var pen = new Pen(Color.FromArgb(elev.Alpha, 0, 0, 0));

        // 하단 그림자
        for (var i = 1; i <= elev.Spread; i++)
            g.DrawLine(pen,
                rect.X + 4, rect.Bottom + i,
                rect.Right - 4, rect.Bottom + i);

        // Level 2+: 우측 그림자
        if (elev.Spread >= 2)
            g.DrawLine(pen,
                rect.Right + 1, rect.Y + 4,
                rect.Right + 1, rect.Bottom - 4);
    }

    // ═══════════════════════════════════════════
    // Segoe MDL2 Assets Icons (Windows 10/11 내장)
    // ═══════════════════════════════════════════

    /// <summary>MDL2 아이콘 폰트 (12pt)</summary>
    public static Font IconFont => new("Segoe MDL2 Assets", 12f);
    /// <summary>MDL2 아이콘 폰트 (16pt — 사이드바용)</summary>
    public static Font IconFontLarge => new("Segoe MDL2 Assets", 16f);

    // 자주 쓰는 아이콘 코드포인트
    public const string IconCalendar = "\uE787";     // 📅 예약/매출
    public const string IconSchedule = "\uE8BF";     // 📋 스케줄
    public const string IconChecklist = "\uE73A";    // ✅ 체크리스트
    public const string IconClock = "\uE823";        // ⏰ 출퇴근
    public const string IconNote = "\uE70B";         // 📝 인수인계
    public const string IconTicket = "\uEB54";       // 🎫 무료이용권
    public const string IconBox = "\uE7B8";          // 📦 물품
    public const string IconDocument = "\uE8A5";     // 📄 업무자료
    public const string IconKey = "\uE8D7";          // 🔑 테마힌트
    public const string IconSettings = "\uE713";     // ⚙️ 설정
    public const string IconShield = "\uE83D";       // 🛡️ 관리자

    public const string IconAdd = "\uE710";          // + 추가
    public const string IconDelete = "\uE74D";       // 삭제
    public const string IconSearch = "\uE721";       // 검색
    public const string IconRefresh = "\uE72C";      // 새로고침
    public const string IconSave = "\uE74E";         // 저장
    public const string IconEdit = "\uE70F";         // 편집
    public const string IconClose = "\uE711";        // 닫기
    public const string IconCheck = "\uE73E";        // 체크
    public const string IconAlert = "\uEA39";        // 알림
    public const string IconCalculator = "\uE8EF";   // 계산기
    public const string IconPerson = "\uE77B";       // 사람
    public const string IconBack = "\uE72B";         // ◀ 뒤로
    public const string IconForward = "\uE72A";      // ▶ 앞으로
    public const string IconHome = "\uE80F";         // 홈/오늘

    /// <summary>사이드바 아이콘 배열 (MDL2)</summary>
    public static readonly string[] SideNavIcons =
    [
        IconCalendar, IconSchedule, IconChecklist, IconClock, IconNote,
        IconTicket, IconBox, IconDocument, IconKey, IconSettings, IconShield
    ];
}

/// <summary>Elevation 스타일 정의</summary>
public record ElevationStyle(int Alpha, int Spread);
