@echo off
echo ========================================
echo Step 1: Fixing Orleans Database Tables
echo ========================================
echo.

cd OrleansDbFixer
dotnet run --configuration Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Database fix failed!
    echo Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo ========================================
echo Step 2: Starting FakeMicro API
echo ========================================
echo.

cd ..\FakeMicro.Api
dotnet run

pause
