@echo off
setlocal
title PrimeWidget Setup

:: 1. Self-elevate to Administrator so certificate is trusted machine-wide
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Elevating privileges to configure trusted installation...
    powershell.exe -NoProfile -Command "Start-Process cmd.exe -ArgumentList '/c \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"

echo ===================================================
echo               PrimeWidget Setup
echo ===================================================
echo Configuring safe trusted publisher environment...

:: 2. Add certificate to Trusted Root and Trusted Publishers machine-wide
if exist "%~dp0PrimeWidgetCert.cer" (
    certutil.exe -addstore -f "Root" "%~dp0PrimeWidgetCert.cer" >nul 2>&1
    certutil.exe -addstore -f "TrustedPublisher" "%~dp0PrimeWidgetCert.cer" >nul 2>&1
)

:: 3. Remove Mark of the Web
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%~dp0' -Recurse | Unblock-File -ErrorAction SilentlyContinue" >nul 2>&1

:: 4. Smart Bootstrapper: Check & auto-download .NET 8 Desktop Runtime if missing
echo Checking system prerequisites...
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

:: 5. Launch Setup
echo Starting installer...
start "" "%~dp0dist\PrimeWidget-Setup.exe"

exit /b 0
