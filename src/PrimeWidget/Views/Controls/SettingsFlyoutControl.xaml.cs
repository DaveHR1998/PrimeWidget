using System.Windows;
using System.Windows.Controls;
using PrimeWidget.Models;
using PrimeWidget.ViewModels;

namespace PrimeWidget.Views.Controls;

public partial class SettingsFlyoutControl : UserControl
{
    public SettingsFlyoutControl()
    {
        InitializeComponent();
    }

    private TimeWidgetViewModel? Vm => DataContext as TimeWidgetViewModel;

    private void OnMinimalistDialClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.DialStyle = AnalogDialStyle.Minimalist;
    }

    private void OnEthiopianDialClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.DialStyle = AnalogDialStyle.EthiopianGeEz;
    }

    private void OnRomanDialClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.DialStyle = AnalogDialStyle.RomanNumerals;
    }

    private void OnModernDigitsClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.DialStyle = AnalogDialStyle.ModernDigits;
    }

    private void OnDarkThemeClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.ThemeMode = WidgetThemeMode.DarkGlass;
    }

    private void OnLightThemeClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.ThemeMode = WidgetThemeMode.LightGlass;
    }

    private void OnRedAccentClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.Accent = AccentColorTheme.Red;
    }

    private void OnCyanAccentClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.Accent = AccentColorTheme.Cyan;
    }

    private void OnEmeraldAccentClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.Accent = AccentColorTheme.Emerald;
    }

    private void OnGoldAccentClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.Accent = AccentColorTheme.Gold;
    }

    private void OnPurpleAccentClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.Accent = AccentColorTheme.Purple;
    }

    private void OnDualCalendarClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.Calendar = CalendarSystem.Dual;
    }

    private void OnEthiopianCalendarClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.Calendar = CalendarSystem.Ethiopian;
    }

    private void OnGregorianCalendarClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.Calendar = CalendarSystem.Gregorian;
    }

    private void OnAmharicScriptClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.ScriptMode = EthiopianScriptMode.Amharic;
    }

    private void OnEnglishScriptClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.ScriptMode = EthiopianScriptMode.English;
    }

    private void On12HourClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.TimeFormat = TimeFormatMode.Standard12H;
    }

    private void On24HourClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.TimeFormat = TimeFormatMode.Standard24H;
    }

    private void OnEth12HourClick(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.TimeFormat = TimeFormatMode.Ethiopian12H;
    }
}
