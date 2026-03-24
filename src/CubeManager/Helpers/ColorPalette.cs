using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// CubeManager 전역 색상 시스템 — 2025 Modern Refresh.
/// 라이트/다크 모드 지원 — IsDark 스위치로 즉시 전환.
///
/// 디자인 원칙:
/// 1. Primary는 CTA 버튼 + 선택 상태에만 (면적 최소화)
/// 2. 배경/카드는 따뜻한 뉴트럴 톤 (차가운 순백 X)
/// 3. Semantic 색상은 상태 표시에만
/// 4. 면적이 클수록 채도 낮게
/// </summary>
public static class ColorPalette
{
    /// <summary>다크 모드 여부. 변경 후 모든 컨트롤 Invalidate() 필요.</summary>
    public static bool IsDark { get; set; } = false;

    // ═══════════════════════════════════════════
    // Primary 계열 — Indigo Blue (모던하고 신뢰감)
    // ═══════════════════════════════════════════
    public static Color Primary => IsDark ? FromHex("#818CF8") : FromHex("#4F46E5");
    public static Color Primary50 => IsDark ? FromHex("#1E1B4B") : FromHex("#EEF2FF");
    public static Color Primary100 => IsDark ? FromHex("#312E81") : FromHex("#E0E7FF");
    public static Color Primary700 => IsDark ? FromHex("#A5B4FC") : FromHex("#4338CA");
    public static Color Primary900 => IsDark ? FromHex("#0F0D24") : FromHex("#312E81");

    // ═══════════════════════════════════════════
    // Semantic Colors — 채도 조절 (부드러운 톤)
    // ═══════════════════════════════════════════
    public static Color Success => IsDark ? FromHex("#34D399") : FromHex("#059669");
    public static Color SuccessLight => IsDark ? FromHex("#022C22") : FromHex("#ECFDF5");

    public static Color Warning => IsDark ? FromHex("#FBBF24") : FromHex("#D97706");
    public static Color WarningLight => IsDark ? FromHex("#422006") : FromHex("#FFFBEB");

    public static Color Danger => IsDark ? FromHex("#FB7185") : FromHex("#E11D48");
    public static Color DangerLight => IsDark ? FromHex("#4C0519") : FromHex("#FFF1F2");

    public static Color Info => IsDark ? FromHex("#38BDF8") : FromHex("#0284C7");
    public static Color InfoLight => IsDark ? FromHex("#0C4A6E") : FromHex("#F0F9FF");

    // ═══════════════════════════════════════════
    // Surface / Background — 따뜻한 뉴트럴 (2025 트렌드)
    // ═══════════════════════════════════════════
    public static Color Background => IsDark ? FromHex("#18181B") : FromHex("#F8FAFC");
    public static Color Surface => IsDark ? FromHex("#27272A") : FromHex("#FFFFFF");
    public static Color Card => IsDark ? FromHex("#3F3F46") : FromHex("#FFFFFF");
    public static Color Border => IsDark ? FromHex("#3F3F46") : FromHex("#E2E8F0");
    public static Color Divider => IsDark ? FromHex("#27272A") : FromHex("#F1F5F9");
    public static Color ManualEdit => IsDark ? FromHex("#5C4800") : FromHex("#FEF9C3");

    // Grid / Table 전용
    public static Color HeaderBg => IsDark ? FromHex("#27272A") : FromHex("#F8FAFC");
    public static Color RowAlt => IsDark ? FromHex("#2C2C30") : FromHex("#F8FAFC");
    public static Color HoverBg => IsDark ? FromHex("#3F3F46") : FromHex("#EEF2FF");
    public static Color SelectedBg => IsDark ? FromHex("#312E81") : FromHex("#EEF2FF");

    // 깊이감
    public static Color ShadowLight => IsDark
        ? Color.FromArgb(30, 0, 0, 0)
        : Color.FromArgb(10, 0, 0, 0);
    public static Color CardHover => IsDark ? FromHex("#3F3F46") : FromHex("#F1F5F9");
    public static Color SubtleBg => IsDark ? FromHex("#27272A") : FromHex("#F1F5F9");
    public static Color EditorBg => IsDark ? FromHex("#1C1C1E") : FromHex("#FAFAF9");

    // ═══════════════════════════════════════════
    // Text — 높은 대비 + 부드러운 계층
    // ═══════════════════════════════════════════
    public static Color Text => IsDark ? FromHex("#F4F4F5") : FromHex("#0F172A");
    public static Color TextSecondary => IsDark ? FromHex("#A1A1AA") : FromHex("#475569");
    public static Color TextTertiary => IsDark ? FromHex("#71717A") : FromHex("#94A3B8");
    public static Color TextWhite => Color.White;

    // ═══════════════════════════════════════════
    // Navigation — Primary 기반
    // ═══════════════════════════════════════════
    public static Color NavDefault => IsDark ? FromHex("#71717A") : FromHex("#94A3B8");
    public static Color NavHover => IsDark ? FromHex("#A1A1AA") : FromHex("#475569");
    public static Color NavHoverBg => IsDark ? FromHex("#3F3F46") : FromHex("#F1F5F9");
    public static Color NavActive => IsDark ? FromHex("#818CF8") : FromHex("#4F46E5");
    public static Color NavActiveBg => IsDark ? FromHex("#1E1B4B") : FromHex("#EEF2FF");

    // ═══════════════════════════════════════════
    // Payment Tags (배경, 글자)
    // ═══════════════════════════════════════════
    public static (Color Bg, Color Fg) PaymentCard => IsDark
        ? (FromHex("#1E1B4B"), FromHex("#A5B4FC"))
        : (FromHex("#EEF2FF"), FromHex("#4338CA"));
    public static (Color Bg, Color Fg) PaymentCash => IsDark
        ? (FromHex("#022C22"), FromHex("#34D399"))
        : (FromHex("#ECFDF5"), FromHex("#059669"));
    public static (Color Bg, Color Fg) PaymentTransfer => IsDark
        ? (FromHex("#422006"), FromHex("#FBBF24"))
        : (FromHex("#FFFBEB"), FromHex("#D97706"));
    public static (Color Bg, Color Fg) PaymentExpense => IsDark
        ? (FromHex("#4C0519"), FromHex("#FB7185"))
        : (FromHex("#FFF1F2"), FromHex("#E11D48"));

    // ═══════════════════════════════════════════
    // Attendance (출퇴근)
    // ═══════════════════════════════════════════
    public static Color OnTime => IsDark ? FromHex("#A5B4FC") : FromHex("#4338CA");
    public static Color Late => IsDark ? FromHex("#FB7185") : FromHex("#E11D48");
    public static Color MissingRecord => IsDark ? FromHex("#71717A") : FromHex("#94A3B8");

    // ═══════════════════════════════════════════
    // Employee Schedule Colors (타임테이블 — 더 세련된 파스텔)
    // ═══════════════════════════════════════════
    public static Color[] EmployeeColors => IsDark
        ?
        [
            FromHex("#312E81"),  // 인디고
            FromHex("#064E3B"),  // 에메랄드
            FromHex("#831843"),  // 로즈
            FromHex("#78350F"),  // 앰버
            FromHex("#581C87"),  // 퍼플
            FromHex("#164E63"),  // 사이안
            FromHex("#713F12"),  // 옐로우
            FromHex("#44403C"),  // 스톤
        ]
        :
        [
            FromHex("#C7D2FE"),  // 인디고 100
            FromHex("#A7F3D0"),  // 에메랄드 100
            FromHex("#FECDD3"),  // 로즈 100
            FromHex("#FDE68A"),  // 앰버 100
            FromHex("#DDD6FE"),  // 퍼플 100
            FromHex("#A5F3FC"),  // 사이안 100
            FromHex("#FEF08A"),  // 옐로우 100
            FromHex("#D6D3D1"),  // 스톤 100
        ];

    public static Color GetEmployeeColor(int index) =>
        EmployeeColors[index % EmployeeColors.Length];

    // ═══════════════════════════════════════════
    // Summary Card Accent (통계 카드)
    // ═══════════════════════════════════════════
    public static (Color Light, Color Main) AccentBlue => IsDark
        ? (FromHex("#1E1B4B"), FromHex("#818CF8"))
        : (FromHex("#EEF2FF"), FromHex("#4F46E5"));
    public static (Color Light, Color Main) AccentGreen => IsDark
        ? (FromHex("#022C22"), FromHex("#34D399"))
        : (FromHex("#ECFDF5"), FromHex("#059669"));
    public static (Color Light, Color Main) AccentOrange => IsDark
        ? (FromHex("#422006"), FromHex("#FBBF24"))
        : (FromHex("#FFFBEB"), FromHex("#D97706"));
    public static (Color Light, Color Main) AccentRed => IsDark
        ? (FromHex("#4C0519"), FromHex("#FB7185"))
        : (FromHex("#FFF1F2"), FromHex("#E11D48"));

    // ═══════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════
    private static Color FromHex(string hex) => ColorTranslator.FromHtml(hex);
}
