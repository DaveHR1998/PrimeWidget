using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace PrimeWidget.Services;

public class GlobalHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_W = 0x57;

    private const int HOTKEY_ID = 9001;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _hwndSource;
    private bool _isRegistered;

    public string ActiveShortcutDescription { get; private set; } = "Ctrl + Alt + W";
    public bool IsRegistered => _isRegistered;

    public event Action? HotkeyPressed;

    public void Initialize()
    {
        if (_hwndSource != null) return;

        var parameters = new HwndSourceParameters("PrimeWidgetHotkeyListener")
        {
            WindowStyle = 0,
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        Register();
    }

    public void Register()
    {
        if (_hwndSource == null || _isRegistered) return;

        try
        {
            // Primary shortcut: Ctrl + Alt + W (Never clashes with Windows Xbox Game Bar broadcasting)
            _isRegistered = RegisterHotKey(_hwndSource.Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_W);
            if (_isRegistered)
            {
                ActiveShortcutDescription = "Ctrl + Alt + W";
                return;
            }

            // Fallback 1: Ctrl + Alt + W without MOD_NOREPEAT
            _isRegistered = RegisterHotKey(_hwndSource.Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_W);
            if (_isRegistered)
            {
                ActiveShortcutDescription = "Ctrl + Alt + W";
                return;
            }

            // Fallback 2: Win + Shift + W (Conflict-free Windows combination)
            _isRegistered = RegisterHotKey(_hwndSource.Handle, HOTKEY_ID, MOD_WIN | MOD_SHIFT | MOD_NOREPEAT, VK_W);
            if (_isRegistered)
            {
                ActiveShortcutDescription = "Win + Shift + W";
                return;
            }

            // Fallback 3: Win + Shift + W without MOD_NOREPEAT
            _isRegistered = RegisterHotKey(_hwndSource.Handle, HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_W);
            if (_isRegistered)
            {
                ActiveShortcutDescription = "Win + Shift + W";
            }
        }
        catch { }
    }

    public void Unregister()
    {
        if (_hwndSource != null && _isRegistered)
        {
            try
            {
                UnregisterHotKey(_hwndSource.Handle, HOTKEY_ID);
            }
            catch { }
            _isRegistered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }
}
