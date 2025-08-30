@echo off
echo Organizing benchmark reports...
if "%1"=="" (
    powershell -ExecutionPolicy Bypass -File "organize-reports.ps1"
) else (
    powershell -ExecutionPolicy Bypass -File "organize-reports.ps1" -Profile "%1"
)
pause