@echo off
REM HuLoopBOT Main Menu - Script Launcher

:MENU
cls
echo ========================================
echo HuLoopBOT Script Launcher
echo ========================================
echo.
echo Choose an action:
echo.
echo BUILD
echo   1. Build (Debug)
echo   2. Build (Release)
echo.
echo PUBLISH
echo   3. Publish (Framework-dependent)
echo   4. Publish (Self-contained)
echo.
echo SERVICE MANAGEMENT
echo   5. Install Service (Development)
echo   6. Install Service (Published - instructions)
echo   7. Uninstall Service
echo.
echo DIAGNOSTICS
echo   8. Diagnose Service
echo   9. View Logs
echo.
echo DOCUMENTATION
echo   10. View Scripts Documentation
echo   11. View Installation Guide
echo.
echo   0. Exit
echo.
echo ========================================
echo.

choice /C 1234567890A /N /M "Enter your choice: "
set CHOICE=%errorLevel%

if %CHOICE%==1 goto BUILD_DEBUG
if %CHOICE%==2 goto BUILD_RELEASE
if %CHOICE%==3 goto PUBLISH_FW
if %CHOICE%==4 goto PUBLISH_SC
if %CHOICE%==5 goto INSTALL_DEV
if %CHOICE%==6 goto INSTALL_PUB_INFO
if %CHOICE%==7 goto UNINSTALL
if %CHOICE%==8 goto DIAGNOSE
if %CHOICE%==9 goto VIEW_LOGS
if %CHOICE%==10 goto VIEW_SCRIPTS_DOC
if %CHOICE%==11 goto VIEW_INSTALL_GUIDE
if %CHOICE%==12 goto EXIT

goto MENU

:BUILD_DEBUG
cls
echo.
echo ========================================
echo Building (Debug Configuration)
echo ========================================
echo.
call "%~dp0build-debug.bat"
pause
goto MENU

:BUILD_RELEASE
cls
echo.
echo ========================================
echo Building (Release Configuration)
echo ========================================
echo.
call "%~dp0build-release.bat"
pause
goto MENU

:PUBLISH_FW
cls
echo.
echo ========================================
echo Publishing (Framework-dependent)
echo ========================================
echo.
call "%~dp0publish.bat"
pause
goto MENU

:PUBLISH_SC
cls
echo.
echo ========================================
echo Publishing (Self-contained)
echo ========================================
echo.
call "%~dp0publish-standalone.bat"
pause
goto MENU

:INSTALL_DEV
cls
echo.
echo ========================================
echo Installing Service (Development)
echo ========================================
echo.
echo This requires Administrator privileges.
echo.
pause
call "%~dp0install-service-dev.bat"
pause
goto MENU

:INSTALL_PUB_INFO
cls
echo.
echo ========================================
echo Installing Service (Published App)
echo ========================================
echo.
echo For published applications:
echo.
echo 1. Publish the application first
echo    Run: Scripts\publish.bat
echo.
echo 2. Copy Scripts\install-service-published.bat
echoto the published application folder
echo.
echo 3. Run install-service-published.bat as Administrator
echo    from the published folder
echo.
echo The script must be in the SAME folder as
echo HuLoopBOT_Service.exe
echo.
echo ========================================
echo.
pause
goto MENU

:UNINSTALL
cls
echo.
echo ========================================
echo Uninstalling Service
echo ========================================
echo.
echo This requires Administrator privileges.
echo.
pause
call "%~dp0uninstall-service.bat"
pause
goto MENU

:DIAGNOSE
cls
echo.
echo ========================================
echo Service Diagnostics
echo ========================================
echo.
call "%~dp0diagnose-service.bat"
pause
goto MENU

:VIEW_LOGS
cls
echo.
echo ========================================
echo View Service Logs
echo ========================================
echo.
call "%~dp0view-logs.bat"
pause
goto MENU

:VIEW_SCRIPTS_DOC
cls
echo.
echo Opening Scripts Documentation...
echo.
if exist "%~dp0README.md" (
    start "" notepad "%~dp0README.md"
) else (
    echo README.md not found in Scripts folder
    pause
)
goto MENU

:VIEW_INSTALL_GUIDE
cls
echo.
echo Opening Installation Guide...
echo.
set GUIDE_PATH=%~dp0..\SERVICE_INSTALLATION_GUIDE.md
if exist "%GUIDE_PATH%" (
    start "" notepad "%GUIDE_PATH%"
) else (
    echo SERVICE_INSTALLATION_GUIDE.md not found
    pause
)
goto MENU

:EXIT
cls
echo.
echo Exiting...
echo.
exit /b 0
