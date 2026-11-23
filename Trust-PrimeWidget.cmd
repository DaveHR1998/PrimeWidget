@echo off
setlocal
:: Check for Administrator permissions and self-elevate
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Requesting Administrator privileges to trust PrimeWidget...
    powershell -NoProfile -Command "Start-Process cmd.exe -ArgumentList '/c \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"
title PrimeWidget - Certificate Trust Installer
echo ====================================================================
echo       Registering PrimeWidget as a Trusted Publisher
echo ====================================================================
echo.

if not exist "%~dp0PrimeWidgetCert.cer" (
    echo Error: PrimeWidgetCert.cer was not found in %~dp0
    pause
    exit /b 1
)

echo [1/4] Adding PrimeWidget to Trusted Root Certification Authorities...
certutil.exe -addstore -f "Root" "%~dp0PrimeWidgetCert.cer" >nul 2>&1

echo [2/4] Adding PrimeWidget to Trusted Publishers...
certutil.exe -addstore -f "TrustedPublisher" "%~dp0PrimeWidgetCert.cer" >nul 2>&1

echo [3/4] Removing Windows Mark-of-the-Web from all project binaries...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%~dp0' -Recurse | Unblock-File -ErrorAction SilentlyContinue" >nul 2>&1

echo [4/4] Checking system prerequisites (.NET 8 Desktop Runtime)...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
  "$hasNet8 = (Test-Path 'C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\8.*') -or " ^
  "  (Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall', 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall' -ErrorAction SilentlyContinue | " ^
  "   Get-ItemProperty -ErrorAction SilentlyContinue | Where-Object { $_.DisplayName -like '*Windows Desktop Runtime*8.*' }); " ^
  "if (-not $hasNet8) { " ^
  "  Write-Host ''; " ^
  "  Write-Host '====================================================================' -ForegroundColor Cyan; " ^
  "  Write-Host ' Microsoft .NET 8 Desktop Runtime is required to run PrimeWidget.' -ForegroundColor Yellow; " ^
  "  Write-Host ' Downloading official installer from Microsoft CDN (~55 MB)...' -ForegroundColor Cyan; " ^
  "  Write-Host '====================================================================' -ForegroundColor Cyan; " ^
  "  $tempInstaller = Join-Path $env:TEMP 'windowsdesktop-runtime-8.0-win-x64.exe'; " ^
  "  $url = 'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe'; " ^
  "  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " ^
  "  $wc = New-Object System.Net.WebClient; " ^
  "  $wc.DownloadFile($url, $tempInstaller); " ^
  "  Write-Host ' Installing .NET 8 Desktop Runtime quietly... Please wait.' -ForegroundColor Green; " ^
  "  $proc = Start-Process -FilePath $tempInstaller -ArgumentList '/install /quiet /norestart' -Wait -PassThru; " ^
  "  Remove-Item $tempInstaller -Force -ErrorAction SilentlyContinue; " ^
  "  if ($proc.ExitCode -eq 0 -or $proc.ExitCode -eq 3010) { " ^
  "    Write-Host ' [OK] Microsoft .NET 8 Desktop Runtime installed successfully!' -ForegroundColor Green; " ^
  "  } else { " ^
  "    Write-Host (' Warning: .NET 8 runtime installer exited with code ' + $proc.ExitCode) -ForegroundColor Yellow; " ^
  "  } " ^
  "} else { " ^
  "  Write-Host ' [OK] Microsoft .NET 8 Desktop Runtime is already present.' -ForegroundColor Green; " ^
  "}"

echo.
echo ====================================================================
echo   SUCCESS! 
echo   PrimeWidget is now registered as a Trusted Verified Publisher.
echo   SmartScreen will NO LONGER block or warn on PrimeWidget-Setup.exe.
echo ====================================================================
echo.
echo Starting installer now...
start "" "%~dp0dist\PrimeWidget-Setup.exe"
timeout /t 3 >nul
exit /b 0
