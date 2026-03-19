using System.Drawing;

namespace CubeManager.Helpers;

/// <summary>
/// CubeManager 전역 색상 시스템.
/// design-system.md 기반, 모든 UI 컴포넌트에서 참조.
/// </summary>
public static class ColorPalette
{
    // ═══════════════════════════════════════════
    // Primary 계열 (파랑)
    // ═══════════════════════════════════════════
    public static readonly Color Primary = ColorTranslator.FromHtml("#1976D2");
    public static readonly Color Primary50 = ColorTranslator.FromHtml("#E3F2FD");
    public static readonly Color Primary100 = ColorTranslator.FromHtml("#BBDEFB");
    public static readonly Color Primary700 = ColorTranslator.FromHtml("#1565C0");
    public static readonly Color Primary900 = ColorTranslator.FromHtml("#0D47A1");

    // ═══════════════════════════════════════════
    // Semantic Colors
    // ═══════════════════════════════════════════
    public static readonly Color Success = ColorTranslator.FromHtml("#4CAF50");
    public static readonly Color SuccessLight = ColorTranslator.FromHtml("#E8F5E9");

    public static readonly Color Warning = ColorTranslator.FromHtml("#FF9800");
    public static readonly Color WarningLight = ColorTranslator.FromHtml("#FFF3E0");

    public static readonly Color Danger = ColorTranslator.FromHtml("#F44336");
    public static readonly Color DangerLight = ColorTranslator.FromHtml("#FFEBEE");

    public static readonly Color Info = ColorTranslator.FromHtml("#2196F3");
    public static readonly Color InfoLight = ColorTranslator.FromHtml("#E3F2FD");

    // ═══════════════════════════════════════════
    // Surface / Background
    // ═══════════════════════════════════════════
    public static readonly Color Background = ColorTranslator.FromHtml("#F5F7FA");
    public static readonly Color Surface = Color.White;
    public static readonly Color Card = Color.White;
    public static readonly Color Border = ColorTranslator.FromHtml("#E8ECF1");
    public static readonly Color Divider = ColorTranslator.FromHtml("#F0F0F0");
    public static readonly Color ManualEdit = ColorTranslator.FromHtml("#FFF9C4");

    // Grid / Table 전용
    public static readonly Color HeaderBg = ColorTranslator.FromHtml("#F8FAFC");
    public static readonly Color RowAlt = ColorTranslator.FromHtml("#FAFBFC");
    public static readonly Color HoverBg = ColorTranslator.FromHtml("#F0F7FF");
    public static readonly Color SelectedBg = ColorTranslator.FromHtml("#E3F2FD");

    // ═══════════════════════════════════════════
    // Text
    // ═══════════════════════════════════════════
    public static readonly Color Text = ColorTranslator.FromHtml("#1A1A2E");
    public static readonly Color TextSecondary = ColorTranslator.FromHtml("#6B7280");
    public static readonly Color TextTertiary = ColorTranslator.FromHtml("#9CA3AF");
    public static readonly Color TextWhite = Color.White;

    // ═══════════════════════════════════════════
    // Navigation
    // ═══════════════════════════════════════════
    public static readonly Color NavDefault = ColorTranslator.FromHtml("#9CA3AF");
    public static readonly Color NavHover = ColorTranslator.FromHtml("#6B7280");
    public static readonly Color NavHoverBg = ColorTranslator.FromHtml("#F5F7FA");
    public static readonly Color NavActive = ColorTranslator.FromHtml("#1976D2");
    public static readonly Color NavActiveBg = ColorTranslator.FromHtml("#E3F2FD");

    // ═══════════════════════════════════════════
    // Payment Tags (배경, 글자)
    // ═══════════════════════════════════════════
    public static readonly (Color Bg, Color Fg) PaymentCard =
        (ColorTranslator.FromHtml("#E3F2FD"), ColorTranslator.FromHtml("#1565C0"));
    public static readonly (Color Bg, Color Fg) PaymentCash =
        (ColorTranslator.FromHtml("#E8F5E9"), ColorTranslator.FromHtml("#2E7D32"));
    public static readonly (Color Bg, Color Fg) PaymentTransfer =
        (ColorTranslator.FromHtml("#FFF3E0"), ColorTranslator.FromHtml("#E65100"));
    public static readonly (Color Bg, Color Fg) PaymentExpense =
        (ColorTranslator.FromHtml("#FFEBEE"), ColorTranslator.FromHtml("#C62828"));

    // ═══════════════════════════════════════════
    // Attendance (출퇴근)
    // ═══════════════════════════════════════════
    public static readonly Color OnTime = ColorTranslator.FromHtml("#1565C0");
    public static readonly Color Late = ColorTranslator.FromHtml("#C62828");
    public static readonly Color MissingRecord = ColorTranslator.FromHtml("#9E9E9E");

    // ═══════════════════════════════════════════
    // Employee Schedule Colors (타임테이블 블록)
    // ═══════════════════════════════════════════
    public static readonly Color[] EmployeeColors =
    [
        ColorTranslator.FromHtml("#BBDEFB"),  // 연파랑
        ColorTranslator.FromHtml("#C8E6C9"),  // 연초록
        ColorTranslator.FromHtml("#F8BBD0"),  // 연분홍
        ColorTranslator.FromHtml("#FFE0B2"),  // 연주황
        ColorTranslator.FromHtml("#E1BEE7"),  // 연보라
        ColorTranslator.FromHtml("#B2EBF2"),  // 연하늘
        ColorTranslator.FromHtml("#FFF59D"),  // 연노랑
        ColorTranslator.FromHtml("#BCAAA4"),  // 연갈색
    ];

    public static Color GetEmployeeColor(int index) =>
        EmployeeColors[index % EmployeeColors.Length];

    // ═══════════════════════════════════════════
    // Summary Card Accent (통계 카드 아이콘 배경)
    // ═══════════════════════════════════════════
    public static readonly (Color Light, Color Main) AccentBlue =
        (ColorTranslator.FromHtml("#E3F2FD"), ColorTranslator.FromHtml("#1976D2"));
    public static readonly (Color Light, Color Main) AccentGreen =
        (ColorTranslator.FromHtml("#E8F5E9"), ColorTranslator.FromHtml("#4CAF50"));
    public static readonly (Color Light, Color Main) AccentOrange =
        (ColorTranslator.FromHtml("#FFF3E0"), ColorTranslator.FromHtml("#FF9800"));
    public static readonly (Color Light, Color Main) AccentRed =
        (ColorTranslator.FromHtml("#FFEBEE"), ColorTranslator.FromHtml("#F44336"));
}
