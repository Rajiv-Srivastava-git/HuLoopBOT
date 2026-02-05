@echo off
REM Service Installation with Proper Timing - Fixes Error 1053
REM This script is for DEVELOPMENT BUILDS (not published apps)
REM For published apps, use install-service-published.bat instead

echo ========================================
echo HuLoopBOT Service Installation Fix
echo (Development Build)
echo ========================================
echo.

REM Check admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Must run as Administrator!
    pause
    exit /b 1
)

echo [OK] Running as Administrator
echo.

REM Get the project root directory (parent of Scripts folder)
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
cd /d "%PROJECT_ROOT%"

echo Project Root: %CD%
echo.

REM Stop and remove old service
echo Step 1: Removing existing service (if any)...
sc stop HuLoopBOT_RDP_Monitor >nul 2>&1
timeout /t 2 /nobreak >nul
sc delete HuLoopBOT_RDP_Monitor >nul 2>&1
timeout /t 2 /nobreak >nul
echo [OK] Old service removed
echo.

REM Rebuild
echo Step 2: Rebuilding solution...
dotnet build -c Debug -v minimal
if %errorLevel% neq 0 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)
echo [OK] Build successful
echo.

REM Verify files - Service exe should be in the MAIN project output, not ServiceHost subfolder
echo Step 3: Verifying service executable...
set SERVICE_EXE=%CD%\bin\Debug\net8.0-windows\HuLoopBOT_Service.exe

if not exist "%SERVICE_EXE%" (
    echo [ERROR] Service executable not found!
    echo.
    echo Expected location: %SERVICE_EXE%
    echo.
    echo The build process should copy HuLoopBOT_Service.exe to the main output directory.
    echo.
echo Files in output directory:
    dir "%CD%\bin\Debug\net8.0-windows\*.exe" /b 2>nul
    echo.
    echo Check build output for copy errors.
  pause
    exit /b 1
)

echo [OK] Service executable found
echo     Location: %SERVICE_EXE%
echo.

REM Enable in registry
echo Step 4: Configuring registry...
reg add "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled /t REG_DWORD /d 1 /f >nul
echo [OK] Registry configured
echo.

REM Install service
echo Step 5: Installing service...
sc create HuLoopBOT_RDP_Monitor binPath= "\"%SERVICE_EXE%\"" start= auto DisplayName= "HuLoop BOT - RDP Session Monitor"
if %errorLevel% neq 0 (
    echo [ERROR] Service installation failed!
    pause
    exit /b 1
)
echo [OK] Service created
echo.

REM Set description
echo Setting service description...
sc description HuLoopBOT_RDP_Monitor "Monitors RDP sessions and automatically transfers them to console on disconnect" >nul
echo.

REM Configure failure recovery
echo Configuring failure recovery...
sc failure HuLoopBOT_RDP_Monitor reset= 86400 actions= restart/5000/restart/10000/restart/30000 >nul
echo.

REM CRITICAL: Wait for Windows to fully register the service
echo Step 6: Waiting for service registration...
echo (This delay prevents Error 1053)
timeout /t 5 /nobreak >nul
echo [OK] Service registration complete
echo.

REM Verify service is accessible
echo Verifying service accessibility...
sc query HuLoopBOT_RDP_Monitor >nul 2>&1
if %errorLevel% neq 0 (
    echo [WARNING] Service not immediately accessible, waiting longer...
    timeout /t 3 /nobreak >nul
)
echo [OK] Service is accessible
echo.

REM Start service with retries
echo Step 7: Starting service...
echo.
echo Attempt 1 of 3...
sc start HuLoopBOT_RDP_Monitor >nul 2>&1
set START_RESULT=%errorLevel%

if %START_RESULT% neq 0 (
    echo First attempt failed (code %START_RESULT%), waiting and retrying...
    timeout /t 3 /nobreak >nul
    
    echo Attempt 2 of 3...
    sc start HuLoopBOT_RDP_Monitor >nul 2>&1
    set START_RESULT=%errorLevel%
    
    if %START_RESULT% neq 0 (
  echo Second attempt failed (code %START_RESULT%), waiting longer and retrying...
        timeout /t 5 /nobreak >nul
        
echo Attempt 3 of 3...
      sc start HuLoopBOT_RDP_Monitor >nul 2>&1
        set START_RESULT=%errorLevel%
    )
)

echo.

if %START_RESULT% equ 0 (
    echo [OK] Start command sent
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
    
    echo Checking... (attempt %CHECK_COUNT%/10)
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
    echo Service Details:
    echo - Executable: %SERVICE_EXE%
    echo - Status: Running
    echo.
    echo Service logs:
    if exist "C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log" (
        echo C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log
    ) else (
        echo (No logs created yet)
 )
    echo.
    goto END_SUCCESS
    
    :SERVICE_NOT_RUNNING
    echo.
    echo [WARNING] Service did not reach RUNNING state
    echo.
    sc query HuLoopBOT_RDP_Monitor
    echo.
  echo This may be normal if:
    echo - Service is disabled in registry (check RdpMonitoringEnabled)
    echo - Service is starting slowly
    echo.
    echo Check logs for details:
    echo C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log
    echo.
    echo Check Event Viewer:
    echo - Run: eventvwr.msc
    echo - Navigate to: Windows Logs -^> Application
    echo - Filter by Source: HuLoopBOT_RDP_Monitor
    echo.
    goto END_CHECK
) else (
    echo [ERROR] Failed to start service after 3 attempts!
    echo Error code: %START_RESULT%
    echo.
    
    if %START_RESULT% equ 1053 (
        echo This is Error 1053: Service did not respond in time
        echo.
        echo Common causes:
  echo 1. Service OnStart taking too long
   echo 2. Service has blocking operation
        echo 3. Dependencies not ready
        echo 4. Service crashed during startup
        echo.
        echo Check service log file:
        echo C:\ProgramData\HuLoopBOT\Logs\HuLoopBOT_Service_*.log
        echo.
        echo Check Event Viewer:
     echo - Run: eventvwr.msc
        echo - Windows Logs -^> Application
        echo - Source: HuLoopBOT_RDP_Monitor
    )
    
    echo.
    sc query HuLoopBOT_RDP_Monitor
    echo.
)

:END_CHECK
echo ========================================
echo Installation Complete
echo ========================================
echo.
pause
exit /b 0

:END_SUCCESS
echo ========================================
echo All Done!
echo ========================================
pause
