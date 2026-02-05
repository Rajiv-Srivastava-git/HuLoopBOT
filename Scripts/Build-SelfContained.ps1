# Build Self-Contained Service
# Run this on your DEVELOPMENT machine (no admin needed)

param(
    [string]$OutputDir = "publish-self-contained"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Building Self-Contained Service" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

Write-Host "Project: $projectRoot" -ForegroundColor Gray
Write-Host "Output: $OutputDir" -ForegroundColor Gray
Write-Host ""

# Step 1: Clean previous builds
Write-Host "[Step 1/4] Cleaning previous builds..." -ForegroundColor Yellow
try {
    if (Test-Path $OutputDir) {
        Remove-Item -Path $OutputDir -Recurse -Force
      Write-Host "  Cleaned old output" -ForegroundColor Gray
    }
    
    dotnet clean ServiceHost\HuLoopBOT_Service.csproj -c Release | Out-Null
    dotnet clean -c Release | Out-Null
    
    Write-Host "  ? Clean complete" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Clean failed - $($_.Exception.Message)" -ForegroundColor Red
  exit 1
}

# Step 2: Publish SERVICE as self-contained
Write-Host "[Step 2/4] Publishing service (self-contained)..." -ForegroundColor Yellow
Write-Host "  This includes .NET runtime in output" -ForegroundColor Gray

try {
    dotnet publish ServiceHost\HuLoopBOT_Service.csproj `
        -c Release `
     -r win-x64 `
        --self-contained true `
      -p:PublishSingleFile=false `
        -p:PublishTrimmed=false `
        -o "$OutputDir\Service"
    
    if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
  
    Write-Host "  ? Service published" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Publish failed - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Publish MAIN APP as self-contained (optional, for testing)
Write-Host "[Step 3/4] Publishing main app (self-contained)..." -ForegroundColor Yellow

try {
    dotnet publish HuLoopBOT.csproj `
        -c Release `
 -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=false `
 -p:PublishTrimmed=false `
-o "$OutputDir\MainApp"
    
    if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
    
    Write-Host "  ? Main app published" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Publish failed - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 4: Create deployment package
Write-Host "[Step 4/4] Creating deployment package..." -ForegroundColor Yellow

try {
    # Create Scripts directory in output
    $scriptsDir = Join-Path $OutputDir "Scripts"
    New-Item -ItemType Directory -Path $scriptsDir -Force | Out-Null
    
    # Copy deployment scripts
    Copy-Item "Scripts\Install-Service-SelfContained.ps1" -Destination $scriptsDir -ErrorAction SilentlyContinue
    Copy-Item "Scripts\Uninstall-Service.ps1" -Destination $scriptsDir -ErrorAction SilentlyContinue
    
    # Create README
    $readme = @"
# HuLoopBOT Self-Contained Deployment

## Contents
- Service\     - Windows Service (self-contained, includes .NET runtime)
- MainApp\   - Main application (self-contained)
- Scripts\- Installation scripts

## Installation (Run on TEST machine with Admin rights)

1. Copy this entire folder to the test machine
2. Open PowerShell as Administrator
3. Navigate to this folder
4. Run: .\Scripts\Install-Service-SelfContained.ps1

## Requirements
- Windows 10/11 or Windows Server 2016+
- Administrator privileges (for service installation)
- NO .NET installation required! (included in package)

## Service Details
- Service Name: HuLoopBOT_RDP_Monitor
- Display Name: HuLoop BOT - RDP Session Monitor
- Description: Monitors RDP sessions and automatically transfers them to console on disconnect

## Troubleshooting
If service fails to start:
1. Check logs: C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log
2. Check Event Viewer: eventvwr.msc -> Application logs
3. Ensure registry: HKLM\SOFTWARE\HuLoopBOT\RdpMonitoringEnabled = 1

"@
    
    Set-Content -Path (Join-Path $OutputDir "README.txt") -Value $readme
    
    Write-Host "  ? Deployment package created" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: Could not create deployment package - $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 5: Verify output
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

$serviceExe = Join-Path $OutputDir "Service\HuLoopBOT_Service.exe"
$mainExe = Join-Path $OutputDir "MainApp\HuLoopBOT.exe"

if (Test-Path $serviceExe) {
  $size = (Get-Item $serviceExe).Length
    Write-Host "? Service EXE: $serviceExe" -ForegroundColor Green
    Write-Host "  Size: $([math]::Round($size/1MB, 2)) MB" -ForegroundColor Gray
} else {
    Write-Host "? Service EXE not found!" -ForegroundColor Red
}

if (Test-Path $mainExe) {
    $size = (Get-Item $mainExe).Length
    Write-Host "? Main App EXE: $mainExe" -ForegroundColor Green
    Write-Host "  Size: $([math]::Round($size/1MB, 2)) MB" -ForegroundColor Gray
} else {
    Write-Host "? Main App EXE not found!" -ForegroundColor Red
}

Write-Host ""
Write-Host "Output Directory: $OutputDir" -ForegroundColor Cyan
Write-Host ""

# Count files
$serviceFiles = (Get-ChildItem -Path (Join-Path $OutputDir "Service") -Recurse -File).Count
$mainFiles = (Get-ChildItem -Path (Join-Path $OutputDir "MainApp") -Recurse -File).Count

Write-Host "Service files: $serviceFiles" -ForegroundColor Gray
Write-Host "Main app files: $mainFiles" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Next Steps" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Copy the '$OutputDir' folder to your test machine" -ForegroundColor White
Write-Host "2. On test machine, open PowerShell as Administrator" -ForegroundColor White
Write-Host "3. Run: .\Scripts\Install-Service-SelfContained.ps1" -ForegroundColor White
Write-Host ""
Write-Host "The package is self-contained and includes .NET runtime!" -ForegroundColor Green
Write-Host ""
