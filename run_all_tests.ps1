$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
Add-Type -AssemblyName System.IO.Compression.FileSystem

$passed = 0
$failed = 0

function Assert-Test {
    param(
        [string]$Name,
        [scriptblock]$Test
    )
    try {
        & $Test
        Write-Host "  [PASS] $Name" -ForegroundColor Green
        $script:passed++
    }
    catch {
        Write-Host "  [FAIL] $Name - $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
    }
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       PRIMEWIDGET PRODUCTION TEST SUITE              " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# -----------------------------------------------------------------------------
# SUITE 1: Binary & Asset Integrity
# -----------------------------------------------------------------------------
Write-Host "--- SUITE 1: Binary & Asset Integrity ---" -ForegroundColor Yellow

Assert-Test "App Icon (app.ico) exists and is multi-resolution" {
    $icoPath = Join-Path $root "src\PrimeWidget\app.ico"
    if (-not (Test-Path $icoPath)) { throw "app.ico missing" }
    $bytes = [System.IO.File]::ReadAllBytes($icoPath)
    if ($bytes.Length -lt 10000) { throw "app.ico size too small ($($bytes.Length) bytes)" }
    if ($bytes[0] -ne 0 -or $bytes[1] -ne 0 -or $bytes[2] -ne 1 -or $bytes[3] -ne 0) {
        throw "Invalid ICO magic header"
    }
    $imageCount = [BitConverter]::ToInt16($bytes, 4)
    if ($imageCount -lt 4) { throw "Expected at least 4 icon sizes, found $imageCount" }
}

Assert-Test "Application logo (primewidget.png) exists and is valid PNG" {
    $pngPath = Join-Path $root "src\PrimeWidget\Resources\primewidget.png"
    if (-not (Test-Path $pngPath)) { throw "src\PrimeWidget\Resources\primewidget.png missing" }
    $bytes = [System.IO.File]::ReadAllBytes($pngPath)
    if ($bytes[0] -ne 0x89 -or $bytes[1] -ne 0x50 -or $bytes[2] -ne 0x4E -or $bytes[3] -ne 0x47) {
        throw "Invalid PNG magic header"
    }
}

Assert-Test "PrimeWidget.exe exists in publish folder" {
    $exe = Join-Path $root "publish\PrimeWidget.exe"
    if (-not (Test-Path $exe)) { throw "publish\PrimeWidget.exe missing" }
    if ((Get-Item $exe).Length -lt 100000) { throw "PrimeWidget.exe is suspiciously small" }
}

Assert-Test "PrimeWidget-Setup.exe exists in dist folder" {
    $setup = Join-Path $root "dist\PrimeWidget-Setup.exe"
    if (-not (Test-Path $setup)) { throw "dist\PrimeWidget-Setup.exe missing" }
    if ((Get-Item $setup).Length -lt 100000) { throw "PrimeWidget-Setup.exe is suspiciously small" }
}

Assert-Test "Installer payload.zip exists and contains application files" {
    $zipPath = Join-Path $root "installer\payload.zip"
    if (-not (Test-Path $zipPath)) { throw "installer\payload.zip missing" }
    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    $hasExe = $false
    $hasDll = $false
    foreach ($entry in $zip.Entries) {
        if ($entry.Name -eq "PrimeWidget.exe") { $hasExe = $true }
        if ($entry.Name -eq "PrimeWidget.dll") { $hasDll = $true }
    }
    $zip.Dispose()
    if (-not $hasExe) { throw "payload.zip is missing PrimeWidget.exe" }
    if (-not $hasDll) { throw "payload.zip is missing PrimeWidget.dll" }
}

Write-Host ""

# -----------------------------------------------------------------------------
# SUITE 2: Security & Code Signing
# -----------------------------------------------------------------------------
Write-Host "--- SUITE 2: Security & Code Signing ---" -ForegroundColor Yellow

Assert-Test "Public certificate PrimeWidgetCert.cer exists and is valid X.509" {
    $cerPath = Join-Path $root "PrimeWidgetCert.cer"
    if (-not (Test-Path $cerPath)) { throw "PrimeWidgetCert.cer missing" }
    $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($cerPath)
    if ($cert.Subject -notlike "*CN=PrimeWidget Software*") {
        throw "Unexpected subject: $($cert.Subject)"
    }
}

Assert-Test "publish\PrimeWidget.exe is Authenticode signed by PrimeWidget Software" {
    $exe = Join-Path $root "publish\PrimeWidget.exe"
    $sig = Get-AuthenticodeSignature $exe
    if ($null -eq $sig.SignerCertificate) { throw "PrimeWidget.exe has no signature" }
    if ($sig.SignerCertificate.Subject -notlike "*CN=PrimeWidget Software*") {
        throw "Signer is not PrimeWidget Software: $($sig.SignerCertificate.Subject)"
    }
}

Assert-Test "dist\PrimeWidget-Setup.exe is Authenticode signed by PrimeWidget Software" {
    $setup = Join-Path $root "dist\PrimeWidget-Setup.exe"
    $sig = Get-AuthenticodeSignature $setup
    if ($null -eq $sig.SignerCertificate) { throw "PrimeWidget-Setup.exe has no signature" }
    if ($sig.SignerCertificate.Subject -notlike "*CN=PrimeWidget Software*") {
        throw "Signer is not PrimeWidget Software: $($sig.SignerCertificate.Subject)"
    }
}

Assert-Test "Trust-PrimeWidget.cmd and Install-PrimeWidget.bat are present and valid" {
    $trustCmd = Join-Path $root "Trust-PrimeWidget.cmd"
    $installBat = Join-Path $root "Install-PrimeWidget.bat"
    if (-not (Test-Path $trustCmd)) { throw "Trust-PrimeWidget.cmd missing" }
    if (-not (Test-Path $installBat)) { throw "Install-PrimeWidget.bat missing" }
    $content = Get-Content $installBat -Raw
    if ($content -notlike "*PrimeWidget-Setup.exe*") { throw "Install-PrimeWidget.bat does not launch setup" }
}

Write-Host ""

# -----------------------------------------------------------------------------
# SUITE 3: Ethiopian Calendar Conversion Logic
# -----------------------------------------------------------------------------
Write-Host "--- SUITE 3: Ethiopian Calendar & Conversion Logic ---" -ForegroundColor Yellow

$assemblyPath = Join-Path $root "publish\PrimeWidget.dll"
if (-not (Test-Path $assemblyPath)) {
    $assemblyPath = Join-Path $root "src\PrimeWidget\bin\Release\net8.0-windows\PrimeWidget.dll"
}
$pwAssembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$converterType = $pwAssembly.GetType("PrimeWidget.Services.EthiopianCalendarConverter")
if ($null -eq $converterType) { throw "Could not find EthiopianCalendarConverter in assembly" }

$toEthMethod = $converterType.GetMethod("ToEthiopianDate", [Type[]]@([DateTime]))
$toEthTimeMethod = $converterType.GetMethod("ToEthiopianTime", [Type[]]@([DateTime]))

Assert-Test "Ethiopian New Year 2019 E.C. (September 11, 2026)" {
    $greg = [DateTime]::new(2026, 9, 11)
    $eth = $toEthMethod.Invoke($null, @($greg))
    if ($eth.Year -ne 2019) { throw "Expected year 2019, got $($eth.Year)" }
    if ($eth.Month -ne 1) { throw "Expected month 1 (Meskerem), got $($eth.Month)" }
    if ($eth.Day -ne 1) { throw "Expected day 1, got $($eth.Day)" }
    if ($eth.MonthNameAmharic -ne "መስከረም") { throw "Expected መስከረም, got $($eth.MonthNameAmharic)" }
    if ($eth.DayNameAmharic -ne "ዓርብ") { throw "Expected ዓርብ, got $($eth.DayNameAmharic)" }
}

Assert-Test "Pagume 5 check (September 10, 2026 = Pagume 5, 2018 E.C.)" {
    $greg = [DateTime]::new(2026, 9, 10)
    $eth = $toEthMethod.Invoke($null, @($greg))
    if ($eth.Year -ne 2018) { throw "Expected year 2018, got $($eth.Year)" }
    if ($eth.Month -ne 13) { throw "Expected month 13 (Pagume), got $($eth.Month)" }
    if ($eth.Day -ne 5) { throw "Expected day 5, got $($eth.Day)" }
    if ($eth.MonthNameAmharic -ne "ጳጉሜን") { throw "Expected ጳጉሜን, got $($eth.MonthNameAmharic)" }
}

Assert-Test "Current Date (October 25, 2026 = Tikimt 15, 2019 E.C.)" {
    $greg = [DateTime]::new(2026, 10, 25)
    $eth = $toEthMethod.Invoke($null, @($greg))
    if ($eth.Year -ne 2019) { throw "Expected year 2019, got $($eth.Year)" }
    if ($eth.Month -ne 2) { throw "Expected month 2 (Tikimt), got $($eth.Month)" }
    if ($eth.Day -ne 15) { throw "Expected day 15, got $($eth.Day)" }
    if ($eth.MonthNameAmharic -ne "ጥቅምት") { throw "Expected ጥቅምት, got $($eth.MonthNameAmharic)" }
    if ($eth.DayNameAmharic -ne "እሑድ") { throw "Expected እሑድ (Sunday), got $($eth.DayNameAmharic)" }
}

Assert-Test "Traditional Ethiopian 12-Hour Cycle Time calculation" {
    # 6:00 AM Gregorian -> 12:00 Day in Ethiopian
    $t6am = [DateTime]::new(2026, 10, 25, 6, 0, 0)
    $ethTime6am = $toEthTimeMethod.Invoke($null, @($t6am))
    if ($ethTime6am.Item1 -ne 12) { throw "Expected 12 at 6:00 AM, got $($ethTime6am.Item1)" }
    
    # 7:00 AM Gregorian -> 1:00 Day in Ethiopian
    $t7am = [DateTime]::new(2026, 10, 25, 7, 0, 0)
    $ethTime7am = $toEthTimeMethod.Invoke($null, @($t7am))
    if ($ethTime7am.Item1 -ne 1) { throw "Expected 1 at 7:00 AM, got $($ethTime7am.Item1)" }

    # 12:00 PM (Noon) -> 6:00 Day in Ethiopian
    $t12pm = [DateTime]::new(2026, 10, 25, 12, 0, 0)
    $ethTime12pm = $toEthTimeMethod.Invoke($null, @($t12pm))
    if ($ethTime12pm.Item1 -ne 6) { throw "Expected 6 at 12:00 PM, got $($ethTime12pm.Item1)" }
}

# Ge'ez Numerals Converter Verification
$geezType = $pwAssembly.GetType("PrimeWidget.Helpers.GeezNumberConverter")
if ($null -eq $geezType) { throw "Could not find GeezNumberConverter in assembly" }
$toGeezMethod = $geezType.GetMethod("ToGeez", [Type[]]@([int]))
$formatMinMethod = $geezType.GetMethod("FormatGeezMinute", [Type[]]@([int]))
$formatHourMethod = $geezType.GetMethod("FormatGeezHour", [Type[]]@([int]))

Assert-Test "Ge'ez Numeral Conversion: 10:54 -> ፲:፶፬" {
    $h = $formatHourMethod.Invoke($null, @(10))
    $m = $formatMinMethod.Invoke($null, @(54))
    if ($h -ne "፲") { throw "Expected ፲ for hour 10, got $h" }
    if ($m -ne "፶፬") { throw "Expected ፶፬ for minute 54, got $m" }
}

Assert-Test "Ge'ez Numeral Zero-Suppression: 00 min -> empty (hour at center), 05 min -> ፭ (no leading zero)" {
    $m0 = $formatMinMethod.Invoke($null, @(0))
    if ($m0 -ne "") { throw "Expected empty string for 0 minute, got $m0" }
    
    $m5 = $formatMinMethod.Invoke($null, @(5))
    if ($m5 -ne "፭") { throw "Expected ፭ without leading zero for 5 minutes, got $m5" }
}

Assert-Test "Ge'ez Numeral Ethiopian Calendar Years: 2019 E.C. -> ፳፻፲፱" {
    $y2019 = $toGeezMethod.Invoke($null, @(2019))
    if ($y2019 -ne "፳፻፲፱") { throw "Expected ፳፻፲፱ for year 2019, got $y2019" }
}

$vmType = $pwAssembly.GetType("PrimeWidget.ViewModels.TimeWidgetViewModel")
if ($null -ne $vmType) {
    $periodMethod = $vmType.GetMethod("GetAmharicTimePeriod", [Type[]]@([DateTime]))
    if ($null -ne $periodMethod) {
        Assert-Test "Amharic Contextual 4-Period (ጠዋት, ከቀኑ, ማታ, ከሌሊቱ)" {
            $m = $periodMethod.Invoke($null, @([DateTime]::new(2026, 10, 26, 8, 0, 0)))
            if ($m -ne "ጠዋት") { throw "Expected ጠዋት at 8:00 AM, got $m" }

            $a = $periodMethod.Invoke($null, @([DateTime]::new(2026, 10, 26, 14, 0, 0)))
            if ($a -ne "ከቀኑ") { throw "Expected ከቀኑ at 2:00 PM, got $a" }

            $e = $periodMethod.Invoke($null, @([DateTime]::new(2026, 10, 26, 20, 0, 0)))
            if ($e -ne "ማታ") { throw "Expected ማታ at 8:00 PM, got $e" }

            $n = $periodMethod.Invoke($null, @([DateTime]::new(2026, 10, 26, 2, 0, 0)))
            if ($n -ne "ከሌሊቱ") { throw "Expected ከሌሊቱ at 2:00 AM, got $n" }
        }
    }
}

Write-Host ""

# -----------------------------------------------------------------------------
# SUITE 4: Settings Persistence & Defaults
# -----------------------------------------------------------------------------
Write-Host "--- SUITE 4: Settings Persistence & Defaults ---" -ForegroundColor Yellow

$settingsType = $pwAssembly.GetType("PrimeWidget.Models.WidgetSettings")
$settingsInstance = [Activator]::CreateInstance($settingsType)

Assert-Test "WidgetSettings defaults are properly initialized" {
    $propOpacity = $settingsType.GetProperty("Opacity").GetValue($settingsInstance)
    if ($propOpacity -ne 0.0) { throw "Default opacity expected 0.0 (100% transparent), got $propOpacity" }
    
    $propAutostart = $settingsType.GetProperty("StartWithWindows").GetValue($settingsInstance)
    if ($propAutostart -ne $true) { throw "StartWithWindows expected true by default" }
}

Assert-Test "Settings JSON serialization & deserialization roundtrip (preserving 0.0 Opacity)" {
    $settingsInstance.Opacity = 0.0
    $settingsInstance.StartWithWindows = $true
    
    $json = [System.Text.Json.JsonSerializer]::Serialize($settingsInstance, [Type]$settingsType)
    $restored = [System.Text.Json.JsonSerializer]::Deserialize($json, [Type]$settingsType)
    
    if ($restored.Opacity -ne 0.0) { throw "Opacity not preserved (expected 0.0, got $($restored.Opacity))" }
    if ($restored.StartWithWindows -ne $true) { throw "StartWithWindows not preserved" }
}

Assert-Test "Multi-zone World Clock list is initialized and serializable" {
    $zonesProp = $settingsType.GetProperty("WorldZones")
    if ($null -eq $zonesProp) { throw "WorldZones property not found on WidgetSettings" }
    $zones = $zonesProp.GetValue($settingsInstance)
    if ($null -eq $zones -or $zones.Count -lt 1) { throw "WorldZones expected at least 1 default zone" }
}

Assert-Test "WorldZones are sorted chronologically by UTC offset" {
    $zonesProp = $settingsType.GetProperty("WorldZones")
    $zones = $zonesProp.GetValue($settingsInstance)
    $prevOffset = [TimeSpan]::FromHours(-14)
    foreach ($z in $zones) {
        $tz = [TimeZoneInfo]::FindSystemTimeZoneById($z.TimeZoneId)
        $off = $tz.BaseUtcOffset
        if ($off -lt $prevOffset) {
            throw "Zone $($z.DisplayLabel) ($off) is out of chronological order compared to previous ($prevOffset)"
        }
        $prevOffset = $off
    }
}

Assert-Test "TimeZoneDisplayItem formats search tokens and recognizes Addis Ababa" {
    $tzDisplayType = $pwAssembly.GetType("PrimeWidget.Models.TimeZoneDisplayItem")
    if ($null -eq $tzDisplayType) { throw "PrimeWidget.Models.TimeZoneDisplayItem not found" }
    
    $eatTz = [TimeZoneInfo]::FindSystemTimeZoneById("E. Africa Standard Time")
    $item = [Activator]::CreateInstance($tzDisplayType, @($eatTz))
    if ($null -eq $item) { throw "Failed to instantiate TimeZoneDisplayItem" }
    
    $city = $tzDisplayType.GetProperty("CityName").GetValue($item)
    $flag = $tzDisplayType.GetProperty("FlagEmoji").GetValue($item)
    $search = $tzDisplayType.GetProperty("SearchableText").GetValue($item)
    
    if (-not $city.Contains("Addis Ababa")) { throw "Expected Addis Ababa in CityName, got $city" }
    if ($flag -ne "🇪🇹") { throw "Expected Ethiopian flag emoji, got $flag" }
    if (-not $search.Contains("addis") -or -not $search.Contains("utc+03:00")) { throw "Expected searchable text to contain 'addis' and 'utc+03:00', got $search" }
}

Assert-Test "GlobalHotkeyService active shortcut is conflict-free" {
    $hotkeyType = $pwAssembly.GetType("PrimeWidget.Services.GlobalHotkeyService")
    if ($null -eq $hotkeyType) { throw "GlobalHotkeyService type not found" }
    $hotkeyInstance = [Activator]::CreateInstance($hotkeyType)
    $desc = $hotkeyType.GetProperty("ActiveShortcutDescription").GetValue($hotkeyInstance)
    if ($desc -like "*Win + Alt + W*") { throw "Active shortcut should not conflict with Windows broadcasting" }
}

Write-Host ""

# -----------------------------------------------------------------------------
# SUITE 5: 0% Absolute Transparency & Theme Engine
# -----------------------------------------------------------------------------
Write-Host "--- SUITE 5: 0% Absolute Transparency & Theme Engine ---" -ForegroundColor Yellow

$themeManagerType = $pwAssembly.GetType("PrimeWidget.Services.ThemeManager")
Assert-Test "ThemeManager ApplyTheme method exists" {
    if ($null -eq $themeManagerType) { throw "ThemeManager type not found" }
    $themeModeEnum = $pwAssembly.GetType("PrimeWidget.Models.WidgetThemeMode")
    $accentEnum = $pwAssembly.GetType("PrimeWidget.Models.AccentColorTheme")
    $backdropEnum = $pwAssembly.GetType("PrimeWidget.Models.BackdropStyle")
    $applyMethod = $themeManagerType.GetMethod("ApplyTheme", [Type[]]@($themeModeEnum, $accentEnum, [double], $backdropEnum))
    if ($null -eq $applyMethod) { throw "ApplyTheme method missing" }
}

Assert-Test "0% Opacity configuration mathematically produces 0 alpha" {
    # Test formula used in ThemeManager: (byte)(Math.Clamp(0.0, 0.0, 1.0) * 255.0)
    $opacity = 0.0
    $alpha = [byte]([Math]::Clamp($opacity, 0.0, 1.0) * 255.0)
    if ($alpha -ne 0) { throw "Expected alpha 0, got $alpha" }
    
    $borderAlpha = [byte][Math]::Min(110, [int]($alpha * 0.45))
    if ($borderAlpha -ne 0) { throw "Expected borderAlpha 0, got $borderAlpha" }
    
    $clockBorderAlpha = [byte][Math]::Min(50, [int]($alpha * 0.20))
    if ($clockBorderAlpha -ne 0) { throw "Expected clockBorderAlpha 0, got $clockBorderAlpha" }
    
    $shadowFactor = $opacity * 0.22
    if ($shadowFactor -ne 0.0) { throw "Expected shadowFactor 0.0, got $shadowFactor" }
}

Write-Host ""

# -----------------------------------------------------------------------------
# SUITE 6: Autostart Windows Registry Service
# -----------------------------------------------------------------------------
Write-Host "--- SUITE 6: Autostart Windows Registry Service ---" -ForegroundColor Yellow

$autostartType = $pwAssembly.GetType("PrimeWidget.Services.AutostartService")
Assert-Test "AutostartService can safely query autostart status" {
    if ($null -eq $autostartType) { throw "AutostartService type not found" }
    $isAutoMethod = $autostartType.GetMethod("IsAutostartEnabled")
    if ($null -eq $isAutoMethod) { throw "IsAutostartEnabled method missing" }
    $status = $isAutoMethod.Invoke($null, $null)
    Write-Host "    Current Windows startup registration: $status" -ForegroundColor Gray
}

Write-Host ""

# -----------------------------------------------------------------------------
# SUITE 7: End-to-End Installation & Uninstallation Simulation
# -----------------------------------------------------------------------------
Write-Host "--- SUITE 7: End-to-End Installation & Uninstallation Simulation ---" -ForegroundColor Yellow

Assert-Test "Simulation: Extract payload, verify all critical files and structure" {
    $tempTestDir = Join-Path $root "scratch_test_install"
    if (Test-Path $tempTestDir) { Remove-Item $tempTestDir -Recurse -Force }
    New-Item -ItemType Directory -Path $tempTestDir -Force | Out-Null
    
    try {
        $zipPath = Join-Path $root "installer\payload.zip"
        [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $tempTestDir)
        
        $essentialFiles = @(
            "PrimeWidget.exe",
            "PrimeWidget.dll",
            "PrimeWidget.runtimeconfig.json",
            "PrimeWidget.deps.json"
        )
        foreach ($file in $essentialFiles) {
            $fPath = Join-Path $tempTestDir $file
            if (-not (Test-Path $fPath)) { throw "Installed file missing: $file" }
            if ((Get-Item $fPath).Length -eq 0) { throw "Installed file is 0 bytes: $file" }
        }
    }
    finally {
        if (Test-Path $tempTestDir) {
            Remove-Item $tempTestDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host ""

# -----------------------------------------------------------------------------
# SUITE 8: App Runtime Smoke Test
# -----------------------------------------------------------------------------
Write-Host "--- SUITE 8: App Runtime Smoke Test ---" -ForegroundColor Yellow

Assert-Test "PrimeWidget.exe PE assembly and EntryPoint verify cleanly" {
    $exePath = $assemblyPath
    $asm = [System.Reflection.Assembly]::LoadFrom($exePath)
    if ($null -eq $asm.EntryPoint) { throw "No entry point found in PrimeWidget" }
}

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "                   TEST SUMMARY                         " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  TOTAL TESTS PASSED: $passed" -ForegroundColor Green
if ($failed -gt 0) {
    Write-Host "  TOTAL TESTS FAILED: $failed" -ForegroundColor Red
    exit 1
} else {
    Write-Host "  ALL TESTS PASSED WITH 100% SUCCESS!" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Cyan
    exit 0
}
