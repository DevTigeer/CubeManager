using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// CubeManager 전역 색상 시스템 — 2025 Monochrome + Point Color.
///
/// 디자인 원칙:
/// 1. 기본 UI는 무채색(그레이/블랙/화이트)만 사용
/// 2. 활성 상태(선택/포커스)에만 Accent Blue로 가시성 강조
/// 3. 상태 표시(성공/위험)에만 초록/빨강 대비색
/// 4. 뉴모피즘: 밝은+어두운 그림자 쌍으로 입체감
/// 5. 글래스모피즘 근사: 반투명 배경 + 흰 테두리
/// </summary>
public static class ColorPalette
{
    public static bool IsDark { get; set; } = false;

    // ═══════════════════════════════════════════
    // Accent (활성 상태에만 사용 — 파란색 계열)
    // ═══════════════════════════════════════════
    public static Color Primary => IsDark ? FromHex("#60A5FA") : FromHex("#2563EB");
    public static Color Primary50 => IsDark ? FromHex("#1E293B") : FromHex("#EFF6FF");
    public static Color Primary100 => IsDark ? FromHex("#1E3A5F") : FromHex("#DBEAFE");
    public static Color Primary700 => IsDark ? FromHex("#93C5FD") : FromHex("#1D4ED8");
    public static Color Primary900 => IsDark ? FromHex("#0F172A") : FromHex("#1E3A8A");

    // ═══════════════════════════════════════════
    // Semantic — 포인트 컬러 (상태 표시에만)
    // ═══════════════════════════════════════════
    public static Color Success => IsDark ? FromHex("#4ADE80") : FromHex("#16A34A");
    public static Color SuccessLight => IsDark ? FromHex("#14532D") : FromHex("#F0FDF4");

    public static Color Warning => IsDark ? FromHex("#FBBF24") : FromHex("#D97706");
    public static Color WarningLight => IsDark ? FromHex("#422006") : FromHex("#FFFBEB");

    public static Color Danger => IsDark ? FromHex("#F87171") : FromHex("#DC2626");
    public static Color DangerLight => IsDark ? FromHex("#450A0A") : FromHex("#FEF2F2");

    public static Color Info => IsDark ? FromHex("#60A5FA") : FromHex("#2563EB");
    public static Color InfoLight => IsDark ? FromHex("#1E3A5F") : FromHex("#EFF6FF");

    // ═══════════════════════════════════════════
    // Surface / Background — 무채색 (따뜻한 그레이)
    // ═══════════════════════════════════════════
    public static Color Background => IsDark ? FromHex("#121212") : FromHex("#F5F5F5");
    public static Color Surface => IsDark ? FromHex("#1E1E1E") : FromHex("#FFFFFF");
    public static Color Card => IsDark ? FromHex("#2A2A2A") : FromHex("#FFFFFF");
    public static Color Border => IsDark ? FromHex("#333333") : FromHex("#E0E0E0");
    public static Color Divider => IsDark ? FromHex("#2A2A2A") : FromHex("#F0F0F0");
    public static Color ManualEdit => IsDark ? FromHex("#5C4800") : FromHex("#FEF9C3");

    // Grid / Table 전용
    public static Color HeaderBg => IsDark ? FromHex("#1A1A1A") : FromHex("#FAFAFA");
    public static Color RowAlt => IsDark ? FromHex("#222222") : FromHex("#FAFAFA");
    public static Color HoverBg => IsDark ? FromHex("#2A2A2A") : FromHex("#F5F5F5");
    public static Color SelectedBg => IsDark ? FromHex("#1E3A5F") : FromHex("#EFF6FF");

    // 뉴모피즘 그림자 쌍
    public static Color NeuLight => IsDark
        ? Color.FromArgb(12, 255, 255, 255)   // 밝은 그림자 (다크)
        : Color.FromArgb(255, 255, 255, 255);  // 밝은 그림자 (라이트)
    public static Color NeuDark => IsDark
        ? Color.FromArgb(40, 0, 0, 0)          // 어두운 그림자 (다크)
        : Color.FromArgb(20, 0, 0, 0);         // 어두운 그림자 (라이트)

    // 글래스모피즘 근사
    public static Color GlassBg => IsDark
        ? Color.FromArgb(30, 255, 255, 255)    // 반투명 백 (다크)
        : Color.FromArgb(180, 255, 255, 255);  // 반투명 백 (라이트)
    public static Color GlassBorder => IsDark
        ? Color.FromArgb(40, 255, 255, 255)    // 반투명 테두리 (다크)
        : Color.FromArgb(60, 255, 255, 255);   // 반투명 테두리 (라이트)

    // 깊이감
    public static Color ShadowLight => IsDark
        ? Color.FromArgb(30, 0, 0, 0)
        : Color.FromArgb(12, 0, 0, 0);
    public static Color CardHover => IsDark ? FromHex("#333333") : FromHex("#F5F5F5");
    public static Color SubtleBg => IsDark ? FromHex("#1A1A1A") : FromHex("#F5F5F5");
    public static Color EditorBg => IsDark ? FromHex("#1A1A1A") : FromHex("#FAFAFA");

    // ═══════════════════════════════════════════
    // Text — 무채색 계층
    // ═══════════════════════════════════════════
    public static Color Text => IsDark ? FromHex("#F5F5F5") : FromHex("#1A1A1A");
    public static Color TextSecondary => IsDark ? FromHex("#A0A0A0") : FromHex("#666666");
    public static Color TextTertiary => IsDark ? FromHex("#707070") : FromHex("#999999");
    public static Color TextWhite => Color.White;

    // ═══════════════════════════════════════════
    // Navigation — 무채색 기본 + 활성시만 Accent
    // ═══════════════════════════════════════════
    public static Color NavDefault => IsDark ? FromHex("#808080") : FromHex("#999999");
    public static Color NavHover => IsDark ? FromHex("#B0B0B0") : FromHex("#333333");
    public static Color NavHoverBg => IsDark ? FromHex("#2A2A2A") : FromHex("#F0F0F0");
    public static Color NavActive => IsDark ? FromHex("#60A5FA") : FromHex("#2563EB");
    public static Color NavActiveBg => IsDark ? FromHex("#1E3A5F") : FromHex("#EFF6FF");

    // ═══════════════════════════════════════════
    // Payment Tags — 무채색 + 최소 포인트
    // ═══════════════════════════════════════════
    public static (Color Bg, Color Fg) PaymentCard => IsDark
        ? (FromHex("#1E3A5F"), FromHex("#93C5FD"))
        : (FromHex("#EFF6FF"), FromHex("#1D4ED8"));
    public static (Color Bg, Color Fg) PaymentCash => IsDark
        ? (FromHex("#14532D"), FromHex("#4ADE80"))
        : (FromHex("#F0FDF4"), FromHex("#16A34A"));
    public static (Color Bg, Color Fg) PaymentTransfer => IsDark
        ? (FromHex("#422006"), FromHex("#FBBF24"))
        : (FromHex("#FFFBEB"), FromHex("#D97706"));
    public static (Color Bg, Color Fg) PaymentExpense => IsDark
        ? (FromHex("#450A0A"), FromHex("#F87171"))
        : (FromHex("#FEF2F2"), FromHex("#DC2626"));

    // ═══════════════════════════════════════════
    // Attendance — 포인트 컬러
    // ═══════════════════════════════════════════
    public static Color OnTime => IsDark ? FromHex("#93C5FD") : FromHex("#1D4ED8");
    public static Color Late => IsDark ? FromHex("#F87171") : FromHex("#DC2626");
    public static Color MissingRecord => IsDark ? FromHex("#707070") : FromHex("#999999");

    // ═══════════════════════════════════════════
    // Employee Schedule Colors — 무채색 + 미세 색감 차이
    // ═══════════════════════════════════════════
    public static Color[] EmployeeColors => IsDark
        ?
        [
            FromHex("#1E3A5F"),  // 딥 네이비
            FromHex("#2D4A3E"),  // 딥 틸
            FromHex("#4A2040"),  // 딥 마젠타
            FromHex("#4A3A20"),  // 딥 앰버
            FromHex("#352050"),  // 딥 퍼플
            FromHex("#1A3A40"),  // 딥 사이안
            FromHex("#4A4520"),  // 딥 올리브
            FromHex("#3A3530"),  // 딥 토프
        ]
        :
        [
            FromHex("#BFDBFE"),  // 블루 200
            FromHex("#BBF7D0"),  // 그린 200
            FromHex("#FECDD3"),  // 로즈 200
            FromHex("#FDE68A"),  // 앰버 200
            FromHex("#DDD6FE"),  // 바이올렛 200
            FromHex("#A5F3FC"),  // 사이안 200
            FromHex("#FEF08A"),  // 옐로우 200
            FromHex("#E7E5E4"),  // 스톤 200
        ];

    public static Color GetEmployeeColor(int index) =>
        EmployeeColors[index % EmployeeColors.Length];

    // ═══════════════════════════════════════════
    // Summary Card Accent — 무채색 기본 + Accent 포인트
    // ═══════════════════════════════════════════
    public static (Color Light, Color Main) AccentBlue => IsDark
        ? (FromHex("#1E3A5F"), FromHex("#60A5FA"))
        : (FromHex("#EFF6FF"), FromHex("#2563EB"));
    public static (Color Light, Color Main) AccentGreen => IsDark
        ? (FromHex("#14532D"), FromHex("#4ADE80"))
        : (FromHex("#F0FDF4"), FromHex("#16A34A"));
    public static (Color Light, Color Main) AccentOrange => IsDark
        ? (FromHex("#422006"), FromHex("#FBBF24"))
        : (FromHex("#FFFBEB"), FromHex("#D97706"));
    public static (Color Light, Color Main) AccentRed => IsDark
        ? (FromHex("#450A0A"), FromHex("#F87171"))
        : (FromHex("#FEF2F2"), FromHex("#DC2626"));

    // ═══════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════
    private static Color FromHex(string hex) => ColorTranslator.FromHtml(hex);
}
