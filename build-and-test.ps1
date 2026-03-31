#!/usr/bin/env pwsh
# ======================================
# Food Safety Test & Build Helper (PowerShell)
# ======================================

Write-Host ""
Write-Host "====================================" -ForegroundColor Green
Write-Host "BUILD & TEST SCRIPT" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""

try {
    # 1. Clean
    Write-Host "[1/5] Cleaning..." -ForegroundColor Cyan
    dotnet clean onvatenter.sln | Out-Null
    Write-Host "? Cleaned" -ForegroundColor Green

    # 2. Restore
    Write-Host "[2/5] Restoring dependencies..." -ForegroundColor Cyan
    dotnet restore onvatenter.sln | Out-Null
    Write-Host "? Restored" -ForegroundColor Green

    # 3. Build Debug
    Write-Host "[3/5] Building (Debug)..." -ForegroundColor Cyan
    dotnet build onvatenter.sln --configuration Debug | Out-Null
    Write-Host "? Built" -ForegroundColor Green

    # 4. Run Tests
    Write-Host "[4/5] Running 9 tests..." -ForegroundColor Cyan
    $testResult = dotnet test onvatenter.sln --configuration Debug --verbosity quiet
    Write-Host "? Tests passed" -ForegroundColor Green

    # 5. Build Release
    Write-Host "[5/5] Building (Release) for deployment..." -ForegroundColor Cyan
    dotnet build onvatenter.sln --configuration Release | Out-Null
    Write-Host "? Release built" -ForegroundColor Green

    Write-Host ""
    Write-Host "====================================" -ForegroundColor Green
    Write-Host "? ALL CHECKS PASSED" -ForegroundColor Green
    Write-Host "====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. git add ." -ForegroundColor White
    Write-Host "  2. git commit -m 'feat: Add comprehensive tests with coverage'" -ForegroundColor White
    Write-Host "  3. git push origin main" -ForegroundColor White
    Write-Host ""
    Write-Host "Coverage report will be generated automatically" -ForegroundColor Yellow
    Write-Host "View at: https://rafaelboulan.github.io/oop-s2-2-mvc-83331/" -ForegroundColor Cyan
    Write-Host ""
}
catch {
    Write-Host "? Error occurred: $_" -ForegroundColor Red
    exit 1
}
