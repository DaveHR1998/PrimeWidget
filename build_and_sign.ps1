$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $root ".dotnet"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

Write-Output "=== 1. Compiling PrimeWidget to publish/ ==="
$appProj = Join-Path $root "src\PrimeWidget\PrimeWidget.csproj"
$publishDir = Join-Path $root "publish"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
}
dotnet publish $appProj -c Release --no-self-contained --no-restore -o $publishDir
Get-ChildItem -Path $publishDir -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Output "=== 2. Loading Code Signing Certificate ==="
$pfxPath = Join-Path $root "PrimeWidgetCert.pfx"
$cerPath = Join-Path $root "PrimeWidgetCert.cer"
$pfxPassword = "PrimeWidgetPassword123!"

$cert = $null
if (Test-Path $pfxPath) {
    try {
        $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
            $pfxPath,
            $pfxPassword,
            [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable -bor [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet
        )
        Write-Output "Certificate loaded: $($cert.Subject) [Thumbprint: $($cert.Thumbprint)]"
    } catch {
        Write-Warning "Could not load certificate: $($_.Exception.Message)"
    }
} else {
    Write-Output "Notice: PrimeWidgetCert.pfx not found. Proceeding with unsigned build."
}

Write-Output "=== 3. Signing publish\PrimeWidget.exe ==="
$publishExe = Join-Path $publishDir "PrimeWidget.exe"
if (Test-Path $publishExe) {
    if ($cert) {
        Set-AuthenticodeSignature -FilePath $publishExe -Certificate $cert | Out-Null
        Write-Output "Signed publish\PrimeWidget.exe ($([math]::Round((Get-Item $publishExe).Length / 1MB, 2)) MB)"
    } else {
        Write-Output "Created publish\PrimeWidget.exe ($([math]::Round((Get-Item $publishExe).Length / 1MB, 2)) MB) [Unsigned]"
    }
}

Write-Output "=== 4. Packaging installer\payload.zip ==="
$payloadZip = Join-Path $root "installer\payload.zip"
if (Test-Path $payloadZip) {
    Remove-Item $payloadZip -Force
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($publishDir, $payloadZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
Write-Output "Created $payloadZip ($([math]::Round((Get-Item $payloadZip).Length / 1MB, 2)) MB)"

Write-Output "=== 5. Building Lightweight Single-File Installer (PrimeWidget-Setup.exe) ==="
$installerProj = Join-Path $root "installer\PrimeWidgetSetup.csproj"
$distDir = Join-Path $root "dist"
if (Test-Path $distDir) {
    Get-ChildItem -Path $distDir -Filter "PrimeWidget-Setup.*" -ErrorAction SilentlyContinue | Remove-Item -Force
} else {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}

dotnet publish $installerProj -c Release -r win-x64 --no-self-contained --no-restore -o $distDir
$setupExe = Join-Path $distDir "PrimeWidget-Setup.exe"

# Remove debug symbols from dist
Get-ChildItem -Path $distDir -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Output "=== 6. Signing dist\PrimeWidget-Setup.exe ==="
if (Test-Path $setupExe) {
    if ($cert) {
        Set-AuthenticodeSignature -FilePath $setupExe -Certificate $cert | Out-Null
        $sig = Get-AuthenticodeSignature $setupExe
        Write-Output "Signed PrimeWidget-Setup.exe ($([math]::Round((Get-Item $setupExe).Length / 1MB, 2)) MB) [Status: $($sig.Status)]"
    } else {
        Write-Output "Created PrimeWidget-Setup.exe ($([math]::Round((Get-Item $setupExe).Length / 1MB, 2)) MB) [Unsigned]"
    }
}

# Copy cert and trust script to dist folder as well
Copy-Item $cerPath (Join-Path $distDir "PrimeWidgetCert.cer") -Force
Copy-Item (Join-Path $root "Trust-PrimeWidget.cmd") (Join-Path $distDir "Trust-PrimeWidget.cmd") -Force

Write-Output "=== Build and Signing Complete ==="
Write-Output "Lightweight Installer ready at: $setupExe ($([math]::Round((Get-Item $setupExe).Length / 1MB, 2)) MB)"
