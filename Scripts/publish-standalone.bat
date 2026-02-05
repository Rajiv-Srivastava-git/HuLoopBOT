@echo off
REM Self-contained publish script for HuLoopBOT
REM This creates a standalone deployment with .NET runtime included
REM Run from project root directory

echo ========================================
echo Self-Contained Publish - HuLoopBOT
echo ========================================
echo.

REM Get the project root directory (parent of Scripts folder if running from Scripts)
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
cd /d "%PROJECT_ROOT%"

echo Project Root: %CD%
echo.

set PUBLISH_DIR=publish-standalone
set CONFIGURATION=Release
set RUNTIME=win-x64

REM Clean previous publish
if exist "%PUBLISH_DIR%" (
    echo Cleaning previous publish directory...
    rmdir /s /q "%PUBLISH_DIR%" 2>nul
)
echo [OK] Clean complete
echo.

echo Creating self-contained deployment...
echo This will include the .NET runtime (~100MB)
echo.
echo   Configuration: %CONFIGURATION%
echo   Runtime: %RUNTIME%
echo   Output: %PUBLISH_DIR%\
echo   Self-contained: Yes (includes .NET runtime)
echo.

dotnet publish HuLoopBOT.csproj ^
    -c %CONFIGURATION% ^
-r %RUNTIME% ^
    --self-contained true ^
 -o %PUBLISH_DIR% ^
  /p:PublishSingleFile=false ^
    /p:PublishReadyToRun=true ^
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

REM Verify both executables
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
echo Total files published:
dir "%PUBLISH_DIR%" | find "File(s)"

echo.
echo NOTE: This is a self-contained deployment.
echo - .NET 8 Runtime is INCLUDED
echo - Can run on machines WITHOUT .NET installed
echo - Larger file size (~100MB)
echo.
echo For smaller deployment (requires .NET), use:
echo   publish.bat
echo.
echo ========================================
echo Ready for deployment!
echo ========================================
echo.
pause
