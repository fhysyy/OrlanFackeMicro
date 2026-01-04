@echo off
setlocal enabledelayedexpansion

set "TEST_TYPE=%~1"
set "FILTER=%~2"
set "VERBOSE=%~3"
set "NO_BUILD=%~4"

if "%TEST_TYPE%"=="" set "TEST_TYPE=all"
if "%FILTER%"=="" set "FILTER="
if "%VERBOSE%"=="" set "VERBOSE=false"
if "%NO_BUILD%"=="" set "NO_BUILD=false"

powershell -ExecutionPolicy Bypass -File "%~dp0Run-Tests.ps1" -TestType "%TEST_TYPE%" -Filter "%FILTER%" -Verbose:%VERBOSE% -NoBuild:%NO_BUILD%

endlocal
