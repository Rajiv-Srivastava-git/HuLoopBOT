@echo off
REM Build script for HuLoopBOT and HuLoopBOT Service (Debug Configuration)
REM Run from project root directory

echo ========================================
echo Building HuLoopBOT Application Suite
echo (Debug Configuration)
echo ========================================
echo.

REM Get the project root directory (parent of Scripts folder if running from Scripts)
set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
cd /d "%PROJECT_ROOT%"

echo Project Root: %CD%
echo.

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean HuLoopBOT.csproj >nul 2>&1
dotnet clean ServiceHost\HuLoopBOT_Service.csproj >nul 2>&1
echo [OK] Clean complete
echo.

REM Build the solution (this builds both projects and copies files)
echo Building solution...
dotnet build HuLoopBOT.csproj -c Debug -v minimal
if errorlevel 1 (
    echo [ERROR] Build failed!
    echo.
    echo Check the build output above for errors.
    pause
    exit /b 1
)

echo [OK] Build successful
echo.

REM Verify service executable was copied
echo Verifying service files...
if exist "bin\Debug\net8.0-windows\HuLoopBOT_Service.exe" (
    echo [OK] HuLoopBOT_Service.exe found
) else (
    echo [WARNING] HuLoopBOT_Service.exe not found in output directory
    echo The build targets may not have executed correctly
)

if exist "bin\Debug\net8.0-windows\HuLoopBOT.exe" (
    echo [OK] HuLoopBOT.exe found
) else (
    echo [ERROR] HuLoopBOT.exe not found in output directory
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Output directory: bin\Debug\net8.0-windows\
echo.
echo Built executables:
dir /B "bin\Debug\net8.0-windows\HuLoopBOT*.exe" 2>nul
echo.
echo Ready for debugging and testing.
echo.
pause
