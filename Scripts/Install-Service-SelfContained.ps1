# Install Self-Contained Service
# Run this on TEST machine as Administrator

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Installing Self-Contained Service" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Get service directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$deployRoot = Split-Path -Parent $scriptPath
$serviceDir = Join-Path $deployRoot "Service"

Write-Host "Deployment root: $deployRoot" -ForegroundColor Gray
Write-Host "Service directory: $serviceDir" -ForegroundColor Gray
Write-Host ""

# Verify service executable exists
$serviceExe = Join-Path $serviceDir "HuLoopBOT_Service.exe"

if (-not (Test-Path $serviceExe)) {
    Write-Host "ERROR: Service executable not found!" -ForegroundColor Red
    Write-Host "Expected: $serviceExe" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure you:" -ForegroundColor Yellow
    Write-Host "1. Copied the entire 'publish-self-contained' folder" -ForegroundColor Yellow
    Write-Host "2. Are running this script from the Scripts subdirectory" -ForegroundColor Yellow
    exit 1
}

$exeInfo = Get-Item $serviceExe
Write-Host "? Service executable found" -ForegroundColor Green
Write-Host "  Path: $serviceExe" -ForegroundColor Gray
Write-Host "  Size: $([math]::Round($exeInfo.Length/1MB, 2)) MB" -ForegroundColor Gray
Write-Host "  Modified: $($exeInfo.LastWriteTime)" -ForegroundColor Gray
Write-Host ""

# Check if service exists
Write-Host "[Step 1/6] Checking existing service..." -ForegroundColor Yellow
$service = Get-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction SilentlyContinue

if ($service) {
    Write-Host "  Service already exists (Status: $($service.Status))" -ForegroundColor Gray
    
    # Stop if running
    if ($service.Status -eq "Running") {
        Write-Host "  Stopping service..." -ForegroundColor Gray
        try {
            Stop-Service -Name "HuLoopBOT_RDP_Monitor" -Force
     Start-Sleep -Seconds 2
          Write-Host "  ? Service stopped" -ForegroundColor Green
        } catch {
            Write-Host "  WARNING: Could not stop service" -ForegroundColor Yellow
       if (-not $Force) {
 Write-Host "  Use -Force to continue anyway" -ForegroundColor Yellow
        exit 1
            }
        }
    }
    
  # Uninstall
    Write-Host "  Uninstalling old service..." -ForegroundColor Gray
    try {
        sc.exe delete "HuLoopBOT_RDP_Monitor" | Out-Null
        Start-Sleep -Seconds 2
     Write-Host "  ? Old service uninstalled" -ForegroundColor Green
    } catch {
        Write-Host "  WARNING: Could not uninstall old service" -ForegroundColor Yellow
        if (-not $Force) {
     exit 1
}
    }
} else {
    Write-Host "  ? No existing service" -ForegroundColor Gray
}

# Create registry key
Write-Host "[Step 2/6] Configuring registry..." -ForegroundColor Yellow
try {
    $regPath = "HKLM:\SOFTWARE\HuLoopBOT"
    if (-not (Test-Path $regPath)) {
      New-Item -Path $regPath -Force | Out-Null
Write-Host "  Created registry key" -ForegroundColor Gray
    }
    
  Set-ItemProperty -Path $regPath -Name "RdpMonitoringEnabled" -Value 1 -Type DWord
    Write-Host "  ? Registry configured (RdpMonitoringEnabled = 1)" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: Could not set registry - $($_.Exception.Message)" -ForegroundColor Yellow
}

# Install service
Write-Host "[Step 3/6] Installing service..." -ForegroundColor Yellow
try {
    $result = sc.exe create "HuLoopBOT_RDP_Monitor" binPath= "`"$serviceExe`"" start= auto DisplayName= "HuLoop BOT - RDP Session Monitor"
    
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe create failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "  ? Service installed" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Installation failed - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 2

# Set description
Write-Host "[Step 4/6] Setting service description..." -ForegroundColor Yellow
try {
    sc.exe description "HuLoopBOT_RDP_Monitor" "Monitors RDP sessions and automatically transfers them to console on disconnect" | Out-Null
 Write-Host "  ? Description set" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: Could not set description" -ForegroundColor Yellow
}

# Configure failure recovery
Write-Host "[Step 5/6] Configuring failure recovery..." -ForegroundColor Yellow
try {
    sc.exe failure "HuLoopBOT_RDP_Monitor" reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    Write-Host "  ? Failure recovery configured" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: Could not configure failure recovery" -ForegroundColor Yellow
}

Start-Sleep -Seconds 2

# Verify installation
Write-Host "[Step 6/6] Verifying installation..." -ForegroundColor Yellow
$service = Get-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction SilentlyContinue

if ($service) {
    Write-Host "  ? Service registered successfully" -ForegroundColor Green
    Write-Host "  Current status: $($service.Status)" -ForegroundColor Gray
} else {
    Write-Host "  ERROR: Service not found after installation!" -ForegroundColor Red
 exit 1
}

# Start service
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Starting Service" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "This should complete in < 3 seconds..." -ForegroundColor Gray
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    Start-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction Stop
    $stopwatch.Stop()
  
    Write-Host "??? SUCCESS! ???" -ForegroundColor Green
    Write-Host ""
    Write-Host "Service started in $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Green
    Write-Host ""
    
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name "HuLoopBOT_RDP_Monitor"
    Write-Host "Current status: $($service.Status)" -ForegroundColor Green
    Write-Host ""
    
    # Show log location
    Write-Host "Service logs:" -ForegroundColor Cyan
    Write-Host "  C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" -ForegroundColor Gray
    Write-Host ""
    
    # Try to show recent log entries
    $logPath = "C:\ProgramData\HuLoopBOT\Logs"
    if (Test-Path $logPath) {
        $latestLog = Get-ChildItem -Path $logPath -Filter "HuLoopBOT_Service_*.log" -ErrorAction SilentlyContinue | 
            Sort-Object LastWriteTime -Descending | 
            Select-Object -First 1
  
        if ($latestLog) {
    Write-Host "Recent log entries:" -ForegroundColor Cyan
      Write-Host ""
       Get-Content $latestLog.FullName -Tail 15 -ErrorAction SilentlyContinue | ForEach-Object {
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
    
} catch {
    $stopwatch.Stop()
    
    Write-Host "??? FAILED ???" -ForegroundColor Red
    Write-Host ""
    Write-Host "Service failed to start after $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    
    if ($_.Exception.Message -match "1053") {
     Write-Host "Error 1053: Service did not respond in time" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
        Write-Host "1. Check service log:" -ForegroundColor Gray
        Write-Host "   C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" -ForegroundColor Gray
        Write-Host ""
        Write-Host "2. Check Event Viewer:" -ForegroundColor Gray
        Write-Host "   eventvwr.msc -> Application logs" -ForegroundColor Gray
        Write-Host ""
        Write-Host "3. Verify .NET runtime is included:" -ForegroundColor Gray
        $runtimeDll = Join-Path $serviceDir "System.Runtime.dll"
        if (Test-Path $runtimeDll) {
       Write-Host "   ? .NET runtime found in package" -ForegroundColor Green
        } else {
    Write-Host "   ? .NET runtime NOT found - rebuild with --self-contained" -ForegroundColor Red
        }
    }
    
 exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Service: HuLoopBOT_RDP_Monitor" -ForegroundColor White
Write-Host "Status: Running" -ForegroundColor Green
Write-Host "Startup: Automatic" -ForegroundColor White
Write-Host ""
Write-Host "To verify:" -ForegroundColor Yellow
Write-Host "  Get-Service HuLoopBOT_RDP_Monitor" -ForegroundColor Gray
Write-Host ""
Write-Host "To view logs:" -ForegroundColor Yellow
Write-Host "  Get-Content C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log -Tail 20" -ForegroundColor Gray
Write-Host ""
Write-Host "Done!" -ForegroundColor Green
