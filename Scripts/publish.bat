@echo off
REM Publish script for HuLoopBOT Application Suite (Framework-dependent)
REM Run from project root directory

echo ========================================
echo Publishing HuLoopBOT Application
echo (Framework-dependent deployment)
echo ========================================
echo.

REM Get the project root directory (parent of Scripts folder if running from Scripts)
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
cd /d "%PROJECT_ROOT%"

echo Project Root: %CD%
echo.

REM Set publish configuration
set PUBLISH_DIR=publish
set CONFIGURATION=Release
set RUNTIME=win-x64

REM Clean previous publish
if exist "%PUBLISH_DIR%" (
    echo Cleaning previous publish directory...
    rmdir /s /q "%PUBLISH_DIR%" 2>nul
)
echo [OK] Clean complete
echo.

REM Publish the application
echo Publishing HuLoopBOT...
echo   Configuration: %CONFIGURATION%
echoRuntime: %RUNTIME%
echo   Output: %PUBLISH_DIR%\
echo   Self-contained: No (requires .NET 8 runtime)
echo.

dotnet publish HuLoopBOT.csproj ^
    -c %CONFIGURATION% ^
    -r %RUNTIME% ^
    --self-contained false ^
    -o %PUBLISH_DIR% ^
    /p:PublishSingleFile=false ^
    /p:DebugType=None ^
    /p:DebugSymbols=false

if errorlevel 1 (
    echo.
    echo [ERROR] Publish failed!
    echo.
    echo Check the output above for errors.
    pause
exit /b 1
)

echo.
echo [OK] Publish successful
echo.

REM Verify service executable is included
echo Verifying published files...
if exist "%PUBLISH_DIR%\HuLoopBOT.exe" (
    echo [OK] HuLoopBOT.exe found
) else (
    echo [ERROR] HuLoopBOT.exe NOT found!
)

if exist "%PUBLISH_DIR%\HuLoopBOT_Service.exe" (
    echo [OK] HuLoopBOT_Service.exe found
) else (
    echo [ERROR] HuLoopBOT_Service.exe NOT found!
    echo.
    echo The service executable is missing from publish output.
    echo Check the build targets in HuLoopBOT.csproj
)

echo.
echo ========================================
echo Publish Complete!
echo ========================================
echo.

echo Output directory: %cd%\%PUBLISH_DIR%\
echo.

echo Published executables:
dir /B "%PUBLISH_DIR%\HuLoopBOT*.exe" 2>nul
echo.

REM Show file sizes
echo File Details:
for %%F in ("%PUBLISH_DIR%\HuLoopBOT*.exe") do (
    echo   %%~nxF - %%~zF bytes
)

echo.
echo NOTE: This is a framework-dependent deployment.
echo Target machines must have .NET 8 Runtime installed.
echo.
echo To create a self-contained deployment, use:
echo   publish-standalone.bat
echo.
echo ========================================
echo Ready for deployment!
echo ========================================
echo.
pause
