# Deploy Error 1053 Fix to Service
# This script must be run as Administrator

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Check for admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator', then run this script again." -ForegroundColor Yellow
  exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Deploying Error 1053 Fix" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Change to project directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "Project Directory: $projectRoot" -ForegroundColor Gray
Write-Host ""

# Step 1: Check if service exists
Write-Host "[Step 1] Checking service status..." -ForegroundColor Yellow
$service = Get-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction SilentlyContinue

if ($service) {
    Write-Host "  Service found: $($service.Status)" -ForegroundColor Green
    
    # Step 2: Stop service if running
    if ($service.Status -eq "Running") {
        Write-Host "[Step 2] Stopping service..." -ForegroundColor Yellow
   try {
            Stop-Service -Name "HuLoopBOT_RDP_Monitor" -Force -ErrorAction Stop
         Start-Sleep -Seconds 2
     Write-Host "  Service stopped successfully" -ForegroundColor Green
        } catch {
            Write-Host "  WARNING: Could not stop service: $($_.Exception.Message)" -ForegroundColor Red
            if (-not $Force) {
    Write-Host "  Run with -Force to continue anyway" -ForegroundColor Yellow
          exit 1
         }
    }
    } else {
        Write-Host "[Step 2] Service already stopped" -ForegroundColor Gray
  }
    
    # Step 3: Uninstall service
    Write-Host "[Step 3] Uninstalling service..." -ForegroundColor Yellow
    try {
    $result = sc.exe delete "HuLoopBOT_RDP_Monitor"
        if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 1072) {
       Write-Host "  Service uninstalled successfully" -ForegroundColor Green
            Start-Sleep -Seconds 2
        } else {
            throw "sc.exe delete failed with exit code $LASTEXITCODE"
        }
    } catch {
        Write-Host "  WARNING: Could not uninstall service: $($_.Exception.Message)" -ForegroundColor Red
        if (-not $Force) {
    exit 1
        }
    }
} else {
    Write-Host "[Step 1-3] Service not found - skipping uninstall" -ForegroundColor Gray
}

# Step 4: Verify service executable exists
Write-Host "[Step 4] Checking service executable..." -ForegroundColor Yellow
$serviceExe = Join-Path $projectRoot "bin\Release\net8.0-windows\HuLoopBOT_Service.exe"

if (-not (Test-Path $serviceExe)) {
    Write-Host "  ERROR: Service executable not found!" -ForegroundColor Red
    Write-Host "  Expected: $serviceExe" -ForegroundColor Red
    Write-Host "  Run 'dotnet build -c Release' first" -ForegroundColor Yellow
exit 1
}

$exeInfo = Get-Item $serviceExe
Write-Host "  Service EXE found:" -ForegroundColor Green
Write-Host "  Path: $serviceExe" -ForegroundColor Gray
Write-Host "  Size: $($exeInfo.Length) bytes" -ForegroundColor Gray
Write-Host "  Modified: $($exeInfo.LastWriteTime)" -ForegroundColor Gray
Write-Host ""

# Step 5: Install service
Write-Host "[Step 5] Installing service with Error 1053 fixes..." -ForegroundColor Yellow
try {
    $result = sc.exe create "HuLoopBOT_RDP_Monitor" binPath= "`"$serviceExe`"" start= auto DisplayName= "HuLoop BOT - RDP Session Monitor"
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe create failed with exit code $LASTEXITCODE : $result"
    }
    Write-Host "  Service created successfully" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Failed to install service: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 2

# Step 6: Set service description
Write-Host "[Step 6] Setting service description..." -ForegroundColor Yellow
try {
    $description = "Monitors RDP sessions and automatically transfers them to console on disconnect"
    sc.exe description "HuLoopBOT_RDP_Monitor" "$description" | Out-Null
    Write-Host "  Description set successfully" -ForegroundColor Green
} catch {
 Write-Host "  WARNING: Could not set description: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 7: Configure failure recovery
Write-Host "[Step 7] Configuring failure recovery..." -ForegroundColor Yellow
try {
    sc.exe failure "HuLoopBOT_RDP_Monitor" reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
  Write-Host "  Failure recovery configured" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: Could not configure failure recovery: $($_.Exception.Message)" -ForegroundColor Yellow
}

Start-Sleep -Seconds 2

# Step 8: Verify service is accessible
Write-Host "[Step 8] Verifying service registration..." -ForegroundColor Yellow
$service = Get-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "  Service registered successfully" -ForegroundColor Green
    Write-Host "  Current status: $($service.Status)" -ForegroundColor Gray
} else {
    Write-Host "  ERROR: Service not accessible after installation!" -ForegroundColor Red
    exit 1
}

# Step 9: Ensure registry key is set
Write-Host "[Step 9] Checking registry configuration..." -ForegroundColor Yellow
$regPath = "HKLM:\SOFTWARE\HuLoopBOT"
try {
    if (-not (Test-Path $regPath)) {
      New-Item -Path $regPath -Force | Out-Null
        Write-Host "  Created registry key: $regPath" -ForegroundColor Green
    }
    
    $enabledValue = Get-ItemProperty -Path $regPath -Name "RdpMonitoringEnabled" -ErrorAction SilentlyContinue
    if ($null -eq $enabledValue -or $enabledValue.RdpMonitoringEnabled -ne 1) {
     Set-ItemProperty -Path $regPath -Name "RdpMonitoringEnabled" -Value 1 -Type DWord
        Write-Host "  Set RdpMonitoringEnabled = 1" -ForegroundColor Green
    } else {
        Write-Host "  RdpMonitoringEnabled already set to 1" -ForegroundColor Gray
    }
} catch {
    Write-Host "  WARNING: Could not set registry key: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 10: Start service
Write-Host "[Step 10] Starting service with Error 1053 fix..." -ForegroundColor Yellow
Write-Host "  This should now complete within 3 seconds..." -ForegroundColor Gray

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    Start-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction Stop
    $stopwatch.Stop()
    
    Write-Host "  Service started successfully in $($stopwatch.ElapsedMilliseconds)ms!" -ForegroundColor Green
    
    Start-Sleep -Seconds 2
    
    # Verify service is running
    $service = Get-Service -Name "HuLoopBOT_RDP_Monitor"
    Write-Host "  Current status: $($service.Status)" -ForegroundColor Green
    
} catch {
    $stopwatch.Stop()
    Write-Host "  ERROR: Service failed to start after $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Check if it's still Error 1053
    if ($_.Exception.Message -match "1053" -or $_.Exception.Message -match "did not respond") {
    Write-Host ""
   Write-Host "  Still getting Error 1053!" -ForegroundColor Red
    Write-Host "  This means the service OnStart() is still taking too long." -ForegroundColor Yellow
        Write-Host ""
      Write-Host "  Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Check service log: C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" -ForegroundColor Gray
        Write-Host "  2. Check Event Viewer: eventvwr.msc -> Application logs" -ForegroundColor Gray
        Write-Host "  3. Verify the service executable was rebuilt with fixes" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "  Check logs for details:" -ForegroundColor Yellow
    Write-Host "  C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" -ForegroundColor Gray
    
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Deployment Successful!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Service has been deployed with Error 1053 fixes." -ForegroundColor White
Write-Host ""
Write-Host "To verify:" -ForegroundColor Yellow
Write-Host "  1. Check service status: Get-Service HuLoopBOT_RDP_Monitor" -ForegroundColor Gray
Write-Host "  2. View logs: Get-Content C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log -Tail 50" -ForegroundColor Gray
Write-Host "  3. Check for success message in logs: Service Started Successfully" -ForegroundColor Gray
Write-Host ""
Write-Host "Expected log indicators:" -ForegroundColor Yellow
Write-Host "  - OnStart completed successfully (within 3 seconds)" -ForegroundColor Gray
Write-Host "  - Service Started Successfully" -ForegroundColor Gray
Write-Host "  - Window Handle: 0x[non-zero]" -ForegroundColor Gray
Write-Host "  - Ready to monitor RDP sessions" -ForegroundColor Gray
Write-Host ""

# Show last few lines of log
Write-Host "Recent service log entries:" -ForegroundColor Cyan
$logPath = "C:\ProgramData\HuLoopBOT\Logs"
if (Test-Path $logPath) {
    $latestLog = Get-ChildItem -Path $logPath -Filter "HuLoopBOT_Service_*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Write-Host "Log file: $($latestLog.FullName)" -ForegroundColor Gray
        Write-Host ""
        Get-Content $latestLog.FullName -Tail 20 | ForEach-Object {
            if ($_ -match "ERROR") {
         Write-Host $_ -ForegroundColor Red
            } elseif ($_ -match "WARNING") {
 Write-Host $_ -ForegroundColor Yellow
    } elseif ($_ -match "?|SUCCESS|Started Successfully") {
            Write-Host $_ -ForegroundColor Green
         } else {
     Write-Host $_ -ForegroundColor Gray
            }
        }
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
