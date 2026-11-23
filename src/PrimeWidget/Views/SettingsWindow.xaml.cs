using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PrimeWidget.Models;
using PrimeWidget.ViewModels;

namespace PrimeWidget.Views;

public partial class SettingsWindow : Window
{
    private readonly TimeWidgetViewModel _viewModel;
    private readonly WidgetWindow _widgetWindow;
    private bool _isExplicitExit;

    // DWM Immersive Dark Mode API
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    // VS Code Dark Theme Selection Palette
    private static readonly SolidColorBrush ActiveBg = new(Color.FromRgb(55, 55, 61));      // #37373D (VS Code tree selection)
    private static readonly SolidColorBrush ActiveFg = new(Color.FromRgb(255, 255, 255));   // #FFFFFF
    private static readonly SolidColorBrush ActiveBorder = new(Color.FromRgb(0, 122, 204)); // #007ACC (VS Code focus border)

    private static readonly SolidColorBrush InactiveBg = new(Color.FromRgb(45, 45, 45));    // #2D2D2D
    private static readonly SolidColorBrush InactiveFg = new(Color.FromRgb(204, 204, 204)); // #CCCCCC
    private static readonly SolidColorBrush InactiveBorder = new(Color.FromRgb(62, 62, 66)); // #3E3E42

    public SettingsWindow(TimeWidgetViewModel viewModel, WidgetWindow widgetWindow)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _widgetWindow = widgetWindow;
        DataContext = _viewModel;

        Loaded += (s, e) => UpdateActiveButtons();
        Closing += OnSettingsClosing;
        _viewModel.PropertyChanged += (s, e) => UpdateActiveButtons();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            int darkMode = 1;
            if (DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
            }

            int captionColor = 0x00262525; // #252526 in BGR
            DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            int textColor = 0x00FFFFFF;
            DwmSetWindowAttribute(handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));

            AdjustWindowSizeForScreen();
        }
        catch
        {
            // Graceful fallback on older Windows
        }
    }

    private void AdjustWindowSizeForScreen()
    {
        try
        {
            var workArea = SystemParameters.WorkArea;
            // On 13" laptop displays (e.g. 1366x768 or 1080p @ 125%/150%), workArea is ~1280x680.
            // Ensure window fits comfortably on screen without spilling into taskbar or edges
            double maxAllowedHeight = Math.Max(480, workArea.Height * 0.90);
            double maxAllowedWidth = Math.Max(760, workArea.Width * 0.94);

            if (Height > maxAllowedHeight)
            {
                Height = maxAllowedHeight;
            }

            if (Width > maxAllowedWidth)
            {
                Width = maxAllowedWidth;
            }

            Left = (workArea.Width - Width) / 2 + workArea.Left;
            Top = (workArea.Height - Height) / 2 + workArea.Top;
        }
        catch
        {
            // Fallback to default position
        }
    }

    private TimeWidgetViewModel Vm => _viewModel;

    private void SetButtonActive(Button? btn, bool active)
    {
        if (btn == null) return;
        btn.Background = active ? ActiveBg : InactiveBg;
        btn.Foreground = active ? ActiveFg : InactiveFg;
        btn.BorderBrush = active ? ActiveBorder : InactiveBorder;
        btn.FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium;
    }

    public void UpdateActiveButtons()
    {
        // View Mode
        SetButtonActive(BtnViewCombined, Vm.ViewMode == WidgetViewMode.Combined);
        SetButtonActive(BtnViewAnalog, Vm.ViewMode == WidgetViewMode.AnalogOnly);
        SetButtonActive(BtnViewDigital, Vm.ViewMode == WidgetViewMode.DigitalOnly);

        // Dial Style
        SetButtonActive(BtnDialMinimalist, Vm.DialStyle == AnalogDialStyle.Minimalist);
        SetButtonActive(BtnDialEthiopian, Vm.DialStyle == AnalogDialStyle.EthiopianGeEz);
        SetButtonActive(BtnDialRoman, Vm.DialStyle == AnalogDialStyle.RomanNumerals);
        SetButtonActive(BtnDialModern, Vm.DialStyle == AnalogDialStyle.ModernDigits);

        // Calendar
        SetButtonActive(BtnCalDual, Vm.Calendar == CalendarSystem.Dual);
        SetButtonActive(BtnCalEthiopian, Vm.Calendar == CalendarSystem.Ethiopian);
        SetButtonActive(BtnCalGregorian, Vm.Calendar == CalendarSystem.Gregorian);

        // Windows 11 Backdrop
        SetButtonActive(BtnBackdropAcrylic, Vm.Backdrop == BackdropStyle.Acrylic);
        SetButtonActive(BtnBackdropMica, Vm.Backdrop == BackdropStyle.Mica);
        SetButtonActive(BtnBackdropMicaAlt, Vm.Backdrop == BackdropStyle.MicaAlt);
        SetButtonActive(BtnBackdropNone, Vm.Backdrop == BackdropStyle.None);

        // Visual Theme
        SetButtonActive(BtnThemeDark, Vm.ThemeMode == WidgetThemeMode.DarkGlass);
        SetButtonActive(BtnThemeLight, Vm.ThemeMode == WidgetThemeMode.LightGlass);

        // Accent
        SetButtonActive(BtnAccentMonochrome, Vm.Accent == AccentColorTheme.Monochrome);
        SetButtonActive(BtnAccentSilver, Vm.Accent == AccentColorTheme.Cyan);
        SetButtonActive(BtnAccentRed, Vm.Accent == AccentColorTheme.Red);
        SetButtonActive(BtnAccentGold, Vm.Accent == AccentColorTheme.Gold);

        // Scale Presets
        SetButtonActive(BtnScaleCompact, Math.Abs(Vm.WidgetScale - 0.85) < 0.03);
        SetButtonActive(BtnScaleStandard, Math.Abs(Vm.WidgetScale - 1.0) < 0.03);
        SetButtonActive(BtnScaleHero, Math.Abs(Vm.WidgetScale - 1.25) < 0.03);

        // Script Mode
        SetButtonActive(BtnScriptAmharic, Vm.ScriptMode == EthiopianScriptMode.Amharic);
        SetButtonActive(BtnScriptEnglish, Vm.ScriptMode == EthiopianScriptMode.English);

        // Time Format
        SetButtonActive(BtnTime12H, Vm.TimeFormat == TimeFormatMode.Standard12H);
        SetButtonActive(BtnTime24H, Vm.TimeFormat == TimeFormatMode.Standard24H);
        SetButtonActive(BtnTimeEth12H, Vm.TimeFormat == TimeFormatMode.Ethiopian12H);

        // Numeral System
        SetButtonActive(BtnNumeralsWestern, Vm.Numerals == NumeralSystem.WesternArabic);
        SetButtonActive(BtnNumeralsGeez, Vm.Numerals == NumeralSystem.EthiopianGeEz);

        // Opacity Presets
        SetButtonActive(BtnOpacityZero, Vm.Opacity < 0.05);
        SetButtonActive(BtnOpacity30, Math.Abs(Vm.Opacity - 0.30) < 0.05);
        SetButtonActive(BtnOpacity60, Math.Abs(Vm.Opacity - 0.60) < 0.05);
        SetButtonActive(BtnOpacity90, Math.Abs(Vm.Opacity - 0.90) < 0.05);

        // Chime Sounds
        SetButtonActive(BtnChimeAcoustic, Vm.ChimeSound == HourlyChimeSound.AcousticBell);
        SetButtonActive(BtnChimeCrystal, Vm.ChimeSound == HourlyChimeSound.CrystalDigital);
        SetButtonActive(BtnChimeChurch, Vm.ChimeSound == HourlyChimeSound.ChurchBell);

        // Timezones List default selection
        if (LstTimeZones != null && LstTimeZones.SelectedIndex < 0 && LstTimeZones.Items.Count > 0)
        {
            LstTimeZones.SelectedIndex = 0;
        }
    }

    private void OnTitleMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnTitleMaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnTitleCloseClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnSettingsClosing(object? sender, CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true;
            Hide();
        }
    }

    public void ShutdownApplication()
    {
        _isExplicitExit = true;
        Close();
    }

    private void OnCloseWindowClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnSaveAndApplyClick(object sender, RoutedEventArgs e)
    {
        Vm.Save();
        _widgetWindow.Show();
        _widgetWindow.Activate();
    }

    private void OnToggleWidgetVisibilityClick(object sender, RoutedEventArgs e)
    {
        if (_widgetWindow.IsVisible)
        {
            _widgetWindow.Hide();
        }
        else
        {
            _widgetWindow.Show();
            _widgetWindow.Activate();
        }
    }

    // View Mode
    private void OnCombinedViewClick(object sender, RoutedEventArgs e) { Vm.ViewMode = WidgetViewMode.Combined; UpdateActiveButtons(); }
    private void OnAnalogViewClick(object sender, RoutedEventArgs e) { Vm.ViewMode = WidgetViewMode.AnalogOnly; UpdateActiveButtons(); }
    private void OnDigitalViewClick(object sender, RoutedEventArgs e) { Vm.ViewMode = WidgetViewMode.DigitalOnly; UpdateActiveButtons(); }

    // Analog Dial Style
    private void OnMinimalistDialClick(object sender, RoutedEventArgs e) { Vm.DialStyle = AnalogDialStyle.Minimalist; UpdateActiveButtons(); }
    private void OnEthiopianDialClick(object sender, RoutedEventArgs e) { Vm.DialStyle = AnalogDialStyle.EthiopianGeEz; UpdateActiveButtons(); }
    private void OnRomanDialClick(object sender, RoutedEventArgs e) { Vm.DialStyle = AnalogDialStyle.RomanNumerals; UpdateActiveButtons(); }
    private void OnModernDigitsClick(object sender, RoutedEventArgs e) { Vm.DialStyle = AnalogDialStyle.ModernDigits; UpdateActiveButtons(); }

    // Backdrop
    private void OnBackdropAcrylicClick(object sender, RoutedEventArgs e) { Vm.Backdrop = BackdropStyle.Acrylic; UpdateActiveButtons(); }
    private void OnBackdropMicaClick(object sender, RoutedEventArgs e) { Vm.Backdrop = BackdropStyle.Mica; UpdateActiveButtons(); }
    private void OnBackdropMicaAltClick(object sender, RoutedEventArgs e) { Vm.Backdrop = BackdropStyle.MicaAlt; UpdateActiveButtons(); }
    private void OnBackdropNoneClick(object sender, RoutedEventArgs e) { Vm.Backdrop = BackdropStyle.None; UpdateActiveButtons(); }

    // Themes
    private void OnLightThemeClick(object sender, RoutedEventArgs e) { Vm.ThemeMode = WidgetThemeMode.LightGlass; UpdateActiveButtons(); }
    private void OnDarkThemeClick(object sender, RoutedEventArgs e) { Vm.ThemeMode = WidgetThemeMode.DarkGlass; UpdateActiveButtons(); }

    // Accents
    private void OnMonochromeAccentClick(object sender, RoutedEventArgs e) { Vm.Accent = AccentColorTheme.Monochrome; UpdateActiveButtons(); }
    private void OnSilverAccentClick(object sender, RoutedEventArgs e) { Vm.Accent = AccentColorTheme.Cyan; UpdateActiveButtons(); }
    private void OnRedAccentClick(object sender, RoutedEventArgs e) { Vm.Accent = AccentColorTheme.Red; UpdateActiveButtons(); }
    private void OnGoldAccentClick(object sender, RoutedEventArgs e) { Vm.Accent = AccentColorTheme.Gold; UpdateActiveButtons(); }

    // Scale
    private void OnScaleCompactClick(object sender, RoutedEventArgs e) { Vm.WidgetScale = 0.85; UpdateActiveButtons(); }
    private void OnScaleStandardClick(object sender, RoutedEventArgs e) { Vm.WidgetScale = 1.0; UpdateActiveButtons(); }
    private void OnScaleHeroClick(object sender, RoutedEventArgs e) { Vm.WidgetScale = 1.25; UpdateActiveButtons(); }

    // Calendars
    private void OnDualCalendarClick(object sender, RoutedEventArgs e) { Vm.Calendar = CalendarSystem.Dual; UpdateActiveButtons(); }
    private void OnEthiopianCalendarClick(object sender, RoutedEventArgs e) { Vm.Calendar = CalendarSystem.Ethiopian; UpdateActiveButtons(); }
    private void OnGregorianCalendarClick(object sender, RoutedEventArgs e) { Vm.Calendar = CalendarSystem.Gregorian; UpdateActiveButtons(); }

    // Scripts
    private void OnAmharicScriptClick(object sender, RoutedEventArgs e) { Vm.ScriptMode = EthiopianScriptMode.Amharic; UpdateActiveButtons(); }
    private void OnEnglishScriptClick(object sender, RoutedEventArgs e) { Vm.ScriptMode = EthiopianScriptMode.English; UpdateActiveButtons(); }

    // Time Formats
    private void On12HourClick(object sender, RoutedEventArgs e) { Vm.TimeFormat = TimeFormatMode.Standard12H; UpdateActiveButtons(); }
    private void On24HourClick(object sender, RoutedEventArgs e) { Vm.TimeFormat = TimeFormatMode.Standard24H; UpdateActiveButtons(); }
    private void OnEth12HourClick(object sender, RoutedEventArgs e) { Vm.TimeFormat = TimeFormatMode.Ethiopian12H; UpdateActiveButtons(); }

    // Numeral Systems
    private void OnWesternNumeralsClick(object sender, RoutedEventArgs e) { Vm.Numerals = NumeralSystem.WesternArabic; UpdateActiveButtons(); }
    private void OnGeezNumeralsClick(object sender, RoutedEventArgs e) { Vm.Numerals = NumeralSystem.EthiopianGeEz; UpdateActiveButtons(); }

    // Opacity Presets
    private void OnOpacityZeroClick(object sender, RoutedEventArgs e) { Vm.Opacity = 0.0; UpdateActiveButtons(); }
    private void OnOpacity30Click(object sender, RoutedEventArgs e) { Vm.Opacity = 0.30; UpdateActiveButtons(); }
    private void OnOpacity60Click(object sender, RoutedEventArgs e) { Vm.Opacity = 0.60; UpdateActiveButtons(); }
    private void OnOpacity90Click(object sender, RoutedEventArgs e) { Vm.Opacity = 0.90; UpdateActiveButtons(); }

    // Chime Sounds
    private void OnChimeAcousticClick(object sender, RoutedEventArgs e) { Vm.ChimeSound = HourlyChimeSound.AcousticBell; UpdateActiveButtons(); }
    private void OnChimeCrystalClick(object sender, RoutedEventArgs e) { Vm.ChimeSound = HourlyChimeSound.CrystalDigital; UpdateActiveButtons(); }
    private void OnChimeChurchClick(object sender, RoutedEventArgs e) { Vm.ChimeSound = HourlyChimeSound.ChurchBell; UpdateActiveButtons(); }

    // World Timezones
    private void OnAddSelectedZoneClick(object sender, RoutedEventArgs e)
    {
        if (LstTimeZones.SelectedItem is TimeZoneDisplayItem item)
        {
            Vm.AddWorldZone(item);
        }
        else if (Vm.FilteredTimeZones.Count > 0)
        {
            Vm.AddWorldZone(Vm.FilteredTimeZones[0]);
        }
    }

    private void OnTimeZoneListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstTimeZones.SelectedItem is TimeZoneDisplayItem item)
        {
            Vm.AddWorldZone(item);
        }
    }

    private void OnClearSearchClick(object sender, RoutedEventArgs e)
    {
        Vm.ClearTimeZoneSearch();
        TxtSearchTimeZone.Focus();
    }

    private void OnQuickAddCityClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cityCode)
        {
            switch (cityCode)
            {
                case "ADDIS":
                    Vm.QuickAddZone("E. Africa Standard Time", "Addis Ababa", "🇪🇹");
                    break;
                case "NYC":
                    Vm.QuickAddZone("Eastern Standard Time", "New York", "🇺🇸");
                    break;
                case "LON":
                    Vm.QuickAddZone("GMT Standard Time", "London", "🇬🇧");
                    break;
                case "DXB":
                    Vm.QuickAddZone("Arabian Standard Time", "Dubai", "🇦🇪");
                    break;
                case "TYO":
                    Vm.QuickAddZone("Tokyo Standard Time", "Tokyo", "🇯🇵");
                    break;
                case "NBO":
                    Vm.QuickAddZone("E. Africa Standard Time", "Nairobi", "🇰🇪");
                    break;
            }
        }
    }

    private void OnRemoveZoneClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn.DataContext is WorldClockDisplayItem clock)
            {
                Vm.RemoveWorldZone(clock);
            }
            else if (btn.Tag is string tzId)
            {
                Vm.RemoveWorldZone(tzId);
            }
        }
    }

    private void OnSortWorldZonesClick(object sender, RoutedEventArgs e)
    {
        Vm.SortWorldClocksByOffset();
    }
}
