@echo off
REM Service Installation for PUBLISHED Application
REM This script installs the service from the published/deployed location
REM Usage: Copy this file to the published application directory and run as Admin

echo ========================================
echo HuLoopBOT Service Installation
echo (For Published Application)
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

REM Get the directory where this batch file is located
set APP_DIR=%~dp0
REM Remove trailing backslash
if "%APP_DIR:~-1%"=="\" set APP_DIR=%APP_DIR:~0,-1%

echo Application Directory: %APP_DIR%
echo.

REM Stop and remove old service
echo Step 1: Removing existing service (if any)...
sc query HuLoopBOT_RDP_Monitor >nul 2>&1
if %errorLevel% equ 0 (
    echo Service found, stopping...
    sc stop HuLoopBOT_RDP_Monitor >nul 2>&1
    timeout /t 2 /nobreak >nul
    
    echo Deleting service...
    sc delete HuLoopBOT_RDP_Monitor >nul 2>&1
    timeout /t 2 /nobreak >nul
    echo [OK] Old service removed
) else (
    echo [OK] No existing service found
)
echo.

REM Verify service executable exists
echo Step 2: Verifying service executable...
set SERVICE_EXE=%APP_DIR%\HuLoopBOT_Service.exe

if not exist "%SERVICE_EXE%" (
 echo [ERROR] Service executable not found!
    echo.
    echo Expected location: %SERVICE_EXE%
    echo.
    echo Files in current directory:
    dir "%APP_DIR%\*.exe" /b 2>nul
    echo.
    echo Make sure you:
    echo 1. Published the application (not just built it)
    echo 2. Copied this script to the published application folder
    echo 3. The HuLoopBOT_Service.exe is in the same folder
    echo.
    pause
    exit /b 1
)

echo [OK] Service executable found: %SERVICE_EXE%
echo.

REM Verify main application exists too
if not exist "%APP_DIR%\HuLoopBOT.exe" (
    echo [WARNING] Main application (HuLoopBOT.exe) not found in same directory
 echo This may indicate incorrect deployment
    echo.
)

REM Enable in registry
echo Step 3: Configuring registry...
reg add "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled /t REG_DWORD /d 1 /f >nul 2>&1
if %errorLevel% equ 0 (
    echo [OK] Registry configured (RdpMonitoringEnabled = 1)
) else (
  echo [WARNING] Failed to set registry key
)
echo.

REM Install service
echo Step 4: Installing Windows Service...
sc create HuLoopBOT_RDP_Monitor binPath= "\"%SERVICE_EXE%\"" start= auto DisplayName= "HuLoop BOT - RDP Session Monitor"
if %errorLevel% neq 0 (
 echo [ERROR] Service installation failed!
    echo.
    echo Common causes:
    echo - Service already exists and wasn't fully removed
    echo - Insufficient permissions
    echo - Path contains invalid characters
    echo.
    pause
    exit /b 1
)
echo [OK] Service created successfully
echo.

REM Set description
echo Step 5: Setting service description...
sc description HuLoopBOT_RDP_Monitor "Monitors RDP sessions and automatically transfers them to console on disconnect" >nul
echo [OK] Description set
echo.

REM Configure failure recovery
echo Step 6: Configuring failure recovery...
sc failure HuLoopBOT_RDP_Monitor reset= 86400 actions= restart/5000/restart/10000/restart/30000 >nul
echo [OK] Failure recovery configured
echo     - Reset counter after 24 hours
echo     - Restart after 5s, 10s, 30s on consecutive failures
echo.

REM Wait for Windows to fully register the service
echo Step 7: Waiting for service registration...
echo (This delay prevents Error 1053)
timeout /t 5 /nobreak >nul
echo [OK] Service registration complete
echo.

REM Verify service is accessible
echo Step 8: Verifying service accessibility...
sc query HuLoopBOT_RDP_Monitor >nul 2>&1
if %errorLevel% neq 0 (
    echo [WARNING] Service not immediately accessible, waiting longer...
    timeout /t 3 /nobreak >nul
    sc query HuLoopBOT_RDP_Monitor >nul 2>&1
    if %errorLevel% neq 0 (
        echo [ERROR] Service still not accessible!
  echo.
      pause
        exit /b 1
    )
)
echo [OK] Service is accessible
echo.

REM Start service with retries
echo Step 9: Starting service...
echo.

set MAX_ATTEMPTS=3
set ATTEMPT=1

:START_RETRY
echo Attempt %ATTEMPT% of %MAX_ATTEMPTS%...
sc start HuLoopBOT_RDP_Monitor >nul 2>&1
set START_RESULT=%errorLevel%

if %START_RESULT% equ 0 goto START_SUCCESS

echo Start command returned error code %START_RESULT%

if %ATTEMPT% lss %MAX_ATTEMPTS% (
    set /a WAIT_TIME=%ATTEMPT%*2
    echo Waiting %WAIT_TIME% seconds before retry...
 timeout /t %WAIT_TIME% /nobreak >nul
    set /a ATTEMPT+=1
    goto START_RETRY
)

echo.
echo [ERROR] Failed to start service after %MAX_ATTEMPTS% attempts!
echo Error code: %START_RESULT%
echo.

if %START_RESULT% equ 1053 (
    echo This is Error 1053: Service did not respond in time
    echo.
    echo Common causes:
    echo 1. Service OnStart taking too long
echo 2. Service has blocking operation in startup
    echo 3. Dependencies not ready
    echo 4. Service crashed during startup
    echo.
)

if %START_RESULT% equ 1056 (
    echo Service is already running
    goto CHECK_STATUS
)

echo Check service log file:
echo C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log
echo.
echo Check Event Viewer:
echo - Run: eventvwr.msc
echo - Windows Logs -^> Application
echo - Filter by Source: HuLoopBOT_RDP_Monitor
echo.
goto CHECK_STATUS

:START_SUCCESS
echo [OK] Start command sent successfully
echo.
echo Waiting for service to initialize...

REM Give service time to start
timeout /t 3 /nobreak >nul

REM Check status with retries
set CHECK_COUNT=0
:CHECK_LOOP
sc query HuLoopBOT_RDP_Monitor | findstr "RUNNING" >nul 2>&1
if %errorLevel% equ 0 goto SERVICE_RUNNING

set /a CHECK_COUNT+=1
if %CHECK_COUNT% geq 10 goto SERVICE_NOT_RUNNING

echo Checking status... (attempt %CHECK_COUNT%/10)
timeout /t 1 /nobreak >nul
goto CHECK_LOOP

:SERVICE_RUNNING
echo.
echo ========================================
echo SUCCESS! Service is RUNNING
echo ========================================
echo.
sc query HuLoopBOT_RDP_Monitor
echo.
echo Installation complete!
echo.
echo Service Details:
echo - Name: HuLoopBOT_RDP_Monitor
echo - Path: %SERVICE_EXE%
echo - Status: Running
echo - Auto-start: Enabled
echo.
echo Log files will be created at:
echo C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log
echo.
echo ========================================
pause
exit /b 0

:SERVICE_NOT_RUNNING
echo.
echo [WARNING] Service did not reach RUNNING state within 10 seconds
echo.

:CHECK_STATUS
sc query HuLoopBOT_RDP_Monitor
echo.
echo This may be normal if:
echo - Service is disabled in registry (RdpMonitoringEnabled = 0)
echo - Service is starting slowly
echo - Service initialization is waiting for resources
echo.
echo To check status later, run:
echo   sc query HuLoopBOT_RDP_Monitor
echo.
echo To view logs:
echo   C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log
echo.
echo To check Event Viewer:
echo   eventvwr.msc
echo   Navigate to: Windows Logs -^> Application
echo   Filter by Source: HuLoopBOT_RDP_Monitor
echo.
echo ========================================
echo Installation Complete (Status Unknown)
echo ========================================
pause
exit /b 0
