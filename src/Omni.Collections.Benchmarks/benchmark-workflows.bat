@echo off
REM Common benchmark workflow shortcuts

echo ===============================================
echo Omni.Collections Benchmark Workflow Shortcuts
echo ===============================================
echo.

:menu
echo Select a workflow:
echo.
echo 1. Quick Development Test (hybrid + spatial, fast)
echo 2. Daily Validation (all categories, fast) 
echo 3. Weekly Review (all categories, medium)
echo 4. Production Release (all categories, hard)
echo 5. Linear Structures Only (fast)
echo 6. Hybrid Structures Only (fast)
echo 7. Spatial Structures Only (fast)
echo 8. Custom workflow
echo 9. Show current results
echo 0. Exit
echo.

set /p choice="Enter your choice (0-9): "

if "%choice%"=="1" (
    echo Running quick development test...
    call run-benchmarks.bat "hybrid,spatial" fast
    goto :menu
)

if "%choice%"=="2" (
    echo Running daily validation...
    call run-benchmarks.bat all fast
    goto :menu
)

if "%choice%"=="3" (
    echo Running weekly review...
    call run-benchmarks.bat all medium
    goto :menu
)

if "%choice%"=="4" (
    echo Running production release benchmarks...
    call run-benchmarks.bat all hard
    goto :menu
)

if "%choice%"=="5" (
    echo Running linear structures benchmarks...
    call run-benchmarks.bat linear fast
    goto :menu
)

if "%choice%"=="6" (
    echo Running hybrid structures benchmarks...
    call run-benchmarks.bat hybrid fast
    goto :menu
)

if "%choice%"=="7" (
    echo Running spatial structures benchmarks...
    call run-benchmarks.bat spatial fast
    goto :menu
)

if "%choice%"=="8" (
    echo Custom workflow:
    echo Available categories: linear, hybrid, grid, spatial, probabilistic, reactive, temporal, all
    echo Available profiles: fast, medium, hard
    echo.
    set /p categories="Enter categories (comma-separated): "
    set /p profile="Enter profile: "
    
    call run-benchmarks.bat "%categories%" "%profile%"
    goto :menu
)

if "%choice%"=="9" (
    echo Current benchmark results:
    echo.
    if exist "BenchmarkDotNet.Artifacts\organized-results-fast" (
        echo Fast Results:
        dir /b "BenchmarkDotNet.Artifacts\organized-results-fast"
        echo.
    )
    if exist "BenchmarkDotNet.Artifacts\organized-results-medium" (
        echo Medium Results:
        dir /b "BenchmarkDotNet.Artifacts\organized-results-medium"
        echo.
    )
    if exist "BenchmarkDotNet.Artifacts\organized-results-hard" (
        echo Hard Results:
        dir /b "BenchmarkDotNet.Artifacts\organized-results-hard"
        echo.
    )
    if exist "BenchmarkDotNet.Artifacts\archived-results*" (
        echo Archived Results:
        dir /b "BenchmarkDotNet.Artifacts\archived-results*"
        echo.
    )
    pause
    goto :menu
)

if "%choice%"=="0" (
    echo Goodbye!
    goto :eof
)

echo Invalid choice. Please try again.
goto :menu