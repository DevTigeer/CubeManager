using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// CubeManager 2025 Design System — Dark Tone Monochrome.
///
/// 원칙:
/// 1. 배경: 진한 그레이(#1E1E1E~#2D2D2D) 계층으로 영역 구분
/// 2. 텍스트: 흰색/밝은 회색 (#F5F5F5, #B0B0B0)
/// 3. 포인트: 파란색(활성), 초록(성공), 빨강(위험)만
/// 4. 뉴모피즘: 밝은/어두운 그림자 쌍으로 입체감
/// 5. 글래스: 반투명 + 밝은 테두리로 깊이감
/// </summary>
public static class ColorPalette
{
    public static bool IsDark { get; set; } = false;

    // ═══════════════════════════════════════════
    // Accent (활성 상태에만)
    // ═══════════════════════════════════════════
    public static Color Primary => FromHex("#3B82F6");          // Blue 500
    public static Color Primary50 => FromHex("#2A3A50");        // Blue tint on dark
    public static Color Primary100 => FromHex("#2A4A7F");       // Blue bg
    public static Color Primary700 => FromHex("#2563EB");       // Blue 600
    public static Color Primary900 => FromHex("#1E3A8A");       // Blue 900

    // ═══════════════════════════════════════════
    // Semantic — 포인트 (상태 표시에만)
    // ═══════════════════════════════════════════
    public static Color Success => FromHex("#22C55E");          // Green 500
    public static Color SuccessLight => FromHex("#14532D");     // Green 900
    public static Color Warning => FromHex("#F59E0B");          // Amber 500
    public static Color WarningLight => FromHex("#422006");     // Amber 950
    public static Color Danger => FromHex("#EF4444");           // Red 500
    public static Color DangerLight => FromHex("#450A0A");      // Red 950
    public static Color Info => FromHex("#3B82F6");             // = Primary
    public static Color InfoLight => FromHex("#1E3A5F");

    // ═══════════════════════════════════════════
    // Surface / Background — 밝은 그레이 계층
    // ═══════════════════════════════════════════
    public static Color Background => FromHex("#2A2A2A");       // 사이드바/가장 어두운 영역
    public static Color Surface => FromHex("#333333");          // 메인 패널 배경
    public static Color Card => FromHex("#3D3D3D");             // 카드/섹션 배경
    public static Color Border => FromHex("#4A4A4A");           // 구분선
    public static Color Divider => FromHex("#404040");          // 미세 구분
    public static Color ManualEdit => FromHex("#5C4800");       // 수기 편집

    // Grid / Table — 배경보다 밝은 화이트 톤 (대비)
    public static Color HeaderBg => FromHex("#484848");         // 테이블 헤더 (밝은 그레이)
    public static Color RowAlt => FromHex("#424242");           // 교차행
    public static Color HoverBg => FromHex("#505050");          // 행 호버
    public static Color SelectedBg => FromHex("#2A4A7F");       // 선택행 (파란 tint)

    // 뉴모피즘 그림자 쌍
    public static Color NeuLight => Color.FromArgb(15, 255, 255, 255);  // 밝은 그림자
    public static Color NeuDark => Color.FromArgb(60, 0, 0, 0);        // 어두운 그림자

    // 글래스모피즘 근사
    public static Color GlassBg => Color.FromArgb(40, 255, 255, 255);
    public static Color GlassBorder => Color.FromArgb(50, 255, 255, 255);

    // 깊이감
    public static Color ShadowLight => Color.FromArgb(40, 0, 0, 0);
    public static Color CardHover => FromHex("#474747");
    public static Color SubtleBg => FromHex("#353535");
    public static Color EditorBg => FromHex("#383838");

    // ═══════════════════════════════════════════
    // Text — 밝은 톤 계층
    // ═══════════════════════════════════════════
    public static Color Text => FromHex("#F5F5F5");             // 메인 텍스트 (밝은 흰)
    public static Color TextSecondary => FromHex("#A0A0A0");    // 보조 텍스트
    public static Color TextTertiary => FromHex("#707070");     // 힌트/캡션
    public static Color TextWhite => Color.White;

    // ═══════════════════════════════════════════
    // Navigation
    // ═══════════════════════════════════════════
    public static Color NavDefault => FromHex("#808080");       // 비활성 아이콘
    public static Color NavHover => FromHex("#D0D0D0");         // 호버 (밝아짐)
    public static Color NavHoverBg => FromHex("#3A3A3A");       // 호버 배경
    public static Color NavActive => FromHex("#3B82F6");        // 활성 (파란)
    public static Color NavActiveBg => FromHex("#2A4A7F");      // 활성 배경

    // ═══════════════════════════════════════════
    // Payment Tags
    // ═══════════════════════════════════════════
    public static (Color Bg, Color Fg) PaymentCard =>
        (FromHex("#1E3A5F"), FromHex("#93C5FD"));
    public static (Color Bg, Color Fg) PaymentCash =>
        (FromHex("#14532D"), FromHex("#4ADE80"));
    public static (Color Bg, Color Fg) PaymentTransfer =>
        (FromHex("#422006"), FromHex("#FBBF24"));
    public static (Color Bg, Color Fg) PaymentExpense =>
        (FromHex("#450A0A"), FromHex("#F87171"));

    // ═══════════════════════════════════════════
    // Attendance
    // ═══════════════════════════════════════════
    public static Color OnTime => FromHex("#3B82F6");
    public static Color Late => FromHex("#EF4444");
    public static Color MissingRecord => FromHex("#707070");

    // ═══════════════════════════════════════════
    // Employee Schedule Colors (어두운 파스텔)
    // ═══════════════════════════════════════════
    public static Color[] EmployeeColors =>
    [
        FromHex("#1E3A5F"),  // 딥 네이비
        FromHex("#14532D"),  // 딥 에메랄드
        FromHex("#4C0519"),  // 딥 로즈
        FromHex("#78350F"),  // 딥 앰버
        FromHex("#3B0764"),  // 딥 퍼플
        FromHex("#164E63"),  // 딥 사이안
        FromHex("#422006"),  // 딥 브론즈
        FromHex("#292524"),  // 딥 스톤
    ];

    public static Color GetEmployeeColor(int index) =>
        EmployeeColors[index % EmployeeColors.Length];

    // ═══════════════════════════════════════════
    // Summary Card Accent
    // ═══════════════════════════════════════════
    public static (Color Light, Color Main) AccentBlue =>
        (FromHex("#1E3A5F"), FromHex("#3B82F6"));
    public static (Color Light, Color Main) AccentGreen =>
        (FromHex("#14532D"), FromHex("#22C55E"));
    public static (Color Light, Color Main) AccentOrange =>
        (FromHex("#422006"), FromHex("#F59E0B"));
    public static (Color Light, Color Main) AccentRed =>
        (FromHex("#450A0A"), FromHex("#EF4444"));

    // ═══════════════════════════════════════════
    private static Color FromHex(string hex) => ColorTranslator.FromHtml(hex);
}
