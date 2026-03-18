using System.Drawing;

namespace CubeManager.Helpers;

public static class ColorPalette
{
    // Base
    public static readonly Color Primary = ColorTranslator.FromHtml("#1976D2");
    public static readonly Color Success = ColorTranslator.FromHtml("#4CAF50");
    public static readonly Color Warning = ColorTranslator.FromHtml("#FFC107");
    public static readonly Color Danger = ColorTranslator.FromHtml("#F44336");
    public static readonly Color Info = ColorTranslator.FromHtml("#2196F3");
    public static readonly Color Background = ColorTranslator.FromHtml("#FAFAFA");
    public static readonly Color Card = Color.White;
    public static readonly Color Border = ColorTranslator.FromHtml("#E0E0E0");
    public static readonly Color Text = ColorTranslator.FromHtml("#212121");
    public static readonly Color TextSecondary = ColorTranslator.FromHtml("#757575");
    public static readonly Color ManualEdit = ColorTranslator.FromHtml("#FFF9C4");

    // Payment Tags
    public static readonly (Color Bg, Color Fg) PaymentCard =
        (ColorTranslator.FromHtml("#E3F2FD"), ColorTranslator.FromHtml("#1565C0"));
    public static readonly (Color Bg, Color Fg) PaymentCash =
        (ColorTranslator.FromHtml("#E8F5E9"), ColorTranslator.FromHtml("#2E7D32"));
    public static readonly (Color Bg, Color Fg) PaymentTransfer =
        (ColorTranslator.FromHtml("#FFF8E1"), ColorTranslator.FromHtml("#F57F17"));
    public static readonly (Color Bg, Color Fg) PaymentExpense =
        (ColorTranslator.FromHtml("#FFEBEE"), ColorTranslator.FromHtml("#C62828"));

    // Attendance
    public static readonly Color OnTime = ColorTranslator.FromHtml("#1565C0");
    public static readonly Color Late = ColorTranslator.FromHtml("#C62828");
    public static readonly Color MissingRecord = ColorTranslator.FromHtml("#9E9E9E");

    // Employee Schedule Colors
    public static readonly Color[] EmployeeColors =
    [
        ColorTranslator.FromHtml("#BBDEFB"),
        ColorTranslator.FromHtml("#C8E6C9"),
        ColorTranslator.FromHtml("#F8BBD0"),
        ColorTranslator.FromHtml("#FFE0B2"),
        ColorTranslator.FromHtml("#E1BEE7"),
        ColorTranslator.FromHtml("#B2EBF2"),
        ColorTranslator.FromHtml("#FFF59D"),
        ColorTranslator.FromHtml("#BCAAA4"),
    ];

    public static Color GetEmployeeColor(int index) =>
        EmployeeColors[index % EmployeeColors.Length];
}
