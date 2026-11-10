# ==============================================================================
# AutoIME MSIX Build Script
# ==============================================================================
param (
    [string]$Configuration = "Release",
    [string]$Platform = "win-x64"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectRoot = Resolve-Path "$ScriptDir\.."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AutoIME MSIX Package Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Clean previous build artifacts
$appPackagesDir = "$ProjectRoot\src\AutoIME\AppPackages"
if (Test-Path $appPackagesDir) {
    Remove-Item -Recurse -Force $appPackagesDir -ErrorAction SilentlyContinue
}

$OutDir = "$ProjectRoot\dist-msix"
if (Test-Path $OutDir) {
    Remove-Item -Recurse -Force "$OutDir\*" -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

Write-Host "`n[1/2] Compiling and packaging MSIX ($Configuration $Platform)..." -ForegroundColor Yellow

Push-Location $ProjectRoot
try {
    dotnet publish src/AutoIME/AutoIME.csproj `
        -c $Configuration `
        -r $Platform `
        -p:WindowsPackageType=MSIX `
        -p:EnableMsixTooling=true `
        -p:GenerateAppxPackageOnBuild=true `
        -p:AppxPackageSigningEnabled=false `
        -p:EnableDefaultPriItems=false `
        -o "$OutDir\_temp"
} finally {
    Pop-Location
}

Write-Host "`n[2/2] Collecting MSIX packages..." -ForegroundColor Yellow

$packages = Get-ChildItem -Path "$ProjectRoot\src\AutoIME\AppPackages" -Recurse | `
    Where-Object { $_.Extension -match "^\.(msix|msixbundle)$" }

if ($packages) {
    foreach ($pkg in $packages) {
        Copy-Item -Path $pkg.FullName -Destination $OutDir -Force
        Write-Host "  -> Copied: $($pkg.Name)" -ForegroundColor Green
    }
}

Remove-Item -Recurse -Force "$OutDir\_temp" -ErrorAction SilentlyContinue

# Sign with local test certificate if available, and export .cer
$publisher = "CN=8FBE9AE2-DEFD-4655-8015-EAAE068A82F9"
$cert = Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue | Where-Object { $_.Subject -eq $publisher } | Select-Object -First 1
if ($cert) {
    $cerPath = Join-Path $OutDir "AutoIME_LocalTest.cer"
    Export-Certificate -Cert $cert -FilePath $cerPath -Force | Out-Null
    Write-Host "  -> Exported Certificate: AutoIME_LocalTest.cer" -ForegroundColor Green

    $signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"
    if (Test-Path $signtool) {
        Get-ChildItem -Path $OutDir -Filter "*.msixbundle" | ForEach-Object {
            & $signtool sign /fd SHA256 /a /sha1 $cert.Thumbprint $_.FullName | Out-Null
            Write-Host "  -> Signed for local testing: $($_.Name)" -ForegroundColor Green
        }
    }
}

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "  MSIX packaging completed successfully!" -ForegroundColor Green
Write-Host "  Output Directory: $OutDir" -ForegroundColor White
Write-Host "========================================================`n" -ForegroundColor Green

