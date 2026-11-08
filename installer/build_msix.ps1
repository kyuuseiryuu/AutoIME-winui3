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

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "  MSIX packaging completed successfully!" -ForegroundColor Green
Write-Host "  Output Directory: $OutDir" -ForegroundColor White
Write-Host "========================================================`n" -ForegroundColor Green

