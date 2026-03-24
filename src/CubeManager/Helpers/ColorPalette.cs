using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// CubeManager 2025 Design System — #2D3047 Base Color Scheme.
///
/// ── 색상 정책 ──
/// 주색: #2D3047 (깊은 네이비 그레이)
/// 보색: #F18A3D (따뜻한 주황, 강조/CTA)
/// 중성: #F0F0F0 (밝은 회색, 표/카드 배경)
///
/// 삼분색: #47A8D7 (청록, 링크/활성), #D747A8 (자홍, 특수 강조)
/// 유사색: #1E2335 (더 어두운), #3F425D (약간 밝은)
///
/// 원칙:
/// 1. 배경: #1E2335 → #2D3047 → #3F425D (3단 계층)
/// 2. 표/카드: 배경보다 밝은 #F0F0F0 계열 (강한 대비)
/// 3. 텍스트: 모두 Bold, 크기로 계층 구분
/// 4. 포인트: 주황(CTA), 청록(활성), 자홍(특수), 초록(성공), 빨강(위험)
/// </summary>
public static class ColorPalette
{
    public static bool IsDark { get; set; } = false;

    // ═══════════════════════════════════════════
    // 주색 계열 (Main + Analogous)
    // ═══════════════════════════════════════════
    public static Color Main => FromHex("#2D3047");             // 주색
    public static Color MainDark => FromHex("#1E2335");         // 유사색 (더 어두운)
    public static Color MainLight => FromHex("#3F425D");        // 유사색 (약간 밝은)

    // ═══════════════════════════════════════════
    // 보색 & 삼분색 (Accent)
    // ═══════════════════════════════════════════
    public static Color Accent => FromHex("#F18A3D");           // 보색 (따뜻한 주황) — CTA/강조
    public static Color AccentHover => FromHex("#D97830");      // 주황 호버
    public static Color AccentPressed => FromHex("#C06A28");    // 주황 Pressed
    public static Color Primary => FromHex("#47A8D7");          // 삼분색 (청록) — 활성/링크
    public static Color Primary50 => FromHex("#2D4A5F");        // 청록 tint
    public static Color Primary100 => FromHex("#2D5F7F");       // 청록 bg
    public static Color Primary700 => FromHex("#3A95C0");       // 청록 호버
    public static Color Primary900 => FromHex("#1E5A7A");       // 청록 dark
    public static Color Special => FromHex("#D747A8");          // 삼분색 (자홍) — 특수 강조

    // ═══════════════════════════════════════════
    // Semantic — 상태 표시 포인트
    // ═══════════════════════════════════════════
    public static Color Success => FromHex("#22C55E");
    public static Color SuccessLight => FromHex("#1A3D2A");
    public static Color Warning => FromHex("#F59E0B");
    public static Color WarningLight => FromHex("#3D3020");
    public static Color Danger => FromHex("#EF4444");
    public static Color DangerLight => FromHex("#3D1A1A");
    public static Color Info => FromHex("#47A8D7");             // = Primary
    public static Color InfoLight => FromHex("#2D4A5F");

    // ═══════════════════════════════════════════
    // Surface / Background — 주색 계층
    // ═══════════════════════════════════════════
    public static Color Background => FromHex("#1E2335");       // 가장 어두운 (사이드바)
    public static Color Surface => FromHex("#2D3047");          // 메인 패널 = 주색
    public static Color Card => FromHex("#3F425D");             // 카드/섹션 = 유사색 밝은
    public static Color Border => FromHex("#4A4D68");           // 구분선
    public static Color Divider => FromHex("#383B55");          // 미세 구분
    public static Color ManualEdit => FromHex("#5C4800");

    // Grid / Table — 배경과 강한 대비 (밝은 톤)
    public static Color HeaderBg => FromHex("#3F425D");         // 테이블 헤더 (유사색 밝은)
    public static Color RowAlt => FromHex("#E8E8EC");           // 교차행 (밝은 회색)
    public static Color HoverBg => FromHex("#D8D8E0");          // 행 호버
    public static Color SelectedBg => FromHex("#47A8D7");       // 선택행 (청록)

    // 표 전용 색상 (배경 대비)
    public static Color TableBg => FromHex("#F0F0F0");          // 표 배경 (중성 밝은 회색)
    public static Color TableText => FromHex("#1E2335");        // 표 텍스트 (어두운)
    public static Color TableHeaderText => FromHex("#F0F0F0");  // 표 헤더 텍스트 (밝은)

    // 뉴모피즘/글래스
    public static Color NeuLight => Color.FromArgb(20, 255, 255, 255);
    public static Color NeuDark => Color.FromArgb(50, 0, 0, 0);
    public static Color GlassBg => Color.FromArgb(30, 255, 255, 255);
    public static Color GlassBorder => Color.FromArgb(40, 255, 255, 255);

    // 깊이감
    public static Color ShadowLight => Color.FromArgb(35, 0, 0, 0);
    public static Color CardHover => FromHex("#4A4D68");
    public static Color SubtleBg => FromHex("#353850");
    public static Color EditorBg => FromHex("#353850");

    // ═══════════════════════════════════════════
    // Text — 밝은 톤 (모두 Bold, 크기로 계층)
    // ═══════════════════════════════════════════
    public static Color Text => FromHex("#F0F0F0");             // 메인 (밝은 회색)
    public static Color TextSecondary => FromHex("#A0A3B8");    // 보조
    public static Color TextTertiary => FromHex("#6E7190");     // 힌트/캡션
    public static Color TextWhite => Color.White;

    // ═══════════════════════════════════════════
    // Navigation
    // ═══════════════════════════════════════════
    public static Color NavDefault => FromHex("#7A7D95");
    public static Color NavHover => FromHex("#D0D2E0");
    public static Color NavHoverBg => FromHex("#353850");
    public static Color NavActive => FromHex("#47A8D7");        // 청록
    public static Color NavActiveBg => FromHex("#2D4A5F");

    // ═══════════════════════════════════════════
    // Payment Tags
    // ═══════════════════════════════════════════
    public static (Color Bg, Color Fg) PaymentCard =>
        (FromHex("#2D4A5F"), FromHex("#7EC8F0"));
    public static (Color Bg, Color Fg) PaymentCash =>
        (FromHex("#1A3D2A"), FromHex("#4ADE80"));
    public static (Color Bg, Color Fg) PaymentTransfer =>
        (FromHex("#3D3020"), FromHex("#FBBF24"));
    public static (Color Bg, Color Fg) PaymentExpense =>
        (FromHex("#3D1A1A"), FromHex("#F87171"));

    // ═══════════════════════════════════════════
    // Attendance
    // ═══════════════════════════════════════════
    public static Color OnTime => FromHex("#47A8D7");
    public static Color Late => FromHex("#EF4444");
    public static Color MissingRecord => FromHex("#6E7190");

    // ═══════════════════════════════════════════
    // Employee Schedule Colors
    // ═══════════════════════════════════════════
    public static Color[] EmployeeColors =>
    [
        FromHex("#2D5F7F"),  // 딥 사이안
        FromHex("#2D7F4A"),  // 딥 에메랄드
        FromHex("#7F2D5F"),  // 딥 로즈
        FromHex("#7F5F2D"),  // 딥 앰버
        FromHex("#5F2D7F"),  // 딥 퍼플
        FromHex("#2D7F7F"),  // 딥 틸
        FromHex("#7F4A2D"),  // 딥 브론즈
        FromHex("#4A4D68"),  // 딥 슬레이트
    ];

    public static Color GetEmployeeColor(int index) =>
        EmployeeColors[index % EmployeeColors.Length];

    // ═══════════════════════════════════════════
    // Summary Card Accent
    // ═══════════════════════════════════════════
    public static (Color Light, Color Main) AccentBlue =>
        (FromHex("#2D4A5F"), FromHex("#47A8D7"));
    public static (Color Light, Color Main) AccentGreen =>
        (FromHex("#1A3D2A"), FromHex("#22C55E"));
    public static (Color Light, Color Main) AccentOrange =>
        (FromHex("#3D3020"), FromHex("#F18A3D"));
    public static (Color Light, Color Main) AccentRed =>
        (FromHex("#3D1A1A"), FromHex("#EF4444"));

    // ═══════════════════════════════════════════
    private static Color FromHex(string hex) => ColorTranslator.FromHtml(hex);
}
