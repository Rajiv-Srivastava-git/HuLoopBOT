@echo off
REM Deploy Error 1053 Fix - Batch Wrapper
REM This will request administrator privileges automatically

echo ========================================
echo  Deploying Error 1053 Fix to Service
echo ========================================
echo.
echo This script will:
echo  1. Stop and uninstall the old service
echo  2. Install the newly built service with fixes
echo  3. Start the service and verify it works
echo.
echo Requesting Administrator privileges...
echo.

PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& {Start-Process PowerShell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0Deploy-Error1053-Fix.ps1\"' -Verb RunAs}"

echo.
echo Script launched with admin privileges.
echo Check the new window for results.
echo.
pause
