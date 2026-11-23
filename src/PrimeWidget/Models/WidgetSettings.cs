namespace PrimeWidget.Models;

public enum WidgetViewMode
{
    Combined,
    AnalogOnly,
    DigitalOnly
}

public enum WidgetThemeMode
{
    DarkGlass,
    LightGlass
}

public enum BackdropStyle
{
    None,       // Pure software glassmorphism with opacity slider
    Acrylic,    // Windows 11/10 Native Acrylic Desktop Blur
    Mica,       // Windows 11 Native Mica Wallpaper Tint
    MicaAlt     // Windows 11 Tabbed Mica Alt
}

public enum CalendarSystem
{
    Dual,
    Ethiopian,
    Gregorian
}

public enum EthiopianScriptMode
{
    Amharic,
    English
}

public enum TimeFormatMode
{
    Standard12H,
    Standard24H,
    Ethiopian12H
}

public enum AccentColorTheme
{
    Monochrome,
    Red,
    Cyan,
    Emerald,
    Gold,
    Purple
}

public enum AnalogDialStyle
{
    Minimalist,      // Clean ticks like reference
    EthiopianGeEz,   // Authentic Ge'ez numerals (፲፪, ፫, ፮, ፱) + Ethiopian time
    RomanNumerals,   // Classic Roman numerals (XII, III, VI, IX)
    ModernDigits     // Crisp modern numerals (12, 3, 6, 9)
}

public enum NumeralSystem
{
    WesternArabic, // 1, 2, 3...
    EthiopianGeEz  // ፩, ፪, ፫, ፲, ፶፬...
}

public enum HourlyChimeSound
{
    AcousticBell,
    CrystalDigital,
    ChurchBell
}

public class WidgetSettings
{
    public WidgetViewMode ViewMode { get; set; } = WidgetViewMode.Combined;
    public WidgetThemeMode ThemeMode { get; set; } = WidgetThemeMode.DarkGlass;
    public BackdropStyle Backdrop { get; set; } = BackdropStyle.Acrylic;
    public bool SyncWithWindowsTheme { get; set; } = false;
    public double WidgetScale { get; set; } = 1.0;
    public bool IsClickThrough { get; set; } = false;
    public bool EnableGlobalHotkey { get; set; } = true;
    public AccentColorTheme Accent { get; set; } = AccentColorTheme.Monochrome;
    public AnalogDialStyle DialStyle { get; set; } = AnalogDialStyle.Minimalist;
    public CalendarSystem Calendar { get; set; } = CalendarSystem.Dual;
    public EthiopianScriptMode ScriptMode { get; set; } = EthiopianScriptMode.Amharic;
    public TimeFormatMode TimeFormat { get; set; } = TimeFormatMode.Standard12H;
    public NumeralSystem Numerals { get; set; } = NumeralSystem.WesternArabic;
    public double Opacity { get; set; } = 0.0;
    public bool SmoothSweepSecondHand { get; set; } = true;
    public bool ShowSeconds { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = false;
    public bool StartWithWindows { get; set; } = true;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;

    // World Timezone
    public bool ShowWorldTimezone { get; set; } = false;
    public string WorldTimeZoneId { get; set; } = "E. Africa Standard Time";
    public string WorldTimeZoneLabel { get; set; } = "Addis Ababa";
    public List<WorldClockZone> WorldZones { get; set; } = new()
    {
        new WorldClockZone("Eastern Standard Time", "New York", "🇺🇸"),
        new WorldClockZone("GMT Standard Time", "London", "🇬🇧"),
        new WorldClockZone("E. Africa Standard Time", "Addis Ababa", "🇪🇹")
    };

    // Focus / Pomodoro Mode
    public int FocusDurationMinutes { get; set; } = 25;
    public int BreakDurationMinutes { get; set; } = 5;

    // Hourly Chime
    public bool EnableHourlyChime { get; set; } = false;
    public bool ChimeQuietHoursEnabled { get; set; } = true;
    public int ChimeQuietStartHour { get; set; } = 22; // 10 PM
    public int ChimeQuietEndHour { get; set; } = 7;    // 7 AM
    public HourlyChimeSound ChimeSound { get; set; } = HourlyChimeSound.AcousticBell;
}

public class WorldClockZone
{
    public string TimeZoneId { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = string.Empty;

    public WorldClockZone() { }

    public WorldClockZone(string timeZoneId, string displayLabel, string flagEmoji = "")
    {
        TimeZoneId = timeZoneId;
        DisplayLabel = displayLabel;
        FlagEmoji = flagEmoji;
    }
}
