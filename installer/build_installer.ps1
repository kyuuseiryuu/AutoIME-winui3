# ==============================================================================
# AutoIME Installer Build Script
# ==============================================================================
param (
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectRoot = Resolve-Path "$ScriptDir\.."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AutoIME Installer Builder (Inno Setup)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Publish Application to dist
if (-not $SkipPublish) {
    Write-Host "`n[1/3] Publishing AutoIME (Release win-x64)..." -ForegroundColor Yellow
    Push-Location $ProjectRoot
    try {
        dotnet publish src/AutoIME/AutoIME.csproj -c Release -r win-x64 --self-contained true -o ./dist
        $docFiles = @("AutoIME_config.example.ini", "LICENSE", "README.md", "README_zh.md", "PRIVACY.md")
        foreach ($file in $docFiles) {
            if (Test-Path $file) {
                Copy-Item $file "./dist/" -Force
            }
        }
    } finally {
        Pop-Location
    }
} else {
    Write-Host "`n[1/3] Skipping publish step as requested." -ForegroundColor DarkGray
}

# 2. Check Inno Setup Compiler (ISCC.exe)
Write-Host "`n[2/3] Checking Inno Setup Compiler (iscc.exe)..." -ForegroundColor Yellow

$isccPath = $null
$possiblePaths = @(
    (Get-Command iscc.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source),
    "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe",
    "${env:ProgramFiles}\Inno Setup 6\iscc.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 7\iscc.exe",
    "${env:ProgramFiles}\Inno Setup 7\iscc.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 6\iscc.exe"
)

foreach ($p in $possiblePaths) {
    if (![string]::IsNullOrEmpty($p) -and (Test-Path $p)) {
        $isccPath = $p
        break
    }
}

if (-not $isccPath) {
    Write-Host "`n[ERROR] Inno Setup compiler (iscc.exe) was not found!" -ForegroundColor Red
    Write-Host "Please install Inno Setup by running this command in PowerShell:" -ForegroundColor Yellow
    Write-Host "  winget install JRSoftware.InnoSetup`n" -ForegroundColor White
    exit 1
}

Write-Host "Found Inno Setup at: $isccPath" -ForegroundColor Green

# 3. Compile Installer
Write-Host "`n[3/3] Compiling installer..." -ForegroundColor Yellow
$issPath = "$ScriptDir\AutoIME.iss"

& "$isccPath" "$issPath"

if ($LASTEXITCODE -eq 0) {
    $outDir = Resolve-Path "$ProjectRoot\dist-installer"
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host "  Installer generated successfully!" -ForegroundColor Green
    Write-Host "  Location: $outDir" -ForegroundColor White
    Write-Host "========================================================`n" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] Inno Setup compilation failed with exit code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}

