# Fix for Error 1053 - Deployment Guide

## What Was Fixed

### Issue #1: Service Timeout (Error 1053)
The service's `OnStart()` method was waiting up to 3 seconds for initialization, causing the Windows Service Control Manager to timeout (default 30 seconds).

**Fix:** `OnStart()` now returns immediately and all initialization happens in the background.

### Issue #2: Missing Assembly
The `EventLog.CreateEventSource()` method was trying to load `System.Threading.AccessControl` assembly which wasn't referenced.

**Fix:** Added `System.Threading.AccessControl` NuGet package to both projects.

### Issue #3: EventLog Admin Permissions
EventLog source creation was failing when not running as administrator.

**Fix:** Added admin check before attempting to create event log sources.

---

## Files Changed

1. **Services/RdpMonitoringService.cs**
   - `OnStart()`: Returns immediately without waiting
   - `InitializeService()`: Added 100ms delay at start
   - `StartService()`: Increased timeout to 60s, added 2s post-start wait

2. **Utilities/Logger.cs**
   - Added admin check in `EnsureMainEventSource()`
   - Added admin check in `EnsureRdpEventSource()`

3. **HuLoopBOT.csproj**
   - Added `System.Threading.AccessControl` package

4. **ServiceHost/HuLoopBOT_Service.csproj**
   - Added `System.Threading.AccessControl` package

---

## Deployment Steps

### Step 1: Build the Solution

```powershell
# In Visual Studio
Build > Rebuild Solution

# Or via command line
cd D:\HuLoop\Development\HuLoopBOT_Full_Project
dotnet restore
dotnet build --configuration Release
```

### Step 2: Publish the Application

```powershell
# Using PowerShell script (recommended)
.\Scripts\Build-SelfContained.ps1

# Or manual publish
dotnet publish -c Release -r win-x64 --self-contained true -o "D:\HuLoop\Publish\HuLoopBOT"
```

### Step 3: Stop and Remove Old Service

On the target machine (run as Administrator):

```powershell
# Stop the service if running
sc stop HuLoopBOT_RDP_Monitor

# Delete the service
sc delete HuLoopBOT_RDP_Monitor

# Wait for deletion to complete
timeout /t 3
```

### Step 4: Copy New Files to Target Machine

```powershell
# Copy to target machine
xcopy /E /Y "D:\HuLoop\Publish\HuLoopBOT\*" "C:\Rajiv\HuLoopBOT\"

# Or if deploying to remote machine
xcopy /E /Y "D:\HuLoop\Publish\HuLoopBOT\*" "\\TargetMachine\C$\Rajiv\HuLoopBOT\"
```

### Step 5: Verify Service Files Exist

On target machine:

```powershell
# Check that service executable exists
dir C:\Rajiv\HuLoopBOT\HuLoopBOT_Service.exe

# Expected output should show the file with timestamp
```

### Step 6: Install and Start Service

#### Option A: Using HuLoopBOT GUI (Recommended)

1. Run `C:\Rajiv\HuLoopBOT\HuLoopBOT.exe` as Administrator
2. Click **"Start Session Transfer"**
3. The service will be installed and started automatically

#### Option B: Using Command Line

```powershell
# Install service
sc create "HuLoopBOT_RDP_Monitor" binPath= "\"C:\Rajiv\HuLoopBOT\HuLoopBOT_Service.exe\"" start= auto DisplayName= "HuLoop BOT - RDP Session Monitor"

# Enable in registry
reg add "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled /t REG_DWORD /d 1 /f

# Start service
sc start HuLoopBOT_RDP_Monitor

# Check status
sc query HuLoopBOT_RDP_Monitor
```

### Step 7: Verify Service is Running

```powershell
# Check service status
sc query HuLoopBOT_RDP_Monitor

# Expected output:
# STATE : 4  RUNNING

# Check logs
type "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log"
```

---

## Expected Log Output

After successful deployment, you should see:

```
========================================
RDP Monitoring Service - OnStart Called
========================================
OnStart returning immediately - initialization will continue in background
This prevents Error 1053 timeout issues
========================================
Service Initialization Started
========================================
Starting initialization after OnStart return...
Step 1: Checking registry configuration...
? Registry check passed - Service is enabled
Step 2: Creating RDP Session Monitor...
? RDP Session Monitor created successfully
? RDP monitor started
========================================
??? RDP Monitoring Service Started Successfully ???
========================================
```

---

## Troubleshooting

### If Service Still Fails to Start

1. **Check Event Viewer**
   ```powershell
   eventvwr.msc
   ```
   - Navigate to: Windows Logs > Application
   - Filter by Source: `HuLoopBOT_RDP_Monitor`

2. **Check Service Log File**
   ```powershell
   # Open in Notepad
   notepad "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log"
   
   # Or view recent logs
   type "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" | more
   ```

3. **Verify Registry Setting**
   ```powershell
   reg query "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled
   
   # Should show: REG_DWORD 0x1
   ```

4. **Test Service Manually**
   ```powershell
   # Try starting service with verbose output
   sc start HuLoopBOT_RDP_Monitor
   
   # Wait a few seconds
   timeout /t 5
   
   # Check status
   sc query HuLoopBOT_RDP_Monitor
   ```

### If EventLog Errors Persist

The EventLog source creation errors are now handled gracefully. If you still see them:

1. **Create Event Sources Manually** (Run as Administrator):
   ```powershell
   # Using PowerShell
   New-EventLog -LogName Application -Source "HuLoopBOT"
   New-EventLog -LogName Application -Source "HuLoopBOT_RDP_Monitor"
   ```

2. **Verify Event Sources**:
   ```powershell
   [System.Diagnostics.EventLog]::SourceExists("HuLoopBOT")
   [System.Diagnostics.EventLog]::SourceExists("HuLoopBOT_RDP_Monitor")
   
   # Both should return: True
   ```

### Common Errors and Solutions

| Error | Solution |
|-------|----------|
| "Service executable not found" | Verify `HuLoopBOT_Service.exe` exists in the application directory |
| "Access denied" | Run command prompt or GUI as Administrator |
| "Service is marked for deletion" | Reboot the machine or wait a few minutes |
| "Registry key not found" | Run: `reg add "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled /t REG_DWORD /d 1 /f` |

---

## Quick Test After Deployment

```powershell
# 1. Check service is installed
sc query HuLoopBOT_RDP_Monitor

# 2. Check if running
sc query HuLoopBOT_RDP_Monitor | findstr STATE

# 3. View recent logs
type "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log"

# 4. Test RDP disconnect (connect via RDP and disconnect)
# Service should automatically transfer session to console
```

---

## Rollback Plan

If you need to revert to the old version:

```powershell
# 1. Stop and remove service
sc stop HuLoopBOT_RDP_Monitor
sc delete HuLoopBOT_RDP_Monitor

# 2. Restore old files
xcopy /E /Y "C:\Rajiv\HuLoopBOT_Backup\*" "C:\Rajiv\HuLoopBOT\"

# 3. Reinstall old service
C:\Rajiv\HuLoopBOT\HuLoopBOT.exe
# (Use GUI to reinstall)
```

---

## Success Indicators

? Service installs without errors  
? Service starts within 5-10 seconds  
? Service status shows "RUNNING"  
? Log file shows "Service Started Successfully"  
? No Error 1053 in Event Viewer  
? RDP disconnect triggers session transfer  

---

## Additional Notes

- **Service now starts in under 2 seconds** instead of timing out
- **EventLog errors are handled gracefully** - service will work even if EventLog creation fails
- **All initialization happens asynchronously** - OnStart() returns immediately
- **Improved logging** shows exactly where the service is in the startup process

---

## Support

If issues persist after following this guide:

1. Check the service log file for detailed error messages
2. Check Windows Event Viewer for system-level errors
3. Verify all dependencies are present in the application directory
4. Ensure you're running as Administrator
5. Try a clean reinstall (uninstall, reboot, reinstall)
