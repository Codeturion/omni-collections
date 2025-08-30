@echo off
REM Batch wrapper for PowerShell benchmark automation script

if "%~1"=="" (
    echo Usage: run-benchmarks.bat [categories] [profile] [filter]
    echo.
    echo Examples:
    echo   run-benchmarks.bat all fast
    echo   run-benchmarks.bat hybrid precision "*Counter*"
    echo   run-benchmarks.bat "hybrid,spatial" medium
    echo   run-benchmarks.bat linear hard
    echo.
    echo Categories: linear, hybrid, grid, spatial, probabilistic, reactive, temporal, all
    echo Profiles: fast, medium, hard, precision, release
    echo Filter: Optional filter pattern e.g. "*CounterDictionary*" "*BloomDictionary*"
    echo Note: Results are automatically timestamped - no archiving needed
    goto :eof
)

set CATEGORIES=%1
set PROFILE=%2
set FILTER=%3

if "%FILTER%"=="" (
    powershell -ExecutionPolicy Bypass -File run-benchmarks.ps1 -Categories "%CATEGORIES%" -Profile "%PROFILE%"
) else (
    powershell -ExecutionPolicy Bypass -File run-benchmarks.ps1 -Categories "%CATEGORIES%" -Profile "%PROFILE%" -Filter "%FILTER%"
)