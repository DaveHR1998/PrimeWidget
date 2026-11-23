using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace PrimeWidgetSetup;

public partial class MainWindow : Window
{
    private int _currentPage;
    private bool _isInstalling;

    public MainWindow()
    {
        InitializeComponent();

        string defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PrimeWidget"
        );
        TxtInstallPath.Text = defaultPath;

        UpdateWizardPage();
    }

    private void UpdateWizardPage()
    {
        // 1. Hide all pages
        PageWelcome.Visibility = Visibility.Collapsed;
        PageLocation.Visibility = Visibility.Collapsed;
        PageTasks.Visibility = Visibility.Collapsed;
        PageReady.Visibility = Visibility.Collapsed;
        PageInstalling.Visibility = Visibility.Collapsed;
        PageFinished.Visibility = Visibility.Collapsed;

        // 2. Configure headers and visibility per page
        switch (_currentPage)
        {
            case 0: // Welcome
                TopHeaderBanner.Visibility = Visibility.Collapsed;
                PageWelcome.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = false;
                BtnNext.Content = "&Next >";
                BtnNext.IsEnabled = true;
                BtnCancel.Visibility = Visibility.Visible;
                BtnCancel.IsEnabled = true;
                break;

            case 1: // Location
                TopHeaderBanner.Visibility = Visibility.Visible;
                TxtHeaderTitle.Text = "Select Destination Location";
                TxtHeaderSubtitle.Text = "Where should PrimeWidget be installed?";
                PageLocation.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = true;
                BtnNext.Content = "&Next >";
                BtnNext.IsEnabled = true;
                break;

            case 2: // Additional Tasks
                TopHeaderBanner.Visibility = Visibility.Visible;
                TxtHeaderTitle.Text = "Select Additional Tasks";
                TxtHeaderSubtitle.Text = "Which additional tasks should be performed?";
                PageTasks.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = true;
                BtnNext.Content = "&Next >";
                BtnNext.IsEnabled = true;
                break;

            case 3: // Ready to Install
                TopHeaderBanner.Visibility = Visibility.Visible;
                TxtHeaderTitle.Text = "Ready to Install";
                TxtHeaderSubtitle.Text = "Setup is now ready to begin installing PrimeWidget on your computer.";
                PageReady.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = true;
                BtnNext.Content = "&Install";
                BtnNext.IsEnabled = true;

                // Populate summary
                TxtSummaryLocation.Text = TxtInstallPath.Text;
                var tasks = new System.Text.StringBuilder();
                if (ChkDesktopShortcut.IsChecked == true) tasks.AppendLine("• Create desktop shortcut");
                if (ChkStartMenuShortcut.IsChecked == true) tasks.AppendLine("• Create Start Menu shortcut");
                if (ChkAutostart.IsChecked == true) tasks.AppendLine("• Start PrimeWidget automatically on Windows startup");
                TxtSummaryTasks.Text = tasks.Length > 0 ? tasks.ToString().TrimEnd() : "• None";
                break;

            case 4: // Installing
                TopHeaderBanner.Visibility = Visibility.Visible;
                TxtHeaderTitle.Text = "Installing";
                TxtHeaderSubtitle.Text = "Please wait while Setup installs PrimeWidget on your computer.";
                PageInstalling.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = false;
                BtnNext.IsEnabled = false;
                BtnCancel.IsEnabled = false;
                break;

            case 5: // Finished
                TopHeaderBanner.Visibility = Visibility.Collapsed;
                PageFinished.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = false;
                BtnNext.Content = "Finish";
                BtnNext.IsEnabled = true;
                BtnCancel.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 0 && !_isInstalling)
        {
            _currentPage--;
            UpdateWizardPage();
        }
    }

    private async void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_currentPage < 3)
        {
            _currentPage++;
            UpdateWizardPage();
        }
        else if (_currentPage == 3)
        {
            _currentPage = 4;
            UpdateWizardPage();
            await RunInstallationAsync();
        }
        else if (_currentPage == 5)
        {
            // Finish
            string targetDir = TxtInstallPath.Text.Trim();
            string exePath = Path.Combine(targetDir, "PrimeWidget.exe");

            if (ChkLaunchAfter.IsChecked == true && File.Exists(exePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                }
                catch { }
            }

            Close();
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 0 && _currentPage < 5)
        {
            var result = MessageBox.Show(
                "Setup is not complete. If you exit now, PrimeWidget will not be installed.\n\nAre you sure you want to exit Setup?",
                "Exit Setup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );
            if (result != MessageBoxResult.Yes) return;
        }

        Close();
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Destination Location",
            InitialDirectory = TxtInstallPath.Text
        };
        if (dialog.ShowDialog() == true)
        {
            TxtInstallPath.Text = dialog.FolderName;
        }
    }

    private async Task RunInstallationAsync()
    {
        _isInstalling = true;
        string targetDir = TxtInstallPath.Text.Trim();
        bool makeDesktopShortcut = ChkDesktopShortcut.IsChecked == true;
        bool makeStartMenuShortcut = ChkStartMenuShortcut.IsChecked == true;
        bool enableAutostart = ChkAutostart.IsChecked == true;

        try
        {
            await Task.Run(() =>
            {
                // 1. Terminate running process if any
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = "Stopping existing PrimeWidget processes...";
                    InstallProgressBar.Value = 15;
                });

                // Force terminate any lingering instances
                try
                {
                    var psi = new ProcessStartInfo("taskkill", "/F /T /IM PrimeWidget.exe")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi)?.WaitForExit(1500);
                }
                catch { }

                foreach (var proc in Process.GetProcessesByName("PrimeWidget"))
                {
                    try { proc.Kill(); proc.WaitForExit(1500); } catch { }
                }

                // Allow OS handles to release
                System.Threading.Thread.Sleep(500);

                // 2. Ensure target directory exists with retry loop
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        if (!Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }
                        break;
                    }
                    catch (Exception) when (attempt < 4)
                    {
                        System.Threading.Thread.Sleep(400);
                    }
                }

                // 3. Extract payload
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = "Extracting files to destination folder...";
                    InstallProgressBar.Value = 40;
                });

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "PrimeWidgetSetup.payload.zip";
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new FileNotFoundException($"Embedded payload not found: {resourceName}");
                    }

                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        string destPath = Path.Combine(targetDir, entry.FullName);
                        string? dir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        // Extract with retry and clear ReadOnly flag if file exists
                        bool extracted = false;
                        for (int attempt = 0; attempt < 5; attempt++)
                        {
                            try
                            {
                                if (File.Exists(destPath))
                                {
                                    File.SetAttributes(destPath, FileAttributes.Normal);
                                }
                                entry.ExtractToFile(destPath, overwrite: true);
                                extracted = true;
                                break;
                            }
                            catch (Exception) when (attempt < 4)
                            {
                                System.Threading.Thread.Sleep(300);
                            }
                        }

                        if (!extracted)
                        {
                            entry.ExtractToFile(destPath, overwrite: true);
                        }
                    }
                }

                // 4. Create Shortcuts & Autostart
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = "Creating shortcuts and system registrations...";
                    InstallProgressBar.Value = 75;
                });

                string exePath = Path.Combine(targetDir, "PrimeWidget.exe");

                if (makeDesktopShortcut)
                {
                    try
                    {
                        string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        CreateShortcut(Path.Combine(userDesktop, "PrimeWidget.lnk"), exePath, targetDir);

                        string commonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                        if (!string.IsNullOrEmpty(commonDesktop) && Directory.Exists(commonDesktop))
                        {
                            CreateShortcut(Path.Combine(commonDesktop, "PrimeWidget.lnk"), exePath, targetDir);
                        }
                    }
                    catch { }
                }

                if (makeStartMenuShortcut)
                {
                    try
                    {
                        string userPrograms = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                        if (!Directory.Exists(userPrograms)) Directory.CreateDirectory(userPrograms);
                        CreateShortcut(Path.Combine(userPrograms, "PrimeWidget.lnk"), exePath, targetDir);

                        string commonPrograms = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
                        if (!string.IsNullOrEmpty(commonPrograms) && Directory.Exists(commonPrograms))
                        {
                            CreateShortcut(Path.Combine(commonPrograms, "PrimeWidget.lnk"), exePath, targetDir);
                        }
                    }
                    catch { }
                }

                // Windows Startup registry
                using (var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true))
                {
                    if (runKey != null)
                    {
                        if (enableAutostart)
                        {
                            runKey.SetValue("PrimeWidget", $"\"{exePath}\" --autostart");
                        }
                        else
                        {
                            runKey.DeleteValue("PrimeWidget", throwOnMissingValue: false);
                        }
                    }
                }

                // 5. Create clean uninstaller (runs from temp to avoid locking targetDir)
                string uninstallerCmd = Path.Combine(targetDir, "Uninstall.cmd");
                string uninstallerContent = $@"@echo off
title Uninstalling PrimeWidget
taskkill /f /im PrimeWidget.exe >nul 2>&1
reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v ""PrimeWidget"" /f >nul 2>&1
reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\PrimeWidget"" /f >nul 2>&1
reg delete ""HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\PrimeWidget"" /f >nul 2>&1
del ""%USERPROFILE%\Desktop\PrimeWidget.lnk"" >nul 2>&1
del ""%PUBLIC%\Desktop\PrimeWidget.lnk"" >nul 2>&1
del ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\PrimeWidget.lnk"" >nul 2>&1
del ""%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\PrimeWidget.lnk"" >nul 2>&1
cd /d ""%TEMP%""
start /b """" cmd /c ""timeout /t 1 >nul & rmdir /s /q """"{targetDir}""""""
";
                File.WriteAllText(uninstallerCmd, uninstallerContent);

                // 6. Windows Add/Remove Programs Registration (HKCU & HKLM)
                string uninstallSubKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\PrimeWidget";
                RegistryKey?[] uninstallRoots = [
                    Registry.CurrentUser.CreateSubKey(uninstallSubKey),
                    Registry.LocalMachine.CreateSubKey(uninstallSubKey)
                ];
                foreach (var k in uninstallRoots)
                {
                    if (k != null)
                    {
                        using (k)
                        {
                            k.SetValue("DisplayName", "PrimeWidget Desktop Clock & Calendar");
                            k.SetValue("DisplayVersion", "1.0.0");
                            k.SetValue("Publisher", "PrimeWidget");
                            k.SetValue("InstallLocation", targetDir);
                            k.SetValue("UninstallString", $"cmd.exe /c \"{uninstallerCmd}\"");
                            k.SetValue("DisplayIcon", exePath);
                            k.SetValue("EstimatedSize", 2048, RegistryValueKind.DWord);
                        }
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    InstallProgressBar.Value = 100;
                    TxtStatus.Text = "Installation complete.";
                });
            });

            _isInstalling = false;
            _currentPage = 5; // Move to Finish page
            UpdateWizardPage();
        }
        catch (Exception ex)
        {
            _isInstalling = false;
            MessageBox.Show($"An error occurred during installation:\n\n{ex.Message}", "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _currentPage = 3;
            UpdateWizardPage();
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell != null)
                {
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = workingDirectory;
                    shortcut.Description = "PrimeWidget Desktop Clock & Calendar with Ethiopian Ge'ez support";
                    shortcut.IconLocation = targetPath + ",0";
                    shortcut.Save();
                }
            }
        }
        catch
        {
            // Silently fallback if WScript is restricted
        }
    }
}
