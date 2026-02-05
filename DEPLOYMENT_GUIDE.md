# HuLoopBOT Deployment Guide

> **Comprehensive guide for building, publishing, and deploying HuLoopBOT**  
> Version: 1.0 | Target Framework: .NET 8.0 (Windows)

---

## ?? Table of Contents

- [Overview](#-overview)
- [System Architecture](#-system-architecture)
- [Prerequisites](#-prerequisites)
- [Build Methods](#-build-methods)
- [Deployment Scenarios](#-deployment-scenarios)
- [Installation Procedures](#-installation-procedures)
- [Verification & Testing](#-verification--testing)
- [Troubleshooting](#-troubleshooting)
- [Maintenance](#-maintenance)

---

## ?? Overview

**HuLoopBOT** is a two-component system designed to monitor and manage RDP sessions on Windows:

| Component | Type | Purpose | Runtime |
|-----------|------|---------|---------|
| **HuLoopBOT** | Windows Forms Application | GUI for configuration and control | Interactive |
| **HuLoopBOT_Service** | Windows Service | Background RDP session monitoring | System service |

### Key Features

- ? Automatic RDP session transfer on disconnect
- ? Background monitoring with Windows Service
- ? GUI-based configuration and management
- ? Comprehensive logging and diagnostics
- ? Self-contained deployment option (no .NET required on target)

---

## ?? System Architecture

### Project Structure

```
HuLoopBOT_Full_Project/
??? HuLoopBOT.csproj     # Main Windows Forms application
??? ServiceHost/
?   ??? HuLoopBOT_Service.csproj  # Windows Service project
??? Services/        # Shared business logic
??? Utilities/     # Shared utilities (Logger, etc.)
??? Models/           # Shared data models
??? Scripts/    # Build and deployment scripts
```

### Build Integration

The `HuLoopBOT.csproj` includes **custom MSBuild targets** that:

1. ? Automatically build the service project before the main app
2. ? Copy service files to the main app's output directory
3. ? Ensure service files are included in publish operations
4. ? Verify service executable exists in output

**Key Point:** You only need to build/publish `HuLoopBOT.csproj` — the service is built automatically!

---

## ?? Prerequisites

### Development Environment

- **Operating System:** Windows 10/11 or Windows Server 2016+
- **.NET SDK:** .NET 8.0 SDK or later
- **IDE:** Visual Studio 2022 (recommended) or Visual Studio Code
- **PowerShell:** Version 5.1 or later

### Target Machine Requirements

#### Option A: Framework-Dependent Deployment
- ? Windows 10/11 or Windows Server 2016+
- ? .NET 8.0 Runtime installed
- ? Administrator privileges

#### Option B: Self-Contained Deployment (Recommended)
- ? Windows 10/11 or Windows Server 2016+
- ? Administrator privileges
- ? **No .NET installation required** (included in package)

### NuGet Dependencies

Both projects use the following packages (automatically restored):

```xml
<PackageReference Include="System.DirectoryServices.AccountManagement" Version="10.0.2" />
<PackageReference Include="System.ServiceProcess.ServiceController" Version="9.0.0" />
<PackageReference Include="System.Threading.AccessControl" Version="8.0.0" />
```

---

## ?? Build Methods

### Method 1: Visual Studio Build (Development)

#### Debug Build
1. Open `HuLoopBOT.sln` in Visual Studio
2. Set Configuration to **Debug**
3. Build > Rebuild Solution (Ctrl+Shift+B)

**Output:** `bin\Debug\net8.0-windows\`

#### Release Build
1. Set Configuration to **Release**
2. Build > Rebuild Solution
3. Verify service executable exists in output

**Output:** `bin\Release\net8.0-windows\`

### Method 2: Command Line Build

```powershell
# Navigate to project root
cd D:\HuLoop\Development\HuLoopBOT_Full_Project\

# Restore dependencies
dotnet restore

# Build Release configuration
dotnet build HuLoopBOT.csproj -c Release

# Verify output
dir bin\Release\net8.0-windows\HuLoopBOT*.exe
```

### Method 3: Using Build Scripts

#### Debug Build
```cmd
Scripts\build-debug.bat
```

#### Release Build
```cmd
Scripts\build-release.bat
```

**Note:** Build scripts automatically clean previous builds and verify service files.

---

## ?? Publishing for Deployment

### Publishing Overview

| Publish Type | Use When | .NET Required | Package Size | Recommended |
|--------------|----------|---------------|--------------|-------------|
| **Framework-Dependent** | Target has .NET 8 installed | ? Yes | ~10-15 MB | Internal deployment |
| **Self-Contained** | Target may not have .NET | ? No | ~100-120 MB | ? Production/End-users |

---

## ?? Deployment Scenarios

### Scenario 1: Self-Contained Deployment (Recommended)

#### ? Advantages
- ? No .NET runtime required on target machine
- ? "Xcopy deployment" — just copy files
- ? Version isolation — no conflicts with other .NET apps
- ? Predictable behavior across machines

#### ? Disadvantages
- Larger package size (~100MB)
- Includes .NET runtime in each deployment

#### Build Command

```powershell
# Using PowerShell script (Recommended)
.\Scripts\Build-SelfContained.ps1

# Or manual publish
dotnet publish HuLoopBOT.csproj `
  -c Release `
    -r win-x64 `
    --self-contained true `
    -o "publish-standalone"
```

#### Or Using Batch Script

```cmd
Scripts\publish-standalone.bat
```

#### Output Structure

```
publish-standalone/
??? HuLoopBOT.exe       # Main application
??? HuLoopBOT_Service.exe       # Windows Service
??? *.dll       # All dependencies + .NET runtime
??? runtimeconfig.json files
```

---

### Scenario 2: Framework-Dependent Deployment

#### ? Advantages
- Smaller package size (~10-15MB)
- Uses system-installed .NET runtime
- Automatic security updates via Windows Update

#### ? Disadvantages
- Requires .NET 8 Runtime on target machine
- Potential version conflicts

#### Build Command

```powershell
# Using script
.\Scripts\publish.bat

# Or manual publish
dotnet publish HuLoopBOT.csproj `
    -c Release `
-r win-x64 `
    --self-contained false `
  -o "publish"
```

#### Output Structure

```
publish/
??? HuLoopBOT.exe   # Main application
??? HuLoopBOT_Service.exe  # Windows Service
??? *.dll# Application dependencies only
??? runtimeconfig.json files
```

**Note:** Target machine must have .NET 8 Runtime installed.

---

### Scenario 3: Development/Testing Build

For local development and testing:

```powershell
# Build in Release mode (no publish)
dotnet build -c Release

# Output directory
cd bin\Release\net8.0-windows\

# Install service locally
.\HuLoopBOT.exe  # Run GUI and use "Start Session Transfer"
```

---

## ?? Installation Procedures

### Installation Method 1: Using GUI (Recommended for End Users)

1. **Copy files to target location**
   ```powershell
   # Copy to target machine
   xcopy /E /I /Y publish-standalone "C:\Program Files\HuLoopBOT\"
   ```

2. **Run application as Administrator**
 ```powershell
   # Right-click > Run as administrator
   cd "C:\Program Files\HuLoopBOT"
   .\HuLoopBOT.exe
   ```

3. **Install and start service via GUI**
   - Click **"Start Session Transfer"** button
   - Service will be automatically installed and started
   - GUI shows status and logs

4. **Verify installation**
   - Check GUI status indicator (should show "Service Running")
   - Check Windows Services (services.msc)

---

### Installation Method 2: PowerShell Script (Automated)

#### For Self-Contained Deployment

```powershell
# 1. Copy deployment package to target
xcopy /E /I publish-standalone C:\Temp\HuLoopBOT

# 2. Run installation script as Administrator
cd C:\Temp\HuLoopBOT
.\Scripts\Install-Service-SelfContained.ps1
```

#### For Development Installation

```powershell
# From project root (as Administrator)
.\Scripts\install-service-dev.bat
```

**What it does:**
- ? Rebuilds the solution
- ? Stops existing service (if running)
- ? Removes old service registration
- ? Installs new service
- ? Configures registry settings
- ? Starts service with health checks

---

### Installation Method 3: Manual Command Line

#### Step-by-Step Manual Installation

```powershell
# Run as Administrator

# 1. Stop existing service (if any)
sc stop HuLoopBOT_RDP_Monitor

# 2. Delete existing service (if any)
sc delete HuLoopBOT_RDP_Monitor
Start-Sleep -Seconds 3

# 3. Install new service
sc create "HuLoopBOT_RDP_Monitor" `
    binPath= "C:\Program Files\HuLoopBOT\HuLoopBOT_Service.exe" `
    start= auto `
 DisplayName= "HuLoop BOT - RDP Session Monitor"

# 4. Set description
sc description "HuLoopBOT_RDP_Monitor" `
    "Monitors RDP sessions and automatically transfers them to console on disconnect"

# 5. Configure failure recovery
sc failure "HuLoopBOT_RDP_Monitor" `
    reset= 86400 `
    actions= restart/60000/restart/60000/restart/60000

# 6. Enable in registry
reg add "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled /t REG_DWORD /d 1 /f

# 7. Start service
sc start HuLoopBOT_RDP_Monitor

# 8. Verify status
sc query HuLoopBOT_RDP_Monitor
```

---

## ? Verification & Testing

### Post-Installation Verification

#### 1. Check Service Status

```powershell
# Query service status
sc query HuLoopBOT_RDP_Monitor

# Expected output:
# STATE : 4  RUNNING
# START_TYPE : 2AUTO_START
```

#### 2. Check Registry Configuration

```powershell
reg query "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled

# Expected: REG_DWORD 0x1
```

#### 3. Verify Log Files

```powershell
# View service logs
notepad "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log"
```

**Expected log entries:**
```
========================================
RDP Monitoring Service - OnStart Called
========================================
OnStart returning immediately - initialization will continue in background
...
? RDP Monitoring Service Started Successfully
========================================
```

#### 4. Check Event Viewer

```powershell
# Open Event Viewer
eventvwr.msc

# Navigate to: Windows Logs > Application
# Filter by Source: HuLoopBOT_RDP_Monitor
```

#### 5. Test RDP Monitoring

```powershell
# 1. Connect via RDP to the machine
mstsc /v:TARGET_MACHINE

# 2. Disconnect (don't log off)

# 3. Check logs for session transfer
type "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log"

# Should see: "RDP session disconnected - transferring to console..."
```

---

### Health Check Script

Save as `healthcheck.ps1`:

```powershell
# HuLoopBOT Health Check
Write-Host "HuLoopBOT Health Check" -ForegroundColor Cyan
Write-Host "=" * 50

# 1. Service Status
$service = Get-Service -Name "HuLoopBOT_RDP_Monitor" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "[?] Service exists" -ForegroundColor Green
    Write-Host " Status: $($service.Status)"
    Write-Host "    Start Type: $($service.StartType)"
} else {
    Write-Host "[?] Service NOT installed" -ForegroundColor Red
}

# 2. Service Executable
$exePath = "C:\Program Files\HuLoopBOT\HuLoopBOT_Service.exe"
if (Test-Path $exePath) {
  $version = (Get-Item $exePath).VersionInfo.FileVersion
  Write-Host "[?] Service executable exists" -ForegroundColor Green
    Write-Host "    Path: $exePath"
    Write-Host "    Version: $version"
} else {
    Write-Host "[?] Service executable NOT found" -ForegroundColor Red
}

# 3. Registry Settings
$regValue = Get-ItemProperty -Path "HKLM:\SOFTWARE\HuLoopBOT" -Name "RdpMonitoringEnabled" -ErrorAction SilentlyContinue
if ($regValue -and $regValue.RdpMonitoringEnabled -eq 1) {
 Write-Host "[?] Registry configured correctly" -ForegroundColor Green
} else {
    Write-Host "[?] Registry NOT configured or disabled" -ForegroundColor Red
}

# 4. Log Files
$logDir = "C:\ProgramData\HuLoopBOT\Logs"
if (Test-Path $logDir) {
    $logs = Get-ChildItem $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
    Write-Host "[?] Log directory exists" -ForegroundColor Green
    Write-Host "    Latest log: $($logs[0].Name)"
    Write-Host "    Last modified: $($logs[0].LastWriteTime)"
} else {
    Write-Host "[?] Log directory NOT found" -ForegroundColor Red
}

Write-Host "`nHealth check complete!" -ForegroundColor Cyan
```

---

## ?? Troubleshooting

### Common Issues and Solutions

#### Issue 1: Service Fails to Start (Error 1053)

**Symptoms:**
- Service returns "Error 1053: The service did not respond to the start or control request in a timely fashion"
- Service appears in Services but won't start

**Solutions:**

1. **Check service logs:**
   ```powershell
   type "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log"
   ```

2. **Verify files exist:**
   ```powershell
   dir "C:\Program Files\HuLoopBOT\HuLoopBOT_Service.exe"
   dir "C:\Program Files\HuLoopBOT\*.dll"
   ```

3. **Rebuild and redeploy:**
   ```powershell
   # Use quick fix script
   .\Scripts\Quick-Fix-Deploy.ps1
   ```

4. **Check Event Viewer for details:**
   ```powershell
   eventvwr.msc
 # Navigate to: Windows Logs > Application
   ```

---

#### Issue 2: Service Executable Not Found

**Symptoms:**
- Installation fails with "Service executable not found"
- Service binary missing from output directory

**Solutions:**

1. **Verify build targets in project:**
   - Ensure `HuLoopBOT.csproj` has custom build targets
   - Check that service project builds before main project

2. **Clean and rebuild:**
   ```powershell
   dotnet clean -c Release
   dotnet build HuLoopBOT.csproj -c Release
   ```

3. **Check output directory:**
   ```powershell
   dir bin\Release\net8.0-windows\HuLoopBOT_Service.exe
   ```

4. **If using publish, verify targets:**
   ```powershell
   dotnet publish HuLoopBOT.csproj -c Release --self-contained -r win-x64
   dir publish\HuLoopBOT_Service.exe
   ```

---

#### Issue 3: Registry Access Denied

**Symptoms:**
- "Access to the registry key is denied"
- Registry settings not applied

**Solutions:**

1. **Run as Administrator:**
   - Right-click PowerShell/CMD
   - Select "Run as Administrator"

2. **Manual registry setup:**
   ```powershell
   # Run as Administrator
   reg add "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled /t REG_DWORD /d 1 /f
   ```

---

#### Issue 4: EventLog Source Creation Fails

**Symptoms:**
- "The source was not found, but some or all event logs could not be searched"
- Event log warnings in service log

**Solutions:**

This is usually harmless (service works without event log). To fix:

```powershell
# Run as Administrator
New-EventLog -LogName Application -Source "HuLoopBOT"
New-EventLog -LogName Application -Source "HuLoopBOT_RDP_Monitor"

# Verify
[System.Diagnostics.EventLog]::SourceExists("HuLoopBOT")
[System.Diagnostics.EventLog]::SourceExists("HuLoopBOT_RDP_Monitor")
```

---

#### Issue 5: Missing Dependencies

**Symptoms:**
- Service crashes on startup
- "Could not load file or assembly" errors

**Solutions:**

1. **For Self-Contained:** Ensure you published with `--self-contained true`
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true
   ```

2. **For Framework-Dependent:** Install .NET 8 Runtime on target
   ```powershell
   # Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   # Or via winget:
   winget install Microsoft.DotNet.Runtime.8
   ```

3. **Verify all DLLs are present:**
   ```powershell
   dir "C:\Program Files\HuLoopBOT\*.dll"
   ```

---

### Diagnostic Tools

#### Run Full Diagnostics

```powershell
# From project Scripts folder
.\diagnose-service.bat
```

**Diagnostic script checks:**
- ? Service registration and status
- ? Registry configuration
- ? File existence and versions
- ? Recent log entries
- ? Event Viewer errors
- ? .NET Runtime version

#### View Logs Easily

```powershell
.\Scripts\view-logs.bat
```

---

## ?? Maintenance

### Updating the Application

#### Development Environment

```powershell
# 1. Make code changes
# 2. Build new version
dotnet build -c Release

# 3. Stop service
sc stop HuLoopBOT_RDP_Monitor

# 4. Reinstall
.\Scripts\install-service-dev.bat

# 5. Verify
sc query HuLoopBOT_RDP_Monitor
```

#### Production Environment

```powershell
# 1. Publish new version
dotnet publish -c Release -r win-x64 --self-contained true -o "publish-update"

# 2. On target machine (as Administrator):
# Stop service
sc stop HuLoopBOT_RDP_Monitor

# 3. Backup existing installation
xcopy /E /I "C:\Program Files\HuLoopBOT" "C:\Program Files\HuLoopBOT.backup"

# 4. Copy new files (overwrite)
xcopy /E /Y publish-update\* "C:\Program Files\HuLoopBOT\"

# 5. Start service
sc start HuLoopBOT_RDP_Monitor

# 6. Verify
sc query HuLoopBOT_RDP_Monitor
type "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log"
```

---

### Uninstalling

#### Using GUI

1. Run `HuLoopBOT.exe` as Administrator
2. Click "Stop Session Transfer"
3. Service will be stopped and uninstalled

#### Using Script

```powershell
.\Scripts\uninstall-service.bat
```

**Options:**
- Remove registry settings (Y/N)
- Remove log files (Y/N)

#### Manual Uninstall

```powershell
# 1. Stop service
sc stop HuLoopBOT_RDP_Monitor

# 2. Delete service
sc delete HuLoopBOT_RDP_Monitor

# 3. Remove registry (optional)
reg delete "HKLM\SOFTWARE\HuLoopBOT" /f

# 4. Remove files
Remove-Item -Path "C:\Program Files\HuLoopBOT" -Recurse -Force

# 5. Remove logs (optional)
Remove-Item -Path "C:\ProgramData\HuLoopBOT" -Recurse -Force
```

---

### Log Management

**Log Location:** `C:\ProgramData\HuLoopBOT\Logs\`

**Log Files:**
- `HuLoopBOT_Service_YYYY-MM-DD.log` — Daily service logs
- `HuLoopBOT_YYYY-MM-DD.log` — Daily GUI application logs

**Log Rotation:**
Logs are automatically created daily. Old logs are retained.

**Cleanup Old Logs:**
```powershell
# Remove logs older than 30 days
$logPath = "C:\ProgramData\HuLoopBOT\Logs"
Get-ChildItem $logPath -Filter "*.log" | 
Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
    Remove-Item -Force
```

---

## ?? Additional Resources

### Related Documentation

- [Scripts/README.md](Scripts/README.md) — Detailed script documentation
- [Scripts/DEPLOYMENT_FIX_ERROR_1053.md](Scripts/DEPLOYMENT_FIX_ERROR_1053.md) — Error 1053 troubleshooting
- [HuLoopBOT.csproj](HuLoopBOT.csproj) — Build targets and configuration
- [ServiceHost/HuLoopBOT_Service.csproj](ServiceHost/HuLoopBOT_Service.csproj) — Service project configuration

### Quick Reference: Build Commands

| Task | Command |
|------|---------|
| **Debug Build** | `dotnet build -c Debug` |
| **Release Build** | `dotnet build -c Release` |
| **Framework-Dependent Publish** | `dotnet publish -c Release -r win-x64` |
| **Self-Contained Publish** | `dotnet publish -c Release -r win-x64 --self-contained` |
| **Clean** | `dotnet clean` |
| **Restore** | `dotnet restore` |

### Quick Reference: Service Commands

| Task | Command |
|------|---------|
| **Install Service** | `sc create "HuLoopBOT_RDP_Monitor" binPath= "path\to\exe"` |
| **Start Service** | `sc start HuLoopBOT_RDP_Monitor` |
| **Stop Service** | `sc stop HuLoopBOT_RDP_Monitor` |
| **Query Status** | `sc query HuLoopBOT_RDP_Monitor` |
| **Delete Service** | `sc delete HuLoopBOT_RDP_Monitor` |

---

## ?? Best Practices

### ? DO

- ? **Use self-contained deployment** for production/end-users
- ? **Always run installation as Administrator**
- ? **Test on a clean VM** before deploying to production
- ? **Backup existing installation** before updating
- ? **Check logs immediately** after service starts
- ? **Use automated scripts** for consistent deployment
- ? **Verify all files** after publish/copy operations

### ? DON'T

- ? **Don't modify files** while service is running
- ? **Don't use development scripts** on published apps
- ? **Don't skip verification steps** after deployment
- ? **Don't delete logs** before troubleshooting issues
- ? **Don't deploy without testing** the service starts successfully

---

## ?? Support Checklist

When reporting issues, provide:

1. ? Service status: `sc query HuLoopBOT_RDP_Monitor`
2. ? Recent service logs
3. ? Event Viewer Application logs
4. ? Deployment method used (self-contained vs framework-dependent)
5. ? Windows version and edition
6. ? .NET version (if framework-dependent): `dotnet --list-runtimes`
7. ? Installation path and file listing
8. ? Registry settings: `reg query "HKLM\SOFTWARE\HuLoopBOT"`

---

## ?? Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024 | Initial deployment guide |

---

## ? Success Indicators

After successful deployment, you should see:

- ? Service shows **RUNNING** status in Services console
- ? Service starts within **2-5 seconds**
- ? Log file shows **"Service Started Successfully"**
- ? No **Error 1053** in Event Viewer
- ? GUI application can **connect to service**
- ? **RDP disconnect triggers** session transfer correctly

---

**End of Deployment Guide**

For questions or issues, refer to the diagnostic scripts and log files.
