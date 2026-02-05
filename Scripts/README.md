# HuLoopBOT Scripts

This folder contains utility scripts for building, publishing, and managing the HuLoopBOT application and Windows Service.

## ?? Script Categories

### ?? Build Scripts

| Script | Purpose | When to Use |
|--------|---------|-------------|
| `build-debug.bat` | Build Debug configuration | During development |
| `build-release.bat` | Build Release configuration | Before publishing |

### ?? Publish Scripts

| Script | Purpose | Output | Requirements |
|--------|---------|--------|--------------|
| `publish.bat` | Framework-dependent publish | `publish/` | Requires .NET 8 Runtime on target |
| `publish-standalone.bat` | Self-contained publish | `publish-standalone/` | No .NET required (~100MB larger) |

### ?? Service Management Scripts

| Script | Purpose | Run From |
|--------|---------|----------|
| `install-service-dev.bat` | Install service (development) | Project root or Scripts folder |
| `install-service-published.bat` | Install service (published app) | Published application folder |
| `uninstall-service.bat` | Uninstall service | Anywhere (as admin) |

### ?? Diagnostic Scripts

| Script | Purpose |
|--------|---------|
| `diagnose-service.bat` | Comprehensive service diagnostics |
| `view-logs.bat` | View service log files |

---

## ?? Quick Start

### For Development

```cmd
# 1. Build the application
Scripts\build-debug.bat

# 2. Install and start the service
Scripts\install-service-dev.bat
```

### For Production Deployment

```cmd
# 1. Publish the application
Scripts\publish.bat

# 2. Copy publish folder to target machine

# 3. Copy install-service-published.bat to published folder

# 4. Run from published folder as admin
install-service-published.bat
```

---

## ?? Detailed Usage

### Build Scripts

#### build-debug.bat
```cmd
cd D:\HuLoop\Development\HuLoopBOT_Full_Project
Scripts\build-debug.bat
```

**What it does:**
- Cleans previous builds
- Builds solution in Debug configuration
- Verifies service files were copied
- Output: `bin\Debug\net8.0-windows\`

#### build-release.bat
```cmd
cd D:\HuLoop\Development\HuLoopBOT_Full_Project
Scripts\build-release.bat
```

**What it does:**
- Cleans previous builds
- Builds solution in Release configuration
- Optimized for performance
- Output: `bin\Release\net8.0-windows\`

---

### Publish Scripts

#### publish.bat (Framework-dependent)
```cmd
cd D:\HuLoop\Development\HuLoopBOT_Full_Project
Scripts\publish.bat
```

**What it does:**
- Creates framework-dependent deployment
- Smaller file size (~5-10MB)
- **Requires .NET 8 Runtime** on target machine
- Output: `publish\`

**When to use:**
- Target machines have .NET 8 installed
- You want smaller deployment size
- Internal deployment where you control the environment

#### publish-standalone.bat (Self-contained)
```cmd
cd D:\HuLoop\Development\HuLoopBOT_Full_Project
Scripts\publish-standalone.bat
```

**What it does:**
- Creates self-contained deployment
- Includes .NET runtime (~100MB)
- **No .NET required** on target machine
- Output: `publish-standalone\`

**When to use:**
- Target machines may not have .NET 8
- You want "xcopy deployment" (just copy and run)
- Distributing to end users

---

### Service Management Scripts

#### install-service-dev.bat (Development)

**Usage:**
```cmd
# From project root
Scripts\install-service-dev.bat

# Or from Scripts folder
cd Scripts
install-service-dev.bat
```

**What it does:**
1. Checks for admin rights
2. Stops and removes old service
3. **Rebuilds the solution**
4. Verifies service executable
5. Configures registry
6. Installs service
7. Starts service with retries

**Requirements:**
- Run as Administrator
- Run from project root or Scripts folder
- Source code must be available

---

#### install-service-published.bat (Production)

**Usage:**
```cmd
# 1. Publish the app
Scripts\publish.bat

# 2. Copy script to published folder
copy Scripts\install-service-published.bat publish\

# 3. Run from published folder as admin
cd publish
install-service-published.bat
```

**What it does:**
1. Checks for admin rights
2. Finds service executable in **current directory**
3. Stops and removes old service
4. Installs new service
5. Configures failure recovery
6. Starts service

**Requirements:**
- Run as Administrator
- **Must be in the same folder** as HuLoopBOT_Service.exe
- Does NOT rebuild (uses existing files)

**Important:** This script uses `%~dp0` to find the current directory, so it always looks for the service exe in the folder where the script is located.

---

#### uninstall-service.bat

**Usage:**
```cmd
# Can run from anywhere
Scripts\uninstall-service.bat
```

**What it does:**
1. Stops the service
2. Deletes the service registration
3. Optionally removes registry keys
4. Optionally removes log files

**Interactive prompts:**
- Remove registry settings? (Y/N)
- Remove log files? (Y/N)

---

### Diagnostic Scripts

#### diagnose-service.bat

**Usage:**
```cmd
Scripts\diagnose-service.bat
```

**Gathers:**
1. ? Service status and configuration
2. ? Registry settings
3. ? Service executable location and validation
4. ? Recent log files
5. ? Event Viewer entries
6. ? .NET Runtime version

**Provides:**
- Summary of issues
- Troubleshooting suggestions
- Quick fix commands

---

#### view-logs.bat

**Usage:**
```cmd
Scripts\view-logs.bat
```

**What it does:**
1. Checks if logs directory exists
2. Lists all log files
3. Finds most recent service log
4. Offers to open in Notepad
5. Offers to open logs folder in Explorer

**Log Location:**
```
C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log
```

---

## ?? Common Workflows

### Development Workflow

```cmd
# 1. Make code changes

# 2. Build
Scripts\build-debug.bat

# 3. Reinstall service with new build
Scripts\uninstall-service.bat
Scripts\install-service-dev.bat

# 4. Check logs
Scripts\view-logs.bat
```

### Deployment Workflow

```cmd
# 1. Build and test locally
Scripts\build-release.bat
Scripts\install-service-dev.bat

# 2. Publish
Scripts\publish.bat

# 3. Copy published files to target
xcopy /E /I publish \\TargetMachine\C$\Program Files\HuLoopBOT\

# 4. Copy install script
copy Scripts\install-service-published.bat "\\TargetMachine\C$\Program Files\HuLoopBOT\"

# 5. On target machine (as admin)
cd "C:\Program Files\HuLoopBOT"
install-service-published.bat
```

### Troubleshooting Workflow

```cmd
# 1. Run diagnostics
Scripts\diagnose-service.bat

# 2. View logs
Scripts\view-logs.bat

# 3. Check Event Viewer
eventvwr.msc

# 4. Reinstall if needed
Scripts\uninstall-service.bat
Scripts\install-service-dev.bat
```

---

## ?? Best Practices

### ? DO

- **Always run service installation scripts as Administrator**
- **Use `install-service-dev.bat` during development**
- **Use `install-service-published.bat` for production deployments**
- **Run `diagnose-service.bat` when troubleshooting**
- **Check logs after service installation**

### ? DON'T

- **Don't run `install-service-dev.bat` on a published app** (it will try to rebuild)
- **Don't run `install-service-published.bat` from project root** (wrong path)
- **Don't move service files after installation** (service won't start)
- **Don't forget to copy the install script to published folder**

---

## ?? Troubleshooting

### Script says "Service executable not found"

**For Development (install-service-dev.bat):**
- Make sure you're in the project root
- Check that the build succeeded
- Verify `bin\Debug\net8.0-windows\HuLoopBOT_Service.exe` exists

**For Published (install-service-published.bat):**
- Make sure the script is in the **same folder** as HuLoopBOT_Service.exe
- Check that you published (not just built)
- Verify HuLoopBOT_Service.exe exists in the folder

### Service won't start (Error 1053)

Run diagnostics:
```cmd
Scripts\diagnose-service.bat
Scripts\view-logs.bat
```

Common causes:
1. Service OnStart() taking too long (timeout)
2. Missing dependencies
3. Service crashing during startup
4. Registry disabled (RdpMonitoringEnabled = 0)

### "Must run as Administrator"

All service management scripts require admin rights:
- Right-click the script
- Select "Run as administrator"

Or run cmd as admin first:
```cmd
# Run as admin
cd D:\HuLoop\Development\HuLoopBOT_Full_Project
Scripts\install-service-dev.bat
```

---

## ?? Script Exit Codes

| Exit Code | Meaning |
|-----------|---------|
| 0 | Success |
| 1 | General error |
| 1053 | Service did not respond to start/control request |
| 1056 | Service is already running |
| 1060 | Service does not exist |
| 1072 | Service marked for deletion (reboot required) |

---

## ?? Related Documentation

- [SERVICE_INSTALLATION_GUIDE.md](../SERVICE_INSTALLATION_GUIDE.md) - Comprehensive service installation guide
- [HuLoopBOT.csproj](../HuLoopBOT.csproj) - Build targets and file copying logic
- [ServiceHost/HuLoopBOT_Service.csproj](../ServiceHost/HuLoopBOT_Service.csproj) - Service project configuration

---

## ?? Tips

1. **Scripts work from any location:** The scripts automatically detect their location and navigate to the project root when needed.

2. **Safe to run multiple times:** Install scripts check for existing services and handle reinstallation gracefully.

3. **View real-time logs:** After starting the service, immediately run `view-logs.bat` to see initialization messages.

4. **Publish then copy:** When deploying, publish locally first, then copy the published folder to the target machine. Don't forget to copy the install script too!

5. **Check diagnostics first:** Before asking for help, run `diagnose-service.bat` - it gathers all the information you need to troubleshoot.

---

## ?? Support

If you encounter issues:

1. Run `Scripts\diagnose-service.bat`
2. Run `Scripts\view-logs.bat`
3. Check Event Viewer: `eventvwr.msc` ? Application ? HuLoopBOT_RDP_Monitor
4. Review [SERVICE_INSTALLATION_GUIDE.md](../SERVICE_INSTALLATION_GUIDE.md)
