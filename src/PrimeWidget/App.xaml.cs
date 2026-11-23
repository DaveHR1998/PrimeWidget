using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using PrimeWidget.Models;
using PrimeWidget.Services;
using PrimeWidget.ViewModels;
using PrimeWidget.Views;

namespace PrimeWidget;

public partial class App : Application
{
    private const string MutexName = "PrimeWidget_SingleInstance_Mutex_3194";
    private const string EventName = "PrimeWidget_Wakeup_Event_3194";

    private static Mutex? _instanceMutex;
    private static EventWaitHandle? _wakeupEvent;

    private SettingsService? _settingsService;
    private TimeWidgetViewModel? _viewModel;
    private WidgetWindow? _widgetWindow;
    private SettingsWindow? _settingsWindow;
    private TrayService? _trayService;
    private GlobalHotkeyService? _globalHotkeyService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrimeWidget");
        try { Directory.CreateDirectory(logDir); } catch { }

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            try { File.WriteAllText(Path.Combine(logDir, "crash.log"), args.ExceptionObject?.ToString()); } catch { }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            try { File.WriteAllText(Path.Combine(logDir, "crash_dispatcher.log"), args.Exception?.ToString()); } catch { }
        };

        // 1. Single Instance Check
        bool isFirstInstance;
        try
        {
            _instanceMutex = new Mutex(true, MutexName, out isFirstInstance);
        }
        catch
        {
            isFirstInstance = true;
        }

        if (!isFirstInstance)
        {
            // Signal the already-running instance to show settings & bring widget forward
            try
            {
                using var existingEvent = EventWaitHandle.OpenExisting(EventName);
                existingEvent.Set();
            }
            catch { }

            Shutdown(0);
            return;
        }

        try
        {
            // 2. Setup Wakeup Event for future duplicate launches
            try
            {
                _wakeupEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
                var listenerThread = new Thread(() =>
                {
                    while (_wakeupEvent.WaitOne())
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (_widgetWindow != null)
                            {
                                _widgetWindow.Show();
                                _widgetWindow.Activate();
                            }
                            if (_settingsWindow != null)
                            {
                                _settingsWindow.Show();
                                _settingsWindow.WindowState = WindowState.Normal;
                                _settingsWindow.Activate();
                            }
                        });
                    }
                })
                {
                    IsBackground = true
                };
                listenerThread.Start();
            }
            catch { }

            // 3. Run internal verifications
            DateVerification.RunTests();

            if (e.Args.Length > 0 && Array.Exists(e.Args, a => a.Equals("--test", StringComparison.OrdinalIgnoreCase)))
            {
                string report = $"PRIMEWIDGET PRODUCTION SELF-TEST SUCCESS: {DateVerification.PassedCount} tests passed.";
                try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_results.txt"), report); } catch { }
                Shutdown(0);
                return;
            }

            _settingsService = new SettingsService();
            _viewModel = new TimeWidgetViewModel(_settingsService);

            // 4. Desktop Widget Window (Always active on desktop)
            _widgetWindow = new WidgetWindow(_viewModel);
            _widgetWindow.Show();
            _widgetWindow.Activate();

            // 5. Settings & Dashboard Window
            _settingsWindow = new SettingsWindow(_viewModel, _widgetWindow);

            _widgetWindow.RequestOpenSettings += () =>
            {
                _settingsWindow.Show();
                _settingsWindow.WindowState = WindowState.Normal;
                _settingsWindow.Activate();
            };

            // If the user launched PrimeWidget manually (not silent background boot),
            // show the Settings Window so they immediately see the app is running!
            bool isSilentBoot = e.Args.Length > 0 && Array.Exists(e.Args, a =>
                a.Equals("--silent", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--autostart", StringComparison.OrdinalIgnoreCase));

            if (!isSilentBoot)
            {
                _settingsWindow.Show();
                _settingsWindow.WindowState = WindowState.Normal;
                _settingsWindow.Activate();
            }

            // 6. Global Hotkey Service (Ctrl + Alt + W for Quick Glance)
            _globalHotkeyService = new GlobalHotkeyService();
            _globalHotkeyService.HotkeyPressed += () =>
            {
                Dispatcher.Invoke(() => _widgetWindow?.ShowQuickGlance());
            };
            if (_viewModel.EnableGlobalHotkey)
            {
                _globalHotkeyService.Initialize();
            }

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.EnableGlobalHotkey))
                {
                    if (_viewModel.EnableGlobalHotkey)
                    {
                        _globalHotkeyService.Initialize();
                    }
                    else
                    {
                        _globalHotkeyService.Unregister();
                    }
                }
            };

            // 7. System Tray Service
            InitializeSystemTray();
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(Path.Combine(logDir, "startup_error.log"), ex.ToString());
                MessageBox.Show($"PrimeWidget encountered a startup error:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}", "PrimeWidget Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        }
    }

    private void InitializeSystemTray()
    {
        var trayMenu = new ContextMenu();

        var openSettingsItem = new MenuItem { Header = "⚙️ Open Settings & Dashboard..." };
        openSettingsItem.Click += (s, e) =>
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Show();
                _settingsWindow.WindowState = WindowState.Normal;
                _settingsWindow.Activate();
            }
        };

        var toggleWidgetItem = new MenuItem { Header = "👁️ Show / Hide Widget" };
        toggleWidgetItem.Click += (s, e) =>
        {
            if (_widgetWindow != null)
            {
                if (_widgetWindow.IsVisible)
                    _widgetWindow.Hide();
                else
                {
                    _widgetWindow.Show();
                    _widgetWindow.Activate();
                }
            }
        };

        var toggleFocusItem = new MenuItem { Header = "⏱️ Toggle Focus / Pomodoro Mode" };
        toggleFocusItem.Click += (s, e) => _viewModel?.ToggleFocusMode();

        var toggleClickThroughItem = new MenuItem { Header = "👻 Toggle Click-Through (Ghost Mode)" };
        toggleClickThroughItem.Click += (s, e) => _viewModel?.ToggleClickThrough();

        var toggleWorldTimezoneItem = new MenuItem { Header = "🌍 Toggle World Timezone (Addis Ababa)" };
        toggleWorldTimezoneItem.Click += (s, e) =>
        {
            if (_viewModel != null)
                _viewModel.ShowWorldTimezone = !_viewModel.ShowWorldTimezone;
        };

        var toggleThemeItem = new MenuItem { Header = "🌓 Toggle Light / Dark Theme" };
        toggleThemeItem.Click += (s, e) => _viewModel?.ToggleTheme();

        var toggleEthDialItem = new MenuItem { Header = "🇪🇹 Toggle Ethiopian Dial" };
        toggleEthDialItem.Click += (s, e) =>
        {
            if (_viewModel != null)
            {
                _viewModel.DialStyle = _viewModel.DialStyle == AnalogDialStyle.EthiopianGeEz
                    ? AnalogDialStyle.Minimalist
                    : AnalogDialStyle.EthiopianGeEz;
            }
        };

        var toggleNumeralsItem = new MenuItem { Header = "፩፪፫ Toggle Ge'ez Numerals" };
        toggleNumeralsItem.Click += (s, e) => _viewModel?.CycleNumeralSystem();

        var exitItem = new MenuItem { Header = "✕ Exit PrimeWidget" };
        exitItem.Click += (s, e) => ShutdownApplication();

        trayMenu.Items.Add(openSettingsItem);
        trayMenu.Items.Add(new Separator());
        trayMenu.Items.Add(toggleWidgetItem);
        trayMenu.Items.Add(toggleFocusItem);
        trayMenu.Items.Add(toggleClickThroughItem);
        trayMenu.Items.Add(toggleWorldTimezoneItem);
        trayMenu.Items.Add(new Separator());
        trayMenu.Items.Add(toggleThemeItem);
        trayMenu.Items.Add(toggleEthDialItem);
        trayMenu.Items.Add(toggleNumeralsItem);
        trayMenu.Items.Add(new Separator());
        trayMenu.Items.Add(exitItem);

        _trayService = new TrayService();
        _trayService.OnOpenSettings += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_settingsWindow != null)
                {
                    _settingsWindow.Show();
                    _settingsWindow.WindowState = WindowState.Normal;
                    _settingsWindow.Activate();
                }
            });
        };

        _trayService.Initialize(trayMenu);
    }

    private void ShutdownApplication()
    {
        _globalHotkeyService?.Dispose();
        _trayService?.Dispose();
        _settingsWindow?.ShutdownApplication();
        _widgetWindow?.Close();

        try
        {
            _wakeupEvent?.Dispose();
            _instanceMutex?.ReleaseMutex();
            _instanceMutex?.Dispose();
        }
        catch { }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _globalHotkeyService?.Dispose();
        _trayService?.Dispose();
        try
        {
            _wakeupEvent?.Dispose();
            _instanceMutex?.ReleaseMutex();
            _instanceMutex?.Dispose();
        }
        catch { }
        base.OnExit(e);
    }
}
