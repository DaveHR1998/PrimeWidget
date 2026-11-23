using System;
using System.IO;
using Microsoft.Win32;

namespace PrimeWidget.Services;

public static class AutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "PrimeWidget";

    public static bool IsAutostartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var value = key?.GetValue(AppName) as string;
            if (string.IsNullOrWhiteSpace(value)) return false;

            string currentExe = Environment.ProcessPath ?? string.Empty;
            if (string.IsNullOrEmpty(currentExe)) return true;

            // Strip quotes and arguments to compare path if needed
            string raw = value.Trim();
            string registeredPath = raw.StartsWith('"')
                ? raw.Substring(1, raw.IndexOf('"', 1) - 1)
                : raw.Split(' ')[0];

            return File.Exists(registeredPath);
        }
        catch
        {
            return false;
        }
    }

    public static void SetAutostart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null) return;

            if (enable)
            {
                string exePath = Environment.ProcessPath ?? string.Empty;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\" --autostart");
                }
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Silently ignore if registry access is blocked
        }
    }
}
