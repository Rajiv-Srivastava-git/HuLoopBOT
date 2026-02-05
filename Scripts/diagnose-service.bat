@echo off
REM Service Diagnostics Tool for HuLoopBOT
REM Gathers information to help troubleshoot service issues

echo ========================================
echo HuLoopBOT Service Diagnostics
echo ========================================
echo.

REM Check admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
echo [WARNING] Not running as Administrator
    echo Some information may not be available
    echo.
)

echo Gathering diagnostic information...
echo.
echo ========================================
echo 1. SERVICE STATUS
echo ========================================
echo.

sc query HuLoopBOT_RDP_Monitor 2>nul
if %errorLevel% neq 0 (
    echo [INFO] Service not installed
    echo.
    echo To install the service, run:
    echo   Scripts\install-service-dev.bat (for development)
    echo   Scripts\install-service-published.bat (for published app)
    echo.
) else (
    echo.
    echo Service configuration:
    sc qc HuLoopBOT_RDP_Monitor 2>nul
)

echo.
echo ========================================
echo 2. REGISTRY SETTINGS
echo ========================================
echo.

reg query "HKLM\SOFTWARE\HuLoopBOT" /v RdpMonitoringEnabled 2>nul
if %errorLevel% neq 0 (
    echo [INFO] Registry key not found
    echo Expected: HKLM\SOFTWARE\HuLoopBOT\RdpMonitoringEnabled
    echo.
    echo This key controls whether the service monitors RDP sessions:
    echo   1 = Enabled (monitor sessions)
    echo   0 = Disabled (service runs but doesn't monitor)
) else (
    echo.
    echo [OK] Registry key found
)

echo.
echo ========================================
echo 3. SERVICE EXECUTABLE
echo ========================================
echo.

REM Try to find the service executable from service config
for /f "tokens=3*" %%a in ('sc qc HuLoopBOT_RDP_Monitor 2^>nul ^| findstr "BINARY_PATH_NAME"') do set SERVICE_PATH=%%a %%b

if defined SERVICE_PATH (
  REM Remove quotes
    set SERVICE_PATH=%SERVICE_PATH:"=%
    
    echo Service path: %SERVICE_PATH%
    echo.
    
    if exist "%SERVICE_PATH%" (
        echo [OK] Service executable exists
        
        REM Get file information
for %%F in ("%SERVICE_PATH%") do (
  echo File size: %%~zF bytes
echo Last modified: %%~tF
  )
    ) else (
        echo [ERROR] Service executable NOT FOUND!
        echo.
  echo The service is configured but the executable is missing.
        echo This can happen if:
        echo - Files were moved after installation
   echo - Build output directory was cleaned
   echo.
        echo Solution: Reinstall the service
    )
) else (
    echo [INFO] Service not installed or path not found
)

echo.
echo ========================================
echo 4. LOG FILES
echo ========================================
echo.

set LOG_DIR=C:\ProgramData\HuLoopBOT\Logs

if exist "%LOG_DIR%" (
    echo Log directory: %LOG_DIR%
    echo.
    
echo Recent service logs:
    dir /B /O-D "%LOG_DIR%\HuLoopBOT_Service_*.log" 2>nul | findstr /N "^"
    if %errorLevel% neq 0 (
        echo [INFO] No service log files found
        echo.
        echo Logs are created when the service runs.
    ) else (
        echo.
        echo To view logs, run: Scripts\view-logs.bat
    )
) else (
    echo [INFO] Log directory not found: %LOG_DIR%
    echo.
    echo The directory will be created when the application first runs.
)

echo.
echo ========================================
echo 5. EVENT VIEWER
echo ========================================
echo.

echo Checking for recent service events...
wevtutil qe Application "/q:*[System[Provider[@Name='HuLoopBOT_RDP_Monitor']]]" /c:5 /rd:true /f:text 2>nul
if %errorLevel% neq 0 (
    echo [INFO] No recent events found in Application log
    echo.
    echo To view events manually:
    echo 1. Run: eventvwr.msc
    echo 2. Navigate to: Windows Logs -^> Application
  echo 3. Filter by Source: HuLoopBOT_RDP_Monitor
)

echo.
echo ========================================
echo 6. .NET RUNTIME
echo ========================================
echo.

echo Installed .NET runtimes:
dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App"
if %errorLevel% neq 0 (
    echo [WARNING] .NET Desktop Runtime not found
    echo.
    echo HuLoopBOT requires .NET 8 Desktop Runtime
) else (
    echo.
    echo Checking for .NET 8...
    dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App" | findstr "8.0" >nul
    if %errorLevel% neq 0 (
   echo [WARNING] .NET 8 Desktop Runtime not found
        echo.
        echo HuLoopBOT requires .NET 8 Desktop Runtime
        echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    ) else (
        echo [OK] .NET 8 Desktop Runtime found
    )
)

echo.
echo ========================================
echo DIAGNOSTIC SUMMARY
echo ========================================
echo.

REM Summary
sc query HuLoopBOT_RDP_Monitor >nul 2>&1
if %errorLevel% equ 0 (
    echo Service Status: INSTALLED
    
    REM Check if running
    sc query HuLoopBOT_RDP_Monitor | findstr "RUNNING" >nul
    if %errorLevel% equ 0 (
        echo Service State: RUNNING
        echo [OK] Service is operational
    ) else (
        echo Service State: NOT RUNNING
        echo.
     echo Possible causes:
        echo - Service is stopped manually
    echo - Service is disabled in registry
 echo - Service crashed during startup (check logs)
   echo.
        echo Try starting the service:
        echo   sc start HuLoopBOT_RDP_Monitor
    )
) else (
    echo Service Status: NOT INSTALLED
    echo.
    echo To install the service:
    echo   Scripts\install-service-dev.bat (development)
    echo   Scripts\install-service-published.bat (published)
)

echo.
echo ========================================
echo.
echo Diagnostics complete!
echo.
pause
