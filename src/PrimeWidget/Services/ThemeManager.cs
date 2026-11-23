using System;
using System.Windows;
using System.Windows.Media;
using PrimeWidget.Models;

namespace PrimeWidget.Services;

public static class ThemeManager
{
    public static void ApplyTheme(WidgetThemeMode theme, AccentColorTheme accent, double opacity, BackdropStyle backdrop = BackdropStyle.Acrylic)
    {
        var res = Application.Current.Resources;
        // Clamp between 0 and 1.0 (allowing pure 0% absolute transparency)
        double clampedOpacity = Math.Clamp(opacity, 0.0, 1.0);
        
        // When native Acrylic or Mica backdrop is used, lower the brush alpha slightly
        // so the actual blurred desktop wallpaper shows through with stunning glass depth.
        double effectiveOpacity = (backdrop != BackdropStyle.None && clampedOpacity > 0.05)
            ? clampedOpacity * 0.72
            : clampedOpacity;

        byte alpha = (byte)(effectiveOpacity * 255.0);

        Color accentColor;
        Color accentGlowColor;

        if (accent == AccentColorTheme.Monochrome)
        {
            if (theme == WidgetThemeMode.DarkGlass)
            {
                accentColor = Color.FromRgb(255, 255, 255);
                accentGlowColor = Color.FromArgb(45, 255, 255, 255);
            }
            else
            {
                accentColor = Color.FromRgb(15, 23, 42);
                accentGlowColor = Color.FromArgb(35, 0, 0, 0);
            }
        }
        else
        {
            accentColor = accent switch
            {
                AccentColorTheme.Red => Color.FromRgb(239, 68, 68),
                AccentColorTheme.Cyan => Color.FromRgb(6, 182, 212),
                AccentColorTheme.Emerald => Color.FromRgb(16, 185, 129),
                AccentColorTheme.Gold => Color.FromRgb(245, 158, 11),
                AccentColorTheme.Purple => Color.FromRgb(168, 85, 247),
                _ => Color.FromRgb(255, 255, 255)
            };

            accentGlowColor = accent switch
            {
                AccentColorTheme.Red => Color.FromArgb(40, 239, 68, 68),
                AccentColorTheme.Cyan => Color.FromArgb(40, 6, 182, 212),
                AccentColorTheme.Emerald => Color.FromArgb(40, 16, 185, 129),
                AccentColorTheme.Gold => Color.FromArgb(40, 245, 158, 11),
                AccentColorTheme.Purple => Color.FromArgb(40, 168, 85, 247),
                _ => Color.FromArgb(40, 255, 255, 255)
            };
        }

        // Proportional border and shadow scaling down to pure 0 at 0% opacity
        double shadowFactor = clampedOpacity * 0.22;
        res["WidgetShadowOpacity"] = shadowFactor;

        if (theme == WidgetThemeMode.DarkGlass)
        {
            // Dark Frosted Glass (Smooth scaling down to 0% absolute transparency)
            byte borderAlpha = (byte)Math.Min(110, (int)(alpha * 0.45));
            byte clockBorderAlpha = (byte)Math.Min(50, (int)(alpha * 0.20));
            byte clockFaceAlpha = (byte)Math.Min(170, (int)(alpha * 0.80));
            byte clockCapAlpha = (byte)Math.Min(220, (int)(alpha * 0.90));
            byte subtleGlowAlpha = (byte)Math.Min(25, (int)(alpha * 0.10));

            res["WidgetBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(alpha, 18, 20, 26));
            res["WidgetCardBrush"] = new SolidColorBrush(Color.FromArgb((byte)Math.Min(40, (int)(alpha * 0.35)), 255, 255, 255));
            res["WidgetCardHoverBrush"] = new SolidColorBrush(Color.FromArgb((byte)Math.Min(70, (int)(alpha * 0.50)), 255, 255, 255));
            res["WidgetBorderBrush"] = new SolidColorBrush(Color.FromArgb(borderAlpha, 255, 255, 255));
            res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            res["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            // Clock elements (face & border fade out completely at 0% opacity)
            res["ClockFaceBrush"] = new SolidColorBrush(Color.FromArgb(clockFaceAlpha, 24, 28, 38));
            res["ClockBorderBrush"] = new SolidColorBrush(Color.FromArgb(clockBorderAlpha, 255, 255, 255));
            res["ClockTicksBrush"] = new SolidColorBrush(Color.FromArgb(220, 203, 213, 225));
            res["ClockHourHandBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            res["ClockMinuteHandBrush"] = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            res["ClockCenterCapBrush"] = new SolidColorBrush(Color.FromArgb(clockCapAlpha, 30, 36, 48));
            res["SubtleGlowBrush"] = new SolidColorBrush(Color.FromArgb(subtleGlowAlpha, 255, 255, 255));
            res["OverlayToolbarBg"] = new SolidColorBrush(Color.FromArgb(220, 18, 20, 26));
        }
        else
        {
            // Light Frosted Glass (Smooth scaling down to 0% absolute transparency)
            byte borderAlpha = (byte)Math.Min(160, (int)(alpha * 0.50));
            byte clockBorderAlpha = (byte)Math.Min(80, (int)(alpha * 0.30));
            byte clockFaceAlpha = (byte)Math.Min(180, (int)(alpha * 0.85));
            byte clockCapAlpha = (byte)Math.Min(230, (int)(alpha * 0.90));
            byte subtleGlowAlpha = (byte)Math.Min(25, (int)(alpha * 0.10));

            res["WidgetBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(alpha, 250, 250, 252));
            res["WidgetCardBrush"] = new SolidColorBrush(Color.FromArgb((byte)Math.Min(60, (int)(alpha * 0.35)), 0, 0, 0));
            res["WidgetCardHoverBrush"] = new SolidColorBrush(Color.FromArgb((byte)Math.Min(90, (int)(alpha * 0.50)), 0, 0, 0));
            res["WidgetBorderBrush"] = new SolidColorBrush(Color.FromArgb(borderAlpha, 255, 255, 255));
            res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(51, 65, 85));
            res["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));

            // Clock elements (face & border fade out completely at 0% opacity)
            res["ClockFaceBrush"] = new SolidColorBrush(Color.FromArgb(clockFaceAlpha, 240, 244, 248));
            res["ClockBorderBrush"] = new SolidColorBrush(Color.FromArgb(clockBorderAlpha, 203, 213, 225));
            res["ClockTicksBrush"] = new SolidColorBrush(Color.FromArgb(220, 71, 85, 105));
            res["ClockHourHandBrush"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            res["ClockMinuteHandBrush"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            res["ClockCenterCapBrush"] = new SolidColorBrush(Color.FromArgb(clockCapAlpha, 255, 255, 255));
            res["SubtleGlowBrush"] = new SolidColorBrush(Color.FromArgb(subtleGlowAlpha, 0, 0, 0));
            res["OverlayToolbarBg"] = new SolidColorBrush(Color.FromArgb(220, 250, 250, 252));
        }

        res["AccentColor"] = accentColor;
        res["AccentBrush"] = new SolidColorBrush(accentColor);
        res["AccentGlowBrush"] = new SolidColorBrush(accentGlowColor);
        res["SecondHandBrush"] = (accent == AccentColorTheme.Monochrome)
            ? (theme == WidgetThemeMode.DarkGlass ? new SolidColorBrush(Color.FromRgb(255, 255, 255)) : new SolidColorBrush(Color.FromRgb(15, 23, 42)))
            : new SolidColorBrush(accentColor);
    }
}
