@echo off
REM Uninstall HuLoopBOT Windows Service
REM Works for both published and development installations

echo ========================================
echo HuLoopBOT Service Uninstallation
echo ========================================
echo.

REM Check admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Must run as Administrator!
  echo.
    echo Right-click this file and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

echo [OK] Running as Administrator
echo.

REM Check if service exists
echo Step 1: Checking if service exists...
sc query HuLoopBOT_RDP_Monitor >nul 2>&1
if %errorLevel% neq 0 (
    echo [INFO] Service not found
    echo The service is not installed or has already been removed.
    echo.
    goto CLEANUP_REGISTRY
)

echo [OK] Service found
echo.

REM Get service status
for /f "tokens=3" %%i in ('sc query HuLoopBOT_RDP_Monitor ^| findstr "STATE"') do set SERVICE_STATE=%%i
echo Current service status: %SERVICE_STATE%
echo.

REM Stop service if running
if /i "%SERVICE_STATE%"=="RUNNING" (
    echo Step 2: Stopping service...
    sc stop HuLoopBOT_RDP_Monitor >nul 2>&1
    if %errorLevel% equ 0 (
 echo Service stop command sent
        echo Waiting for service to stop...
        timeout /t 3 /nobreak >nul
 
      REM Verify it stopped
        sc query HuLoopBOT_RDP_Monitor | findstr "STOPPED" >nul 2>&1
        if %errorLevel% equ 0 (
  echo [OK] Service stopped
        ) else (
            echo [WARNING] Service may still be stopping
        )
    ) else (
        echo [WARNING] Failed to stop service (may already be stopped)
    )
    echo.
) else if /i "%SERVICE_STATE%"=="STOPPED" (
    echo Step 2: Service already stopped
    echo.
) else (
    echo Step 2: Service in state: %SERVICE_STATE%
 echo Attempting to stop anyway...
    sc stop HuLoopBOT_RDP_Monitor >nul 2>&1
    timeout /t 2 /nobreak >nul
    echo.
)

REM Delete service
echo Step 3: Deleting service...
sc delete HuLoopBOT_RDP_Monitor >nul 2>&1
set DELETE_RESULT=%errorLevel%

if %DELETE_RESULT% equ 0 (
    echo [OK] Service deleted successfully
    echo.
    timeout /t 2 /nobreak >nul
) else if %DELETE_RESULT% equ 1072 (
    echo [OK] Service marked for deletion
    echo The service will be removed after reboot
echo.
) else (
    echo [WARNING] Delete command returned code %DELETE_RESULT%
    echo The service may already be marked for deletion
    echo.
)

REM Verify deletion
echo Verifying service removal...
timeout /t 1 /nobreak >nul
sc query HuLoopBOT_RDP_Monitor >nul 2>&1
if %errorLevel% equ 0 (
    echo [INFO] Service still listed (will be removed after reboot)
) else (
    echo [OK] Service no longer listed
)
echo.

:CLEANUP_REGISTRY
echo Step 4: Do you want to remove registry settings?
echo.
echo This will remove:
echo - HKLM\SOFTWARE\HuLoopBOT\RdpMonitoringEnabled
echo.
echo The main configuration (users, settings) will NOT be removed.
echo.
choice /C YN /M "Remove RdpMonitoringEnabled registry key"
if %errorLevel% equ 1 (
    echo.
    echo Removing registry key...
    reg delete "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled /f >nul 2>&1
    if %errorLevel% equ 0 (
  echo [OK] Registry key removed
    ) else (
        echo [INFO] Registry key not found or already removed
    )
) else (
    echo.
    echo [INFO] Registry key kept (service will remain disabled)
)
echo.

echo Step 5: Do you want to remove log files?
echo.
echo This will remove:
echo - C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log
echo.
choice /C YN /M "Remove service log files"
if %errorLevel% equ 1 (
 echo.
    echo Removing log files...
    if exist "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" (
        del /Q "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" 2>nul
      if %errorLevel% equ 0 (
            echo [OK] Log files removed
      ) else (
          echo [WARNING] Some log files could not be removed
    )
    ) else (
        echo [INFO] No service log files found
    )
) else (
    echo.
    echo [INFO] Log files kept
)
echo.

echo ========================================
echo Uninstallation Complete
echo ========================================
echo.
echo The HuLoopBOT service has been uninstalled.
echo.
echo If the service was marked for deletion, it will be
echo fully removed after the next system reboot.
echo.
echo To verify removal, run:
echo   sc query HuLoopBOT_RDP_Monitor
echo.
echo If you see "service does not exist", it's fully removed.
echo If you see "marked for deletion", reboot to complete removal.
echo.
pause
