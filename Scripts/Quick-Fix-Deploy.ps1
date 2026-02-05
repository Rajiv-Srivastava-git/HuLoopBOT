# Quick Fix Deployment for Error 1053
# Run as Administrator

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Quick Fix: Rebuilding and Redeploying" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Must run as Administrator!" -ForegroundColor Red
    exit 1
}

# Get project directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "Project: $projectRoot" -ForegroundColor Gray
Write-Host ""

# Step 1: Rebuild service
Write-Host "[Step 1/5] Rebuilding service with fix..." -ForegroundColor Yellow
try {
    dotnet clean ServiceHost\HuLoopBOT_Service.csproj -c Release | Out-Null
    dotnet build ServiceHost\HuLoopBOT_Service.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    Write-Host "  ? Service rebuilt" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Build failed" -ForegroundColor Red
    exit 1
}

# Step 2: Rebuild main project
Write-Host "[Step 2/5] Rebuilding main project..." -ForegroundColor Yellow
try {
    dotnet build -c Release --no-restore | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    Write-Host "  ? Main project rebuilt" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Build failed" -ForegroundColor Red
    exit 1
}

# Step 3: Stop service
Write-Host "[Step 3/5] Stopping service..." -ForegroundColor Yellow
$service = Get-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction SilentlyContinue
if ($service -and $service.Status -eq "Running") {
    try {
     Stop-Service -Name "HuLoopBOT_RDP_Monitor" -Force
        Start-Sleep -Seconds 2
        Write-Host "  ? Service stopped" -ForegroundColor Green
    } catch {
        Write-Host "  WARNING: Could not stop service" -ForegroundColor Yellow
}
} else {
  Write-Host "  ? Service not running" -ForegroundColor Gray
}

# Step 4: Uninstall service
Write-Host "[Step 4/5] Uninstalling old service..." -ForegroundColor Yellow
if ($service) {
    try {
        sc.exe delete "HuLoopBOT_RDP_Monitor" | Out-Null
        Start-Sleep -Seconds 2
        Write-Host "  ? Service uninstalled" -ForegroundColor Green
 } catch {
        Write-Host "  WARNING: Could not uninstall" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ? Service not installed" -ForegroundColor Gray
}

# Step 5: Install new service
Write-Host "[Step 5/5] Installing new service..." -ForegroundColor Yellow
$serviceExe = Join-Path $projectRoot "bin\Release\net8.0-windows\HuLoopBOT_Service.exe"

if (-not (Test-Path $serviceExe)) {
    Write-Host "  ERROR: Service executable not found!" -ForegroundColor Red
    Write-Host "  Path: $serviceExe" -ForegroundColor Red
    exit 1
}

try {
    $result = sc.exe create "HuLoopBOT_RDP_Monitor" binPath= "`"$serviceExe`"" start= auto DisplayName= "HuLoop BOT - RDP Session Monitor"
if ($LASTEXITCODE -ne 0) { throw "Install failed" }
    
    sc.exe description "HuLoopBOT_RDP_Monitor" "Monitors RDP sessions and automatically transfers them to console on disconnect" | Out-Null
    sc.exe failure "HuLoopBOT_RDP_Monitor" reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    
    Write-Host "  ? Service installed" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Installation failed" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 2

# Ensure registry
$regPath = "HKLM:\SOFTWARE\HuLoopBOT"
if (-not (Test-Path $regPath)) {
    New-Item -Path $regPath -Force | Out-Null
}
Set-ItemProperty -Path $regPath -Name "RdpMonitoringEnabled" -Value 1 -Type DWord

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Starting Service (Should take < 3s)" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    Start-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction Stop
    $stopwatch.Stop()
    
    Write-Host "??? SUCCESS! ???" -ForegroundColor Green
    Write-Host "Service started in $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Green
    Write-Host ""
    
    $service = Get-Service -Name "HuLoopBOT_RDP_Monitor"
    Write-Host "Status: $($service.Status)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Check logs at:" -ForegroundColor Cyan
    Write-Host "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" -ForegroundColor Gray
    
} catch {
    $stopwatch.Stop()
    Write-Host "? FAILED after $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Message -match "1053") {
        Write-Host ""
        Write-Host "Still Error 1053! Check the service log:" -ForegroundColor Yellow
        Write-Host "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" -ForegroundColor Gray
    }
    
    exit 1
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
