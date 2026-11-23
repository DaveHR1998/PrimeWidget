using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PrimeWidget.Services;

public class TrayService : IDisposable
{
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 101;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONUP = 0x0205;

    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;

    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;

    private const int IDI_APPLICATION = 32512;
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(string szFileName, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    private HwndSource? _hwndSource;
    private NOTIFYICONDATA _nid;
    private bool _isCreated;
    private ContextMenu? _contextMenu;
    private IntPtr _customIconHandle = IntPtr.Zero;

    public event Action? OnOpenSettings;

    public void Initialize(ContextMenu contextMenu)
    {
        _contextMenu = contextMenu;

        var parameters = new HwndSourceParameters("PrimeWidgetTrayListener")
        {
            WindowStyle = 0,
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        _customIconHandle = LoadAppIcon();

        _nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwndSource.Handle,
            uID = 1001,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _customIconHandle,
            szTip = "PrimeWidget"
        };

        _isCreated = Shell_NotifyIcon(NIM_ADD, ref _nid);
    }

    private IntPtr LoadAppIcon()
    {
        try
        {
            // 1. Check local app.ico
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string icoPath = Path.Combine(appDir, "app.ico");
            if (!File.Exists(icoPath))
            {
                icoPath = Path.Combine(appDir, "..", "..", "..", "app.ico");
            }
            if (File.Exists(icoPath))
            {
                IntPtr hIcon = LoadImage(IntPtr.Zero, icoPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
                if (hIcon != IntPtr.Zero) return hIcon;
            }

            // 2. Extract from running executable
            string? exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
            {
                ExtractIconEx(exe, 0, out _, out IntPtr smallIcon, 1);
                if (smallIcon != IntPtr.Zero) return smallIcon;
            }
        }
        catch { }

        // Fallback default
        return LoadIcon(IntPtr.Zero, (IntPtr)IDI_APPLICATION);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAYICON)
        {
            int action = lParam.ToInt32();
            if (action == WM_LBUTTONDBLCLK || action == WM_LBUTTONUP)
            {
                OnOpenSettings?.Invoke();
                handled = true;
            }
            else if (action == WM_RBUTTONUP)
            {
                ShowContextMenu();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        if (_contextMenu == null || _hwndSource == null) return;

        GetCursorPos(out POINT pt);
        SetForegroundWindow(_hwndSource.Handle);

        _contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
        _contextMenu.HorizontalOffset = pt.X;
        _contextMenu.VerticalOffset = pt.Y;
        _contextMenu.IsOpen = true;
    }

    public void Dispose()
    {
        if (_isCreated)
        {
            Shell_NotifyIcon(NIM_DELETE, ref _nid);
            _isCreated = false;
        }

        if (_customIconHandle != IntPtr.Zero)
        {
            try { DestroyIcon(_customIconHandle); } catch { }
            _customIconHandle = IntPtr.Zero;
        }

        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
