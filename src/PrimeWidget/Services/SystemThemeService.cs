using System;
using Microsoft.Win32;

namespace PrimeWidget.Services;

public class SystemThemeService : IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    public event Action<bool>? ThemeChanged; // true = Dark, false = Light

    public SystemThemeService()
    {
        try
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }
        catch { }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color)
        {
            ThemeChanged?.Invoke(IsWindowsInDarkMode());
        }
    }

    public static bool IsWindowsInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            if (key?.GetValue(AppsUseLightThemeValue) is int lightThemeInt)
            {
                return lightThemeInt == 0; // 0 = Dark, 1 = Light
            }
        }
        catch { }

        return true; // Default to Dark
    }

    public void Dispose()
    {
        try
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }
        catch { }
    }
}
