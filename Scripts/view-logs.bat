@echo off
REM View HuLoopBOT Service Logs

echo ========================================
echo HuLoopBOT Service Logs Viewer
echo ========================================
echo.

set LOG_DIR=C:\ProgramData\HuLoopBOT\Logs

REM Check if logs directory exists
if not exist "%LOG_DIR%" (
    echo [INFO] Logs directory not found
    echo Expected location: %LOG_DIR%
    echo.
    echo The logs directory will be created when the service runs.
    echo.
    pause
    exit /b 0
)

echo Log directory: %LOG_DIR%
echo.

REM List all log files
echo Available log files:
echo.
dir /B /O-D "%LOG_DIR%\*.log" 2>nul
if %errorLevel% neq 0 (
    echo [INFO] No log files found
    echo.
    echo Logs will be created when:
    echo - The main application runs
    echo - The service is started
    echo.
    pause
    exit /b 0
)

echo.
echo ========================================
echo.

REM Find the most recent service log
for /f "delims=" %%F in ('dir /B /O-D "%LOG_DIR%\HuLoopBOT_Service_*.log" 2^>nul') do (
    set LATEST_LOG=%%F
    goto FOUND_LOG
)

:NO_SERVICE_LOG
echo [INFO] No service log files found
echo.
echo Service logs are named: HuLoopBOT_Service_YYYYMMDD.log
echo.
echo To generate service logs:
echo 1. Install the service
echo 2. Start the service
echo.
pause
exit /b 0

:FOUND_LOG
echo Latest service log: %LATEST_LOG%
echo Full path: %LOG_DIR%\%LATEST_LOG%
echo.

REM Get file size and modification time
for %%F in ("%LOG_DIR%\%LATEST_LOG%") do (
    echo File size: %%~zF bytes
    echo Last modified: %%~tF
)

echo.
echo ========================================
echo.

choice /C YN /M "Do you want to view this log file"
if %errorLevel% equ 2 goto ASK_OPEN_FOLDER

echo.
echo Opening log file...
echo.
notepad "%LOG_DIR%\%LATEST_LOG%"
goto END

:ASK_OPEN_FOLDER
echo.
choice /C YN /M "Do you want to open the logs folder"
if %errorLevel% equ 2 goto END

echo.
echo Opening logs folder...
explorer "%LOG_DIR%"

:END
echo.
