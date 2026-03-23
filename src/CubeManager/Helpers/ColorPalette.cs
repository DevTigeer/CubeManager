using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// CubeManager 전역 색상 시스템.
/// 라이트/다크 모드 지원 — IsDark 스위치로 즉시 전환.
/// </summary>
public static class ColorPalette
{
    /// <summary>다크 모드 여부. 변경 후 모든 컨트롤 Invalidate() 필요.</summary>
    public static bool IsDark { get; set; } = false;

    // ═══════════════════════════════════════════
    // Primary 계열
    // ═══════════════════════════════════════════
    public static Color Primary => IsDark ? FromHex("#60A5FA") : FromHex("#1976D2");
    public static Color Primary50 => IsDark ? FromHex("#172554") : FromHex("#E3F2FD");
    public static Color Primary100 => IsDark ? FromHex("#1E3A5F") : FromHex("#BBDEFB");
    public static Color Primary700 => IsDark ? FromHex("#93C5FD") : FromHex("#1565C0");
    public static Color Primary900 => IsDark ? FromHex("#0F172A") : FromHex("#0D47A1");

    // ═══════════════════════════════════════════
    // Semantic Colors
    // ═══════════════════════════════════════════
    public static Color Success => IsDark ? FromHex("#4ADE80") : FromHex("#4CAF50");
    public static Color SuccessLight => IsDark ? FromHex("#052E16") : FromHex("#E8F5E9");

    public static Color Warning => IsDark ? FromHex("#FBBF24") : FromHex("#FF9800");
    public static Color WarningLight => IsDark ? FromHex("#422006") : FromHex("#FFF3E0");

    public static Color Danger => IsDark ? FromHex("#F87171") : FromHex("#F44336");
    public static Color DangerLight => IsDark ? FromHex("#450A0A") : FromHex("#FFEBEE");

    public static Color Info => IsDark ? FromHex("#60A5FA") : FromHex("#2196F3");
    public static Color InfoLight => IsDark ? FromHex("#172554") : FromHex("#E3F2FD");

    // ═══════════════════════════════════════════
    // Surface / Background
    // ═══════════════════════════════════════════
    public static Color Background => IsDark ? FromHex("#202020") : FromHex("#F5F7FA");
    public static Color Surface => IsDark ? FromHex("#2D2D2D") : Color.White;
    public static Color Card => IsDark ? FromHex("#383838") : Color.White;
    public static Color Border => IsDark ? FromHex("#404040") : FromHex("#E8ECF1");
    public static Color Divider => IsDark ? FromHex("#333333") : FromHex("#F0F0F0");
    public static Color ManualEdit => IsDark ? FromHex("#5C4800") : FromHex("#FFF9C4");

    // Grid / Table 전용
    public static Color HeaderBg => IsDark ? FromHex("#2D2D2D") : FromHex("#F8FAFC");
    public static Color RowAlt => IsDark ? FromHex("#333333") : FromHex("#FAFBFC");
    public static Color HoverBg => IsDark ? FromHex("#3B3B3B") : FromHex("#F0F7FF");
    public static Color SelectedBg => IsDark ? FromHex("#1E3A5F") : FromHex("#E3F2FD");

    // 깊이감 (2025 추가)
    public static Color ShadowLight => IsDark
        ? Color.FromArgb(20, 0, 0, 0)
        : Color.FromArgb(8, 0, 0, 0);
    public static Color CardHover => IsDark ? FromHex("#3B3B3B") : FromHex("#F8F9FB");
    public static Color SubtleBg => IsDark ? FromHex("#333333") : FromHex("#F0F2F5");
    public static Color EditorBg => IsDark ? FromHex("#1E1E1E") : Color.FromArgb(252, 252, 248);

    // ═══════════════════════════════════════════
    // Text
    // ═══════════════════════════════════════════
    public static Color Text => IsDark ? FromHex("#E4E4E7") : FromHex("#1A1A2E");
    public static Color TextSecondary => IsDark ? FromHex("#A1A1AA") : FromHex("#6B7280");
    public static Color TextTertiary => IsDark ? FromHex("#71717A") : FromHex("#9CA3AF");
    public static Color TextWhite => Color.White;

    // ═══════════════════════════════════════════
    // Navigation
    // ═══════════════════════════════════════════
    public static Color NavDefault => IsDark ? FromHex("#71717A") : FromHex("#9CA3AF");
    public static Color NavHover => IsDark ? FromHex("#A1A1AA") : FromHex("#6B7280");
    public static Color NavHoverBg => IsDark ? FromHex("#383838") : FromHex("#F5F7FA");
    public static Color NavActive => IsDark ? FromHex("#60A5FA") : FromHex("#1976D2");
    public static Color NavActiveBg => IsDark ? FromHex("#1E3A5F") : FromHex("#E3F2FD");

    // ═══════════════════════════════════════════
    // Payment Tags (배경, 글자)
    // ═══════════════════════════════════════════
    public static (Color Bg, Color Fg) PaymentCard => IsDark
        ? (FromHex("#172554"), FromHex("#93C5FD"))
        : (FromHex("#E3F2FD"), FromHex("#1565C0"));
    public static (Color Bg, Color Fg) PaymentCash => IsDark
        ? (FromHex("#052E16"), FromHex("#4ADE80"))
        : (FromHex("#E8F5E9"), FromHex("#2E7D32"));
    public static (Color Bg, Color Fg) PaymentTransfer => IsDark
        ? (FromHex("#422006"), FromHex("#FBBF24"))
        : (FromHex("#FFF3E0"), FromHex("#E65100"));
    public static (Color Bg, Color Fg) PaymentExpense => IsDark
        ? (FromHex("#450A0A"), FromHex("#F87171"))
        : (FromHex("#FFEBEE"), FromHex("#C62828"));

    // ═══════════════════════════════════════════
    // Attendance (출퇴근)
    // ═══════════════════════════════════════════
    public static Color OnTime => IsDark ? FromHex("#93C5FD") : FromHex("#1565C0");
    public static Color Late => IsDark ? FromHex("#F87171") : FromHex("#C62828");
    public static Color MissingRecord => IsDark ? FromHex("#71717A") : FromHex("#9E9E9E");

    // ═══════════════════════════════════════════
    // Employee Schedule Colors (타임테이블 블록)
    // ═══════════════════════════════════════════
    public static Color[] EmployeeColors => IsDark
        ?
        [
            FromHex("#1E3A5F"),  // 어두운 파랑
            FromHex("#1A3B2A"),  // 어두운 초록
            FromHex("#3D1F2E"),  // 어두운 분홍
            FromHex("#3D2B10"),  // 어두운 주황
            FromHex("#2D1F3D"),  // 어두운 보라
            FromHex("#1A3340"),  // 어두운 하늘
            FromHex("#3D3A10"),  // 어두운 노랑
            FromHex("#2D2520"),  // 어두운 갈색
        ]
        :
        [
            FromHex("#BBDEFB"),  // 연파랑
            FromHex("#C8E6C9"),  // 연초록
            FromHex("#F8BBD0"),  // 연분홍
            FromHex("#FFE0B2"),  // 연주황
            FromHex("#E1BEE7"),  // 연보라
            FromHex("#B2EBF2"),  // 연하늘
            FromHex("#FFF59D"),  // 연노랑
            FromHex("#BCAAA4"),  // 연갈색
        ];

    public static Color GetEmployeeColor(int index) =>
        EmployeeColors[index % EmployeeColors.Length];

    // ═══════════════════════════════════════════
    // Summary Card Accent (통계 카드 아이콘 배경)
    // ═══════════════════════════════════════════
    public static (Color Light, Color Main) AccentBlue => IsDark
        ? (FromHex("#172554"), FromHex("#60A5FA"))
        : (FromHex("#E3F2FD"), FromHex("#1976D2"));
    public static (Color Light, Color Main) AccentGreen => IsDark
        ? (FromHex("#052E16"), FromHex("#4ADE80"))
        : (FromHex("#E8F5E9"), FromHex("#4CAF50"));
    public static (Color Light, Color Main) AccentOrange => IsDark
        ? (FromHex("#422006"), FromHex("#FBBF24"))
        : (FromHex("#FFF3E0"), FromHex("#FF9800"));
    public static (Color Light, Color Main) AccentRed => IsDark
        ? (FromHex("#450A0A"), FromHex("#F87171"))
        : (FromHex("#FFEBEE"), FromHex("#F44336"));

    // ═══════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════
    private static Color FromHex(string hex) => ColorTranslator.FromHtml(hex);
}
