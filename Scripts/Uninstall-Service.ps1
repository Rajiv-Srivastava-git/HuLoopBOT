# Uninstall Service
# Run on test machine as Administrator

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Uninstalling HuLoopBOT Service" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Check if service exists
$service = Get-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "Service not found - nothing to uninstall" -ForegroundColor Yellow
    exit 0
}

Write-Host "Service found (Status: $($service.Status))" -ForegroundColor Gray
Write-Host ""

# Stop service
if ($service.Status -eq "Running") {
    Write-Host "[Step 1/2] Stopping service..." -ForegroundColor Yellow
    try {
      Stop-Service -Name "HuLoopBOT_RDP_Monitor" -Force
        Start-Sleep -Seconds 2
        Write-Host "  ? Service stopped" -ForegroundColor Green
    } catch {
      Write-Host "  WARNING: Could not stop service - $($_.Exception.Message)" -ForegroundColor Yellow
  }
} else {
    Write-Host "[Step 1/2] Service already stopped" -ForegroundColor Gray
}

# Uninstall service
Write-Host "[Step 2/2] Uninstalling service..." -ForegroundColor Yellow
try {
    sc.exe delete "HuLoopBOT_RDP_Monitor" | Out-Null
    
    if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 1072) {
        Write-Host "  ? Service uninstalled" -ForegroundColor Green
    } else {
        throw "sc.exe delete failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Host "  ERROR: Uninstall failed - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 2

# Verify
$service = Get-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction SilentlyContinue

if ($service) {
    Write-Host ""
    Write-Host "WARNING: Service still exists after uninstall" -ForegroundColor Yellow
    Write-Host "It may be marked for deletion on next reboot" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Uninstall Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}

Write-Host ""
Write-Host "Note: Registry settings and logs were NOT removed" -ForegroundColor Gray
Write-Host "  Registry: HKLM\SOFTWARE\HuLoopBOT" -ForegroundColor Gray
Write-Host "  Logs: C:\ProgramData\HuLoopBOT\Logs\" -ForegroundColor Gray
Write-Host ""
