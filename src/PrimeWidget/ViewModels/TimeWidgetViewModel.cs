using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Input;
using PrimeWidget.Helpers;
using PrimeWidget.Models;
using PrimeWidget.Services;

namespace PrimeWidget.ViewModels;

public class TimeWidgetViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _timer;
    private readonly SystemThemeService _systemThemeService;
    private readonly HourlyChimeService _hourlyChimeService;

    public ObservableCollection<WorldClockDisplayItem> ActiveWorldClocks { get; } = new();
    public IReadOnlyList<TimeZoneInfo> AvailableSystemTimeZones { get; } = TimeZoneInfo.GetSystemTimeZones().ToList();
    public List<TimeZoneDisplayItem> AllTimeZones { get; } = new();
    public ObservableCollection<TimeZoneDisplayItem> FilteredTimeZones { get; } = new();

    private string _timeZoneSearchText = string.Empty;
    public string TimeZoneSearchText
    {
        get => _timeZoneSearchText;
        set
        {
            if (SetProperty(ref _timeZoneSearchText, value))
            {
                FilterTimeZones(value);
                OnPropertyChanged(nameof(HasSearchText));
                OnPropertyChanged(nameof(FilterCountStatus));
            }
        }
    }

    public bool HasSearchText => !string.IsNullOrWhiteSpace(_timeZoneSearchText);

    public string FilterCountStatus => string.IsNullOrWhiteSpace(_timeZoneSearchText)
        ? $"{AllTimeZones.Count} time zones available"
        : $"{FilteredTimeZones.Count} matches found";

    private TimeZoneDisplayItem? _selectedTimeZoneItem;
    public TimeZoneDisplayItem? SelectedTimeZoneItem
    {
        get => _selectedTimeZoneItem;
        set => SetProperty(ref _selectedTimeZoneItem, value);
    }

    private WidgetSettings _settings;
    private double _hourHandAngle;
    private double _minuteHandAngle;
    private double _secondHandAngle;
    private string _digitalTimeString = string.Empty;
    private string _stackedHourString = string.Empty;
    private string _stackedMinuteString = string.Empty;
    private string _secondsString = string.Empty;
    private string _amPmString = string.Empty;
    private string _dayOfWeekString = string.Empty;
    private string _dayNameUpperString = string.Empty;
    private string _primaryDateString = string.Empty;
    private string _secondaryDateString = string.Empty;
    private string _dualDateCombinedString = string.Empty;
    private string _watermarkHourString = string.Empty;
    private bool _isDualCalendar = true;
    private bool _isSettingsOpen;
    private string _timezoneDisplay = "Local";

    private bool _hasMinutes = true;
    private bool _hasSeconds = true;

    // Dial labels for 12, 3, 6, 9
    private string _dialLabel12 = string.Empty;
    private string _dialLabel3 = string.Empty;
    private string _dialLabel6 = string.Empty;
    private string _dialLabel9 = string.Empty;

    // World Timezone
    private string _worldTimeString = string.Empty;
    private string _worldTimePeriodString = string.Empty;

    // Focus / Pomodoro Timer
    private bool _isFocusMode;
    private bool _isFocusRunning;
    private bool _isBreakMode;
    private int _focusRemainingSeconds;
    private int _focusTotalSeconds;
    private DateTime _lastFocusTick = DateTime.UtcNow;
    private string _focusTimeRemainingString = "25:00";
    private string _focusStatusText = "Focus Session";

    public event Action? RequestWindowStyleUpdate;

    public TimeWidgetViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = _settingsService.LoadSettings();

        _systemThemeService = new SystemThemeService();
        _hourlyChimeService = new HourlyChimeService();

        // Focus mode initial setup
        _focusTotalSeconds = _settings.FocusDurationMinutes * 60;
        _focusRemainingSeconds = _focusTotalSeconds;
        UpdateFocusDisplay();

        // Commands
        CycleViewModeCommand = new RelayCommand(CycleViewMode);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        CycleCalendarCommand = new RelayCommand(CycleCalendar);
        CycleDialStyleCommand = new RelayCommand(CycleDialStyle);
        CycleNumeralSystemCommand = new RelayCommand(CycleNumeralSystem);
        TogglePinCommand = new RelayCommand(TogglePin);
        ToggleSettingsCommand = new RelayCommand(() => IsSettingsOpen = !IsSettingsOpen);
        ToggleClickThroughCommand = new RelayCommand(ToggleClickThrough);

        // Focus Commands
        ToggleFocusModeCommand = new RelayCommand(ToggleFocusMode);
        ToggleFocusRunningCommand = new RelayCommand(ToggleFocusRunning);
        ResetFocusCommand = new RelayCommand(ResetFocus);
        SetFocus15Command = new RelayCommand(() => SetFocusDuration(15));
        SetFocus25Command = new RelayCommand(() => SetFocusDuration(25));
        SetFocus45Command = new RelayCommand(() => SetFocusDuration(45));
        TestChimeCommand = new RelayCommand(TestChime);

        // Windows System Theme auto-sync
        _systemThemeService.ThemeChanged += OnWindowsThemeChanged;
        if (_settings.SyncWithWindowsTheme)
        {
            ApplyWindowsSystemTheme();
        }
        else
        {
            ThemeManager.ApplyTheme(_settings.ThemeMode, _settings.Accent, _settings.Opacity, _settings.Backdrop);
        }

        // Sync Windows startup registry setting
        AutostartService.SetAutostart(_settings.StartWithWindows);

        // Setup timer
        _timer = new DispatcherTimer(DispatcherPriority.Render);
        UpdateTimerInterval();
        _timer.Tick += (s, e) => UpdateTime();
        _timer.Start();

        InitializeTimeZones();
        SyncActiveWorldClocks();
        UpdateTime();
    }

    public WidgetSettings Settings => _settings;

    // Window Style / Backdrop / Scale / Click-Through Properties
    public BackdropStyle Backdrop
    {
        get => _settings.Backdrop;
        set
        {
            if (_settings.Backdrop != value)
            {
                _settings.Backdrop = value;
                ThemeManager.ApplyTheme(_settings.ThemeMode, _settings.Accent, _settings.Opacity, _settings.Backdrop);
                OnPropertyChanged();
                RequestWindowStyleUpdate?.Invoke();
                Save();
            }
        }
    }

    public bool SyncWithWindowsTheme
    {
        get => _settings.SyncWithWindowsTheme;
        set
        {
            if (_settings.SyncWithWindowsTheme != value)
            {
                _settings.SyncWithWindowsTheme = value;
                if (value) ApplyWindowsSystemTheme();
                OnPropertyChanged();
                Save();
            }
        }
    }

    public double WidgetScale
    {
        get => _settings.WidgetScale;
        set
        {
            double clamped = Math.Clamp(value, 0.7, 1.5);
            if (Math.Abs(_settings.WidgetScale - clamped) > 0.01)
            {
                _settings.WidgetScale = clamped;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool IsClickThrough
    {
        get => _settings.IsClickThrough;
        set
        {
            if (_settings.IsClickThrough != value)
            {
                _settings.IsClickThrough = value;
                OnPropertyChanged();
                RequestWindowStyleUpdate?.Invoke();
                Save();
            }
        }
    }

    public bool EnableGlobalHotkey
    {
        get => _settings.EnableGlobalHotkey;
        set
        {
            if (_settings.EnableGlobalHotkey != value)
            {
                _settings.EnableGlobalHotkey = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    // World Timezone
    public bool ShowWorldTimezone
    {
        get => _settings.ShowWorldTimezone;
        set
        {
            if (_settings.ShowWorldTimezone != value)
            {
                _settings.ShowWorldTimezone = value;
                OnPropertyChanged();
                UpdateTime();
                Save();
            }
        }
    }

    public string WorldTimeZoneId
    {
        get => _settings.WorldTimeZoneId;
        set
        {
            if (_settings.WorldTimeZoneId != value)
            {
                _settings.WorldTimeZoneId = value;
                OnPropertyChanged();
                UpdateTime();
                Save();
            }
        }
    }

    public string WorldTimeZoneLabel
    {
        get => _settings.WorldTimeZoneLabel;
        set
        {
            if (_settings.WorldTimeZoneLabel != value)
            {
                _settings.WorldTimeZoneLabel = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string WorldTimeString
    {
        get => _worldTimeString;
        private set => SetProperty(ref _worldTimeString, value);
    }

    public string WorldTimePeriodString
    {
        get => _worldTimePeriodString;
        private set => SetProperty(ref _worldTimePeriodString, value);
    }

    // Hourly Chime Properties
    public bool EnableHourlyChime
    {
        get => _settings.EnableHourlyChime;
        set
        {
            if (_settings.EnableHourlyChime != value)
            {
                _settings.EnableHourlyChime = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool ChimeQuietHoursEnabled
    {
        get => _settings.ChimeQuietHoursEnabled;
        set
        {
            if (_settings.ChimeQuietHoursEnabled != value)
            {
                _settings.ChimeQuietHoursEnabled = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public int ChimeQuietStartHour
    {
        get => _settings.ChimeQuietStartHour;
        set
        {
            if (_settings.ChimeQuietStartHour != value)
            {
                _settings.ChimeQuietStartHour = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public int ChimeQuietEndHour
    {
        get => _settings.ChimeQuietEndHour;
        set
        {
            if (_settings.ChimeQuietEndHour != value)
            {
                _settings.ChimeQuietEndHour = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public HourlyChimeSound ChimeSound
    {
        get => _settings.ChimeSound;
        set
        {
            if (_settings.ChimeSound != value)
            {
                _settings.ChimeSound = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    // Focus Mode Properties
    public bool IsFocusMode
    {
        get => _isFocusMode;
        set
        {
            if (SetProperty(ref _isFocusMode, value))
            {
                OnPropertyChanged(nameof(IsClockMode));
            }
        }
    }

    public bool IsClockMode => !IsFocusMode;

    public bool IsFocusRunning
    {
        get => _isFocusRunning;
        private set
        {
            if (SetProperty(ref _isFocusRunning, value))
            {
                OnPropertyChanged(nameof(FocusPlayButtonText));
                OnPropertyChanged(nameof(FocusPlayButtonIcon));
            }
        }
    }

    public string FocusPlayButtonText => IsFocusRunning ? "Pause" : "Start";
    public string FocusPlayButtonIcon => IsFocusRunning ? "⏸" : "▶";

    public string FocusTimeRemainingString
    {
        get => _focusTimeRemainingString;
        private set => SetProperty(ref _focusTimeRemainingString, value);
    }

    public string FocusStatusText
    {
        get => _focusStatusText;
        private set => SetProperty(ref _focusStatusText, value);
    }

    public double FocusProgress
    {
        get
        {
            if (_focusTotalSeconds <= 0) return 0;
            return Math.Clamp(1.0 - ((double)_focusRemainingSeconds / _focusTotalSeconds), 0.0, 1.0);
        }
    }

    public double FocusProgressBarWidth => FocusProgress * 300.0;

    // Clock Visual Angles & Displays
    public double HourHandAngle
    {
        get => _hourHandAngle;
        private set => SetProperty(ref _hourHandAngle, value);
    }

    public double MinuteHandAngle
    {
        get => _minuteHandAngle;
        private set => SetProperty(ref _minuteHandAngle, value);
    }

    public double SecondHandAngle
    {
        get => _secondHandAngle;
        private set => SetProperty(ref _secondHandAngle, value);
    }

    public string DigitalTimeString
    {
        get => _digitalTimeString;
        private set => SetProperty(ref _digitalTimeString, value);
    }

    public string StackedHourString
    {
        get => _stackedHourString;
        private set => SetProperty(ref _stackedHourString, value);
    }

    public string StackedMinuteString
    {
        get => _stackedMinuteString;
        private set => SetProperty(ref _stackedMinuteString, value);
    }

    public string SecondsString
    {
        get => _secondsString;
        private set
        {
            if (SetProperty(ref _secondsString, value))
            {
                OnPropertyChanged(nameof(SecondsDisplayString));
            }
        }
    }

    public string SecondsDisplayString => string.IsNullOrEmpty(_secondsString) ? string.Empty : $":{_secondsString}";

    public string AmPmString
    {
        get => _amPmString;
        private set => SetProperty(ref _amPmString, value);
    }

    public string DayOfWeekString
    {
        get => _dayOfWeekString;
        private set => SetProperty(ref _dayOfWeekString, value);
    }

    public string DayNameUpperString
    {
        get => _dayNameUpperString;
        private set => SetProperty(ref _dayNameUpperString, value);
    }

    public string PrimaryDateString
    {
        get => _primaryDateString;
        private set => SetProperty(ref _primaryDateString, value);
    }

    public string SecondaryDateString
    {
        get => _secondaryDateString;
        private set => SetProperty(ref _secondaryDateString, value);
    }

    public string DualDateCombinedString
    {
        get => _dualDateCombinedString;
        private set => SetProperty(ref _dualDateCombinedString, value);
    }

    public bool HasMinutes
    {
        get => _hasMinutes;
        private set => SetProperty(ref _hasMinutes, value);
    }

    public bool HasSeconds
    {
        get => _hasSeconds;
        private set => SetProperty(ref _hasSeconds, value);
    }

    public string WatermarkHourString
    {
        get => _watermarkHourString;
        private set => SetProperty(ref _watermarkHourString, value);
    }

    public bool IsDualCalendar
    {
        get => _isDualCalendar;
        private set
        {
            if (SetProperty(ref _isDualCalendar, value))
            {
                OnPropertyChanged(nameof(IsSingleCalendar));
            }
        }
    }

    public bool IsSingleCalendar => !IsDualCalendar;

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    public string TimezoneDisplay
    {
        get => _timezoneDisplay;
        private set => SetProperty(ref _timezoneDisplay, value);
    }

    public string DialLabel12
    {
        get => _dialLabel12;
        private set => SetProperty(ref _dialLabel12, value);
    }

    public string DialLabel3
    {
        get => _dialLabel3;
        private set => SetProperty(ref _dialLabel3, value);
    }

    public string DialLabel6
    {
        get => _dialLabel6;
        private set => SetProperty(ref _dialLabel6, value);
    }

    public string DialLabel9
    {
        get => _dialLabel9;
        private set => SetProperty(ref _dialLabel9, value);
    }

    // Dial style
    public AnalogDialStyle DialStyle
    {
        get => _settings.DialStyle;
        set
        {
            if (_settings.DialStyle != value)
            {
                _settings.DialStyle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEthiopianDial));
                OnPropertyChanged(nameof(IsMinimalistDial));
                OnPropertyChanged(nameof(IsRomanDial));
                UpdateTime();
                Save();
            }
        }
    }

    public bool IsEthiopianDial => _settings.DialStyle == AnalogDialStyle.EthiopianGeEz;
    public bool IsMinimalistDial => _settings.DialStyle == AnalogDialStyle.Minimalist;
    public bool IsRomanDial => _settings.DialStyle == AnalogDialStyle.RomanNumerals;

    // View mode
    public WidgetViewMode ViewMode
    {
        get => _settings.ViewMode;
        set
        {
            if (_settings.ViewMode != value)
            {
                _settings.ViewMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAnalogOnly));
                OnPropertyChanged(nameof(IsDigitalOnly));
                OnPropertyChanged(nameof(IsCombined));
                Save();
            }
        }
    }

    public bool IsAnalogOnly => _settings.ViewMode == WidgetViewMode.AnalogOnly;
    public bool IsDigitalOnly => _settings.ViewMode == WidgetViewMode.DigitalOnly;
    public bool IsCombined => _settings.ViewMode == WidgetViewMode.Combined;

    public WidgetThemeMode ThemeMode
    {
        get => _settings.ThemeMode;
        set
        {
            if (_settings.ThemeMode != value)
            {
                _settings.ThemeMode = value;
                ThemeManager.ApplyTheme(_settings.ThemeMode, _settings.Accent, _settings.Opacity, _settings.Backdrop);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDarkTheme));
                RequestWindowStyleUpdate?.Invoke();
                Save();
            }
        }
    }

    public bool IsDarkTheme => _settings.ThemeMode == WidgetThemeMode.DarkGlass;

    public AccentColorTheme Accent
    {
        get => _settings.Accent;
        set
        {
            if (_settings.Accent != value)
            {
                _settings.Accent = value;
                ThemeManager.ApplyTheme(_settings.ThemeMode, _settings.Accent, _settings.Opacity, _settings.Backdrop);
                OnPropertyChanged();
                Save();
            }
        }
    }

    public CalendarSystem Calendar
    {
        get => _settings.Calendar;
        set
        {
            if (_settings.Calendar != value)
            {
                _settings.Calendar = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDualCalendar));
                OnPropertyChanged(nameof(IsSingleCalendar));
                UpdateTime();
                Save();
            }
        }
    }

    public EthiopianScriptMode ScriptMode
    {
        get => _settings.ScriptMode;
        set
        {
            if (_settings.ScriptMode != value)
            {
                _settings.ScriptMode = value;
                OnPropertyChanged();
                UpdateTime();
                Save();
            }
        }
    }

    public TimeFormatMode TimeFormat
    {
        get => _settings.TimeFormat;
        set
        {
            if (_settings.TimeFormat != value)
            {
                _settings.TimeFormat = value;
                OnPropertyChanged();
                UpdateTime();
                Save();
            }
        }
    }

    public NumeralSystem Numerals
    {
        get => _settings.Numerals;
        set
        {
            if (_settings.Numerals != value)
            {
                _settings.Numerals = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGeezNumerals));
                OnPropertyChanged(nameof(IsWesternNumerals));
                UpdateFocusDisplay();
                UpdateTime();
                Save();
            }
        }
    }

    public bool IsGeezNumerals => _settings.Numerals == NumeralSystem.EthiopianGeEz;
    public bool IsWesternNumerals => _settings.Numerals == NumeralSystem.WesternArabic;

    public double Opacity
    {
        get => _settings.Opacity;
        set
        {
            if (Math.Abs(_settings.Opacity - value) > 0.005)
            {
                _settings.Opacity = Math.Clamp(value, 0.0, 1.0);
                ThemeManager.ApplyTheme(_settings.ThemeMode, _settings.Accent, _settings.Opacity, _settings.Backdrop);
                RequestWindowStyleUpdate?.Invoke();
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set
        {
            if (_settings.StartWithWindows != value)
            {
                _settings.StartWithWindows = value;
                AutostartService.SetAutostart(value);
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool SmoothSweepSecondHand
    {
        get => _settings.SmoothSweepSecondHand;
        set
        {
            if (_settings.SmoothSweepSecondHand != value)
            {
                _settings.SmoothSweepSecondHand = value;
                UpdateTimerInterval();
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool ShowSeconds
    {
        get => _settings.ShowSeconds;
        set
        {
            if (_settings.ShowSeconds != value)
            {
                _settings.ShowSeconds = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool AlwaysOnTop
    {
        get => _settings.AlwaysOnTop;
        set
        {
            if (_settings.AlwaysOnTop != value)
            {
                _settings.AlwaysOnTop = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    // Commands
    public ICommand CycleViewModeCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand CycleCalendarCommand { get; }
    public ICommand CycleDialStyleCommand { get; }
    public ICommand CycleNumeralSystemCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand ToggleSettingsCommand { get; }
    public ICommand ToggleClickThroughCommand { get; }

    // Focus Commands
    public ICommand ToggleFocusModeCommand { get; }
    public ICommand ToggleFocusRunningCommand { get; }
    public ICommand ResetFocusCommand { get; }
    public ICommand SetFocus15Command { get; }
    public ICommand SetFocus25Command { get; }
    public ICommand SetFocus45Command { get; }
    public ICommand TestChimeCommand { get; }

    public void CycleViewMode()
    {
        ViewMode = ViewMode switch
        {
            WidgetViewMode.Combined => WidgetViewMode.AnalogOnly,
            WidgetViewMode.AnalogOnly => WidgetViewMode.DigitalOnly,
            _ => WidgetViewMode.Combined
        };
    }

    public void ToggleTheme()
    {
        ThemeMode = ThemeMode == WidgetThemeMode.DarkGlass
            ? WidgetThemeMode.LightGlass
            : WidgetThemeMode.DarkGlass;
    }

    public void CycleCalendar()
    {
        Calendar = Calendar switch
        {
            CalendarSystem.Dual => CalendarSystem.Ethiopian,
            CalendarSystem.Ethiopian => CalendarSystem.Gregorian,
            _ => CalendarSystem.Dual
        };
    }

    public void CycleDialStyle()
    {
        DialStyle = DialStyle switch
        {
            AnalogDialStyle.Minimalist => AnalogDialStyle.EthiopianGeEz,
            AnalogDialStyle.EthiopianGeEz => AnalogDialStyle.RomanNumerals,
            AnalogDialStyle.RomanNumerals => AnalogDialStyle.ModernDigits,
            _ => AnalogDialStyle.Minimalist
        };
    }

    public void CycleNumeralSystem()
    {
        Numerals = Numerals == NumeralSystem.WesternArabic
            ? NumeralSystem.EthiopianGeEz
            : NumeralSystem.WesternArabic;
    }

    public void TogglePin()
    {
        AlwaysOnTop = !AlwaysOnTop;
    }

    public void ToggleClickThrough()
    {
        IsClickThrough = !IsClickThrough;
    }

    public void ToggleFocusMode()
    {
        IsFocusMode = !IsFocusMode;
    }

    public void ToggleFocusRunning()
    {
        IsFocusRunning = !IsFocusRunning;
        _lastFocusTick = DateTime.UtcNow;
    }

    public void ResetFocus()
    {
        IsFocusRunning = false;
        _isBreakMode = false;
        _focusRemainingSeconds = _focusTotalSeconds;
        _focusStatusText = "Focus Session";
        UpdateFocusDisplay();
    }

    public void SetFocusDuration(int minutes)
    {
        _settings.FocusDurationMinutes = minutes;
        _focusTotalSeconds = minutes * 60;
        ResetFocus();
        Save();
    }

    public void TestChime()
    {
        _hourlyChimeService.PlayChime(_settings.ChimeSound);
    }

    private void UpdateFocusTimer()
    {
        if (!IsFocusRunning) return;

        var now = DateTime.UtcNow;
        var elapsed = (now - _lastFocusTick).TotalSeconds;
        if (elapsed >= 1.0)
        {
            int secondsPassed = (int)elapsed;
            _lastFocusTick = now;
            _focusRemainingSeconds -= secondsPassed;

            if (_focusRemainingSeconds <= 0)
            {
                // Play completion chime
                _hourlyChimeService.PlayChime(HourlyChimeSound.AcousticBell);

                if (!_isBreakMode)
                {
                    // Transition to Break
                    _isBreakMode = true;
                    _focusTotalSeconds = _settings.BreakDurationMinutes * 60;
                    _focusRemainingSeconds = _focusTotalSeconds;
                    _focusStatusText = "☕ Take a Break (ዕረፍት)";
                }
                else
                {
                    // Break finished, return to Focus
                    _isBreakMode = false;
                    _focusTotalSeconds = _settings.FocusDurationMinutes * 60;
                    _focusRemainingSeconds = _focusTotalSeconds;
                    _focusStatusText = "🎯 Focus Session";
                    IsFocusRunning = false;
                }
            }

            UpdateFocusDisplay();
        }
    }

    private void UpdateFocusDisplay()
    {
        int totalSec = Math.Max(0, _focusRemainingSeconds);
        int m = totalSec / 60;
        int s = totalSec % 60;

        if (_settings.Numerals == NumeralSystem.EthiopianGeEz)
        {
            string minGeez = m == 0 ? "0" : GeezNumberConverter.FormatGeezMinute(m);
            string secGeez = s == 0 ? "0" : GeezNumberConverter.FormatGeezMinute(s);
            FocusTimeRemainingString = $"{minGeez}:{secGeez}";
        }
        else
        {
            FocusTimeRemainingString = $"{m:D2}:{s:D2}";
        }

        OnPropertyChanged(nameof(FocusProgress));
        OnPropertyChanged(nameof(FocusProgressBarWidth));
    }

    private void OnWindowsThemeChanged(bool isDark)
    {
        if (_settings.SyncWithWindowsTheme)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                ThemeMode = isDark ? WidgetThemeMode.DarkGlass : WidgetThemeMode.LightGlass;
            });
        }
    }

    private void ApplyWindowsSystemTheme()
    {
        bool isDark = SystemThemeService.IsWindowsInDarkMode();
        ThemeMode = isDark ? WidgetThemeMode.DarkGlass : WidgetThemeMode.LightGlass;
    }

    private void UpdateTimerInterval()
    {
        _timer.Interval = _settings.SmoothSweepSecondHand
            ? TimeSpan.FromMilliseconds(33)
            : TimeSpan.FromMilliseconds(500);
    }

    private void UpdateTime()
    {
        DateTime now = string.IsNullOrEmpty(_settings.TimeZoneId)
            ? DateTime.Now
            : TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, _settings.TimeZoneId);

        // Check & update focus timer
        UpdateFocusTimer();

        // Check & trigger hourly chime
        _hourlyChimeService.CheckAndPlayChime(now, _settings);

        // Sub-second precision
        double millis = now.Millisecond;
        double second = now.Second + (_settings.SmoothSweepSecondHand ? (millis / 1000.0) : 0);
        double minute = now.Minute + (second / 60.0);

        // Second and Minute hand angles
        SecondHandAngle = second * 6.0;
        MinuteHandAngle = minute * 6.0;

        // Hour hand angle
        if (_settings.DialStyle == AnalogDialStyle.EthiopianGeEz)
        {
            double ethHour = ((now.Hour + 6) % 12) + (minute / 60.0);
            HourHandAngle = ethHour * 30.0;
        }
        else
        {
            double hour = (now.Hour % 12) + (minute / 60.0);
            HourHandAngle = hour * 30.0;
        }

        // Dial Labels
        switch (_settings.DialStyle)
        {
            case AnalogDialStyle.EthiopianGeEz:
                DialLabel12 = "፲፪";
                DialLabel3 = "፫";
                DialLabel6 = "፮";
                DialLabel9 = "፱";
                break;
            case AnalogDialStyle.RomanNumerals:
                DialLabel12 = "XII";
                DialLabel3 = "III";
                DialLabel6 = "VI";
                DialLabel9 = "IX";
                break;
            case AnalogDialStyle.ModernDigits:
                DialLabel12 = "12";
                DialLabel3 = "3";
                DialLabel6 = "6";
                DialLabel9 = "9";
                break;
            default:
                DialLabel12 = string.Empty;
                DialLabel3 = string.Empty;
                DialLabel6 = string.Empty;
                DialLabel9 = string.Empty;
                break;
        }

        // Digital Time strings
        int rawHour;
        int rawMinute = now.Minute;
        int rawSecond = now.Second;

        bool useAmharicPeriod = _settings.ScriptMode == EthiopianScriptMode.Amharic || _settings.Numerals == NumeralSystem.EthiopianGeEz;

        if (_settings.TimeFormat == TimeFormatMode.Ethiopian12H)
        {
            var ethTime = EthiopianCalendarConverter.ToEthiopianTime(now);
            rawHour = ethTime.Hour;
            rawMinute = ethTime.Minute;
            rawSecond = ethTime.Second;
            AmPmString = useAmharicPeriod
                ? GetAmharicTimePeriod(now)
                : ethTime.PeriodEnglish;
        }
        else if (_settings.TimeFormat == TimeFormatMode.Standard24H)
        {
            rawHour = now.Hour;
            AmPmString = "24H";
        }
        else // Standard 12H
        {
            int h = now.Hour % 12;
            if (h == 0) h = 12;
            rawHour = h;
            AmPmString = useAmharicPeriod
                ? GetAmharicTimePeriod(now)
                : (now.Hour >= 12 ? "PM" : "AM");
        }

        if (_settings.Numerals == NumeralSystem.EthiopianGeEz)
        {
            int geezHourVal = rawHour == 0 ? 12 : rawHour;
            string hourGeez = GeezNumberConverter.FormatGeezHour(geezHourVal);
            StackedHourString = hourGeez;
            WatermarkHourString = hourGeez;

            if (rawMinute == 0)
            {
                StackedMinuteString = string.Empty;
                HasMinutes = false;
                DigitalTimeString = hourGeez;
            }
            else
            {
                string minGeez = GeezNumberConverter.FormatGeezMinute(rawMinute);
                StackedMinuteString = minGeez;
                HasMinutes = true;
                DigitalTimeString = $"{hourGeez}:{minGeez}";
            }

            if (rawSecond == 0)
            {
                SecondsString = string.Empty;
                HasSeconds = false;
            }
            else
            {
                SecondsString = GeezNumberConverter.FormatGeezMinute(rawSecond);
                HasSeconds = true;
            }
        }
        else
        {
            StackedHourString = $"{rawHour:D2}";
            StackedMinuteString = $"{rawMinute:D2}";
            SecondsString = $"{rawSecond:D2}";
            DigitalTimeString = $"{rawHour:D2}:{rawMinute:D2}";
            WatermarkHourString = $"{rawHour:D2}";
            HasMinutes = true;
            HasSeconds = true;
        }

        // Calendar & Date formatting
        EthiopianDate ethDate = EthiopianCalendarConverter.ToEthiopianDate(now);
        bool useAmharic = _settings.ScriptMode == EthiopianScriptMode.Amharic;
        bool useGeezNum = _settings.Numerals == NumeralSystem.EthiopianGeEz;

        if (_settings.Calendar == CalendarSystem.Dual)
        {
            IsDualCalendar = true;
            DayOfWeekString = useAmharic ? ethDate.DayNameAmharic : now.ToString("dddd");
            DayNameUpperString = (useAmharic ? ethDate.DayNameAmharic : now.ToString("dddd")).ToUpperInvariant();
            PrimaryDateString = now.ToString("MMM d, yyyy");
            SecondaryDateString = ethDate.ToShortString(useAmharic, useGeezNum) + (useAmharic ? " ዓ.ም." : " E.C.");
            DualDateCombinedString = $"{PrimaryDateString}  •  {SecondaryDateString}";
        }
        else if (_settings.Calendar == CalendarSystem.Ethiopian)
        {
            IsDualCalendar = false;
            DayOfWeekString = useAmharic ? ethDate.DayNameAmharic : ethDate.DayNameEnglish;
            DayNameUpperString = (useAmharic ? ethDate.DayNameAmharic : ethDate.DayNameEnglish).ToUpperInvariant();
            PrimaryDateString = ethDate.ToShortString(useAmharic, useGeezNum) + (useAmharic ? " ዓ.ም." : " E.C.");
            SecondaryDateString = string.Empty;
            DualDateCombinedString = PrimaryDateString;
        }
        else // Gregorian
        {
            IsDualCalendar = false;
            DayOfWeekString = now.ToString("dddd");
            DayNameUpperString = now.ToString("dddd").ToUpperInvariant();
            PrimaryDateString = now.ToString("MMMM d, yyyy");
            SecondaryDateString = string.Empty;
            DualDateCombinedString = PrimaryDateString;
        }

        // World Timezones (Multi-zone support for all 140+ global zones)
        if (_settings.ShowWorldTimezone)
        {
            DateTime utcNow = DateTime.UtcNow;
            if (ActiveWorldClocks.Count == 0 && _settings.WorldZones?.Count > 0)
            {
                SyncActiveWorldClocks();
            }

            foreach (var clock in ActiveWorldClocks)
            {
                try
                {
                    DateTime zoneNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcNow, clock.TimeZoneId);
                    int wh = zoneNow.Hour % 12;
                    if (wh == 0) wh = 12;
                    string wAmPm = zoneNow.Hour >= 12 ? "PM" : "AM";

                    if (_settings.Numerals == NumeralSystem.EthiopianGeEz)
                    {
                        string whG = GeezNumberConverter.FormatGeezHour(wh);
                        string wmG = zoneNow.Minute == 0 ? "0" : GeezNumberConverter.FormatGeezMinute(zoneNow.Minute);
                        clock.TimeString = $"{whG}:{wmG}";
                    }
                    else
                    {
                        clock.TimeString = $"{wh:D2}:{zoneNow.Minute:D2}";
                    }
                    clock.PeriodString = wAmPm;
                }
                catch
                {
                    clock.TimeString = "--:--";
                    clock.PeriodString = string.Empty;
                }
            }

            // Sync single badge bindings for backwards compatibility
            var firstClock = ActiveWorldClocks.FirstOrDefault();
            if (firstClock != null)
            {
                WorldTimeString = firstClock.TimeString;
                WorldTimePeriodString = firstClock.PeriodString;
            }
        }
    }

    public void SyncActiveWorldClocks()
    {
        _settings.WorldZones ??= new List<WorldClockZone>
        {
            new WorldClockZone("Eastern Standard Time", "New York", "🇺🇸"),
            new WorldClockZone("GMT Standard Time", "London", "🇬🇧"),
            new WorldClockZone("E. Africa Standard Time", "Addis Ababa", "🇪🇹")
        };

        SortWorldZonesInternal();

        ActiveWorldClocks.Clear();
        foreach (var z in _settings.WorldZones)
        {
            ActiveWorldClocks.Add(new WorldClockDisplayItem
            {
                TimeZoneId = z.TimeZoneId,
                DisplayLabel = z.DisplayLabel,
                FlagEmoji = z.FlagEmoji
            });
        }
    }

    private void SortWorldZonesInternal()
    {
        if (_settings.WorldZones == null || _settings.WorldZones.Count <= 1) return;

        DateTime utcNow = DateTime.UtcNow;
        _settings.WorldZones = _settings.WorldZones
            .OrderBy(z => GetZoneOffset(z.TimeZoneId, utcNow))
            .ThenBy(z => z.DisplayLabel)
            .ToList();
    }

    private static TimeSpan GetZoneOffset(string tzId, DateTime utcNow)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            return tz.GetUtcOffset(utcNow);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    public void SortWorldClocksByOffset()
    {
        SortWorldZonesInternal();
        SyncActiveWorldClocks();
        UpdateTime();
        Save();
    }

    public void MoveWorldZoneUp(string timeZoneId)
    {
        if (_settings.WorldZones == null || _settings.WorldZones.Count <= 1) return;
        int idx = _settings.WorldZones.FindIndex(z => z.TimeZoneId == timeZoneId);
        if (idx > 0)
        {
            var item = _settings.WorldZones[idx];
            _settings.WorldZones.RemoveAt(idx);
            _settings.WorldZones.Insert(idx - 1, item);
            ActiveWorldClocks.Move(idx, idx - 1);
            UpdateTime();
            Save();
        }
    }

    public void MoveWorldZoneDown(string timeZoneId)
    {
        if (_settings.WorldZones == null || _settings.WorldZones.Count <= 1) return;
        int idx = _settings.WorldZones.FindIndex(z => z.TimeZoneId == timeZoneId);
        if (idx >= 0 && idx < _settings.WorldZones.Count - 1)
        {
            var item = _settings.WorldZones[idx];
            _settings.WorldZones.RemoveAt(idx);
            _settings.WorldZones.Insert(idx + 1, item);
            ActiveWorldClocks.Move(idx, idx + 1);
            UpdateTime();
            Save();
        }
    }

    private void InitializeTimeZones()
    {
        var rawZones = TimeZoneInfo.GetSystemTimeZones()
            .OrderBy(tz => tz.BaseUtcOffset)
            .ThenBy(tz => tz.DisplayName);

        AllTimeZones.Clear();
        foreach (var tz in rawZones)
        {
            AllTimeZones.Add(new TimeZoneDisplayItem(tz));
        }

        FilterTimeZones(string.Empty);
    }

    public void FilterTimeZones(string? query)
    {
        FilteredTimeZones.Clear();
        if (string.IsNullOrWhiteSpace(query))
        {
            foreach (var item in AllTimeZones)
            {
                FilteredTimeZones.Add(item);
            }
        }
        else
        {
            string[] tokens = query.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in AllTimeZones)
            {
                bool matches = true;
                foreach (var t in tokens)
                {
                    if (!item.SearchableText.Contains(t))
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                {
                    FilteredTimeZones.Add(item);
                }
            }
        }

        if (FilteredTimeZones.Count > 0)
        {
            SelectedTimeZoneItem = FilteredTimeZones[0];
        }
        else
        {
            SelectedTimeZoneItem = null;
        }
    }

    public void ClearTimeZoneSearch()
    {
        TimeZoneSearchText = string.Empty;
    }

    public void AddWorldZone(TimeZoneInfo? tz)
    {
        if (tz == null) return;
        _settings.WorldZones ??= new List<WorldClockZone>();
        if (_settings.WorldZones.Any(z => z.TimeZoneId == tz.Id)) return;

        string label = ExtractCityOrFriendlyName(tz);
        string emoji = TimeZoneDisplayItem.GuessFlagEmoji(tz.Id, label);
        _settings.WorldZones.Add(new WorldClockZone(tz.Id, label, emoji));
        SyncActiveWorldClocks();
        UpdateTime();
        Save();
    }

    public void AddWorldZone(TimeZoneDisplayItem? item)
    {
        if (item == null) return;
        _settings.WorldZones ??= new List<WorldClockZone>();
        if (_settings.WorldZones.Any(z => z.TimeZoneId == item.Id)) return;

        string label = item.CityName;
        int commaIndex = label.IndexOf(',');
        if (commaIndex > 0)
        {
            label = label[..commaIndex].Trim();
        }

        _settings.WorldZones.Add(new WorldClockZone(item.Id, label, item.FlagEmoji));
        SyncActiveWorldClocks();
        UpdateTime();
        Save();
    }

    public void QuickAddZone(string tzId, string label, string flagEmoji)
    {
        _settings.WorldZones ??= new List<WorldClockZone>();
        if (_settings.WorldZones.Any(z => z.TimeZoneId == tzId && z.DisplayLabel == label)) return;

        _settings.WorldZones.Add(new WorldClockZone(tzId, label, flagEmoji));
        SyncActiveWorldClocks();
        UpdateTime();
        Save();
    }

    public void RemoveWorldZone(string timeZoneId)
    {
        if (_settings.WorldZones == null) return;
        _settings.WorldZones.RemoveAll(z => z.TimeZoneId == timeZoneId);
        SyncActiveWorldClocks();
        UpdateTime();
        Save();
    }

    public void RemoveWorldZone(WorldClockDisplayItem clock)
    {
        if (_settings.WorldZones == null || clock == null) return;
        _settings.WorldZones.RemoveAll(z => z.TimeZoneId == clock.TimeZoneId && z.DisplayLabel == clock.DisplayLabel);
        SyncActiveWorldClocks();
        UpdateTime();
        Save();
    }

    private static string ExtractCityOrFriendlyName(TimeZoneInfo tz)
    {
        if (tz.Id == "E. Africa Standard Time") return "Addis Ababa";

        string name = tz.DisplayName;
        int closeParen = name.IndexOf(')');
        if (closeParen >= 0 && closeParen < name.Length - 1)
        {
            string cities = name[(closeParen + 1)..].Trim();
            var parts = cities.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                return parts[0];
            }
        }
        return tz.StandardName;
    }

    private static string GuessFlagEmoji(string tzId)
    {
        if (tzId.Contains("Africa", StringComparison.OrdinalIgnoreCase)) return "🇪🇹";
        if (tzId.Contains("Eastern", StringComparison.OrdinalIgnoreCase) || tzId.Contains("Pacific", StringComparison.OrdinalIgnoreCase) || tzId.Contains("Central Standard Time", StringComparison.OrdinalIgnoreCase) || tzId.Contains("Mountain", StringComparison.OrdinalIgnoreCase)) return "🇺🇸";
        if (tzId.Contains("GMT", StringComparison.OrdinalIgnoreCase)) return "🇬🇧";
        if (tzId.Contains("Tokyo", StringComparison.OrdinalIgnoreCase) || tzId.Contains("Japan", StringComparison.OrdinalIgnoreCase)) return "🇯🇵";
        if (tzId.Contains("Arabian", StringComparison.OrdinalIgnoreCase) || tzId.Contains("Dubai", StringComparison.OrdinalIgnoreCase)) return "🇦🇪";
        if (tzId.Contains("China", StringComparison.OrdinalIgnoreCase)) return "🇨🇳";
        if (tzId.Contains("India", StringComparison.OrdinalIgnoreCase)) return "🇮🇳";
        if (tzId.Contains("W. Europe", StringComparison.OrdinalIgnoreCase) || tzId.Contains("Romance", StringComparison.OrdinalIgnoreCase) || tzId.Contains("Central Europe", StringComparison.OrdinalIgnoreCase)) return "🇪🇺";
        return "🌍";
    }

    public void Save()
    {
        _settingsService.SaveSettings(_settings);
    }

    public static string GetAmharicTimePeriod(DateTime dt)
    {
        int h = dt.Hour;
        if (h >= 6 && h < 12) return "ጠዋት";
        if (h >= 12 && h < 18) return "ከቀኑ";
        if (h >= 18 && h < 24) return "ማታ";
        return "ከሌሊቱ";
    }
}
