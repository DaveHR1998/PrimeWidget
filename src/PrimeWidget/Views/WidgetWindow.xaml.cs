using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using PrimeWidget.Models;
using PrimeWidget.Services;
using PrimeWidget.ViewModels;

namespace PrimeWidget.Views;

public partial class WidgetWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static int GetWindowLong(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? (int)GetWindowLongPtr64(hWnd, nIndex).ToInt64()
            : GetWindowLong32(hWnd, nIndex);
    }

    private static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
    {
        return IntPtr.Size == 8
            ? (int)SetWindowLongPtr64(hWnd, nIndex, new IntPtr(dwNewLong)).ToInt64()
            : SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    private readonly TimeWidgetViewModel _viewModel;
    private DispatcherTimer? _quickGlanceTimer;
    private bool _isQuickGlanceActive;
    public event Action? RequestOpenSettings;

    public WidgetWindow(TimeWidgetViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += OnLoaded;
        LocationChanged += OnLocationChanged;
        Closing += OnClosing;

        _viewModel.RequestWindowStyleUpdate += ApplyWindowStyles;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.AlwaysOnTop))
            {
                Dispatcher.Invoke(UpdateTopmostState);
            }
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyWindowStyles();
        UpdateTopmostState();
    }

    public void UpdateTopmostState()
    {
        try
        {
            bool shouldBeTop = _viewModel.AlwaysOnTop || _isQuickGlanceActive;
            Topmost = shouldBeTop;

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                IntPtr insertAfter = shouldBeTop ? HWND_TOPMOST : HWND_NOTOPMOST;
                SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }
        }
        catch { }
    }

    public void ApplyWindowStyles()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            // 1. Apply DWM Backdrop (Mica / Acrylic)
            // If opacity is near 0% (< 0.05), disable native backdrop so DWM doesn't paint a blur rectangle behind transparent widget
            var effectiveBackdrop = _viewModel.Settings.Opacity < 0.05 ? BackdropStyle.None : _viewModel.Backdrop;
            BackdropService.ApplyToHwnd(hwnd, effectiveBackdrop, _viewModel.IsDarkTheme);

            // 2. Apply Click-Through / Ghost Mode
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (_viewModel.IsClickThrough)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);
            }

            // 3. Ensure Topmost Z-order is preserved after style changes
            UpdateTopmostState();
        }
        catch { }
    }

    public void ShowQuickGlance()
    {
        Dispatcher.Invoke(() =>
        {
            if (!IsVisible)
            {
                Show();
            }

            _isQuickGlanceActive = true;
            UpdateTopmostState();
            Activate();

            _quickGlanceTimer?.Stop();
            _quickGlanceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _quickGlanceTimer.Tick += (s, e) =>
            {
                _quickGlanceTimer?.Stop();
                _isQuickGlanceActive = false;
                UpdateTopmostState();
            };
            _quickGlanceTimer.Start();
        });
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        if (_viewModel.Settings.WindowLeft.HasValue && _viewModel.Settings.WindowTop.HasValue)
        {
            double left = _viewModel.Settings.WindowLeft.Value;
            double top = _viewModel.Settings.WindowTop.Value;

            // Ensure window is on-screen
            if (left < workArea.Left || left > workArea.Right - 100)
                left = workArea.Right - ActualWidth - 40;
            if (top < workArea.Top || top > workArea.Bottom - 100)
                top = workArea.Top + 60;

            Left = left;
            Top = top;
        }
        else
        {
            Left = workArea.Right - ActualWidth - 40;
            Top = workArea.Top + 60;
        }

        ApplyWindowStyles();
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            _viewModel.Settings.WindowLeft = Left;
            _viewModel.Settings.WindowTop = Top;
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Save();
    }

    private void OnWindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsClickThrough) return;

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
                SnapToScreenEdges();
                _viewModel.Save();
            }
            catch
            {
                // DragMove handles release
            }
        }
    }

    private void OnWindowMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsClickThrough) return;

        if (e.ChangedButton == MouseButton.Left)
        {
            RequestOpenSettings?.Invoke();
        }
    }

    private void SnapToScreenEdges()
    {
        const double snapDistance = 24.0;
        var workArea = SystemParameters.WorkArea;

        // Left edge
        if (Math.Abs(Left - workArea.Left) < snapDistance)
        {
            Left = workArea.Left + 10;
        }
        // Right edge
        else if (Math.Abs((Left + ActualWidth) - workArea.Right) < snapDistance)
        {
            Left = workArea.Right - ActualWidth - 10;
        }

        // Top edge
        if (Math.Abs(Top - workArea.Top) < snapDistance)
        {
            Top = workArea.Top + 10;
        }
        // Bottom edge
        else if (Math.Abs((Top + ActualHeight) - workArea.Bottom) < snapDistance)
        {
            Top = workArea.Bottom - ActualHeight - 10;
        }
    }

    private void OnContextMenuSettingsClick(object sender, RoutedEventArgs e) =>
        RequestOpenSettings?.Invoke();

    private void OnToggleWorldTimezoneClick(object sender, RoutedEventArgs e) =>
        _viewModel.ShowWorldTimezone = !_viewModel.ShowWorldTimezone;

    private void OnContextMenuCombinedClick(object sender, RoutedEventArgs e) =>
        _viewModel.ViewMode = WidgetViewMode.Combined;

    private void OnContextMenuAnalogClick(object sender, RoutedEventArgs e) =>
        _viewModel.ViewMode = WidgetViewMode.AnalogOnly;

    private void OnContextMenuDigitalClick(object sender, RoutedEventArgs e) =>
        _viewModel.ViewMode = WidgetViewMode.DigitalOnly;

    private void OnDialMinimalistClick(object sender, RoutedEventArgs e) =>
        _viewModel.DialStyle = AnalogDialStyle.Minimalist;

    private void OnDialEthiopianClick(object sender, RoutedEventArgs e) =>
        _viewModel.DialStyle = AnalogDialStyle.EthiopianGeEz;

    private void OnDialRomanClick(object sender, RoutedEventArgs e) =>
        _viewModel.DialStyle = AnalogDialStyle.RomanNumerals;

    private void OnDialModernDigitsClick(object sender, RoutedEventArgs e) =>
        _viewModel.DialStyle = AnalogDialStyle.ModernDigits;

    private void OnContextMenuDualCalClick(object sender, RoutedEventArgs e) =>
        _viewModel.Calendar = CalendarSystem.Dual;

    private void OnContextMenuEthCalClick(object sender, RoutedEventArgs e) =>
        _viewModel.Calendar = CalendarSystem.Ethiopian;

    private void OnContextMenuGregCalClick(object sender, RoutedEventArgs e) =>
        _viewModel.Calendar = CalendarSystem.Gregorian;

    private void OnContextMenuHideClick(object sender, RoutedEventArgs e) =>
        Hide();

    private void OnContextMenuExitClick(object sender, RoutedEventArgs e) =>
        Application.Current.Shutdown();
}
