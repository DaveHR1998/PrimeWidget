using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PrimeWidget.Models;

namespace PrimeWidget.Services;

public static class BackdropService
{
    // DWM Backdrops (Windows 11 Build 22621+)
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWA_MICA_EFFECT = 1029;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

    private const int DWMSBT_AUTO = 0;
    private const int DWMSBT_NONE = 1;
    private const int DWMSBT_MAINWINDOW = 2;       // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic
    private const int DWMSBT_TABBEDWINDOW = 4;     // Mica Alt

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // Windows 10 Accent API (User32 undocumented)
    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    private enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    private enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    public static void ApplyBackdrop(Window window, BackdropStyle style, bool isDark)
    {
        if (window == null) return;

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            window.SourceInitialized += (s, e) =>
            {
                var h = new WindowInteropHelper(window).Handle;
                ApplyToHwnd(h, style, isDark);
            };
            return;
        }

        ApplyToHwnd(hwnd, style, isDark);
    }

    public static void ApplyToHwnd(IntPtr hwnd, BackdropStyle style, bool isDark)
    {
        if (hwnd == IntPtr.Zero) return;

        try
        {
            // 1. Sync Immersive Dark Mode for DWM
            int darkMode = isDark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
            }

            // 2. Apply Windows 11 DWM Backdrop (Build 22621+)
            int backdropType = style switch
            {
                BackdropStyle.Mica => DWMSBT_MAINWINDOW,
                BackdropStyle.Acrylic => DWMSBT_TRANSIENTWINDOW,
                BackdropStyle.MicaAlt => DWMSBT_TABBEDWINDOW,
                _ => DWMSBT_NONE
            };

            int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
            if (hr == 0 && style != BackdropStyle.None)
            {
                return; // Windows 11 system backdrop succeeded!
            }

            // 3. Fallback for Windows 11 Build 22000 (Mica flag)
            if (style == BackdropStyle.Mica)
            {
                int micaTrue = 1;
                if (DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref micaTrue, sizeof(int)) == 0)
                {
                    return;
                }
            }

            // 4. Fallback for Windows 10 (Accent Policy for Acrylic Blur)
            if (style == BackdropStyle.Acrylic)
            {
                ApplyWindows10Acrylic(hwnd, isDark);
            }
            else
            {
                DisableWindows10Accent(hwnd);
            }
        }
        catch
        {
            // Silently fall back if running on older OS or restricted sandbox
        }
    }

    private static void ApplyWindows10Acrylic(IntPtr hwnd, bool isDark)
    {
        try
        {
            // BGR + Alpha: 0x9912141A for dark, 0x99FAFAFC for light
            int gradientColor = isDark ? unchecked((int)0x99181A22) : unchecked((int)0x99F0F4F8);

            var policy = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 2,
                GradientColor = gradientColor,
                AnimationId = 0
            };

            int size = Marshal.SizeOf(policy);
            IntPtr policyPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(policy, policyPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = policyPtr,
                    SizeOfData = size
                };
                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(policyPtr);
            }
        }
        catch { }
    }

    private static void DisableWindows10Accent(IntPtr hwnd)
    {
        try
        {
            var policy = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_DISABLED
            };

            int size = Marshal.SizeOf(policy);
            IntPtr policyPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(policy, policyPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = policyPtr,
                    SizeOfData = size
                };
                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(policyPtr);
            }
        }
        catch { }
    }
}
