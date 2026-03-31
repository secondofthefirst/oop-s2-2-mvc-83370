@echo off
REM ======================================
REM Food Safety Test & Build Helper
REM ======================================

echo.
echo ====================================
echo BUILD & TEST SCRIPT
echo ====================================
echo.

REM Colors
setlocal enabledelayedexpansion

REM 1. Clean
echo [1/5] Cleaning...
call dotnet clean onvatenter.sln >nul 2>&1
echo ? Cleaned

REM 2. Restore
echo [2/5] Restoring dependencies...
call dotnet restore onvatenter.sln >nul 2>&1
echo ? Restored

REM 3. Build Debug
echo [3/5] Building (Debug)...
call dotnet build onvatenter.sln --configuration Debug >nul 2>&1
if %errorlevel% neq 0 (
    echo ? Build failed!
    exit /b 1
)
echo ? Built

REM 4. Run Tests
echo [4/5] Running 9 tests...
call dotnet test onvatenter.sln --configuration Debug --verbosity quiet
if %errorlevel% neq 0 (
    echo ? Tests failed!
    exit /b 1
)
echo ? Tests passed

REM 5. Build Release
echo [5/5] Building (Release) for deployment...
call dotnet build onvatenter.sln --configuration Release >nul 2>&1
if %errorlevel% neq 0 (
    echo ? Release build failed!
    exit /b 1
)
echo ? Release built

echo.
echo ====================================
echo ? ALL CHECKS PASSED
echo ====================================
echo.
echo Next steps:
echo   1. git add .
echo   2. git commit -m "feat: Add comprehensive tests with coverage"
echo   3. git push origin main
echo.
echo Coverage report will be generated automatically
echo View at: https://rafaelboulan.github.io/oop-s2-2-mvc-83331/
echo.

endlocal
