#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automated benchmark runner with automatic result organization
    
.DESCRIPTION
    Runs benchmarks and automatically organizes results with proper profile suffixes.
    Supports all benchmark categories and profiles with automatic cleanup.
    
.PARAMETER Categories
    Benchmark categories to run (linear, hybrid, spatial, etc.). Use 'all' for everything.
    
.PARAMETER Profile  
    Performance profile: fast, medium, hard, precision
    
.PARAMETER AutoOrganize
    Automatically organize results after benchmarks complete (default: true)
    
.EXAMPLE
    .\run-benchmarks.ps1 -Categories "hybrid,spatial" -Profile "fast"
    
.EXAMPLE  
    .\run-benchmarks.ps1 -Categories "all" -Profile "medium"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Categories,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("fast", "medium", "hard", "precision", "release")]
    [string]$Profile,
    
    [string]$Filter = "",
    
    [bool]$AutoOrganize = $true
)

# Colors for output
$Green = "Green"
$Yellow = "Yellow"
$Red = "Red"
$Cyan = "Cyan"

function Write-Section {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor $Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor $Green
}

function Write-Warning {
    param([string]$Message) 
    Write-Host "[WARN] $Message" -ForegroundColor $Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $Red
}

# Validate categories
$validCategories = @("linear", "hybrid", "grid", "spatial", "probabilistic", "reactive", "temporal", "all")
$categoryList = $Categories.Split(",") | ForEach-Object { $_.Trim().ToLower() }

foreach ($category in $categoryList) {
    if ($category -notin $validCategories) {
        Write-Error "Invalid category: $category. Valid options: $($validCategories -join ', ')"
        exit 1
    }
}

Write-Section "Automated Benchmark Runner"
Write-Host "Categories: $Categories" -ForegroundColor $Yellow
Write-Host "Profile: $Profile" -ForegroundColor $Yellow
if ($Filter) {
    Write-Host "Filter: $Filter" -ForegroundColor $Yellow
}
Write-Host "Auto-organize: $AutoOrganize" -ForegroundColor $Yellow
Write-Host "Timestamped results: Enabled" -ForegroundColor $Yellow

# Build command arguments
$categoryArgs = @()
foreach ($category in $categoryList) {
    $categoryArgs += "--$category"
}

$categoryArgs += "--$Profile"

# Add filter if specified
if ($Filter) {
    $categoryArgs += "--filter"
    $categoryArgs += $Filter
}

$argumentString = $categoryArgs -join " "

Write-Section "Building Project"
try {
    $buildResult = dotnet build --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
        Write-Host $buildResult -ForegroundColor $Red
        exit 1
    }
    Write-Success "Build completed successfully"
} catch {
    Write-Error "Build failed with exception: $_"
    exit 1
}

Write-Section "Running Benchmarks"
Write-Host "Command: dotnet run --configuration Release -- $argumentString" -ForegroundColor $Yellow

$startTime = Get-Date
try {
    # Use echo to pipe asterisk input to dotnet command
    $command = "echo '*' | dotnet run --configuration Release -- $argumentString"
    Write-Host "Executing: $command" -ForegroundColor $Green
    
    # Execute and capture the result
    Invoke-Expression $command
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Benchmarks failed with exit code: $LASTEXITCODE"
        exit 1
    }
    
    Write-Success "Benchmarks completed in $([math]::Round($duration.TotalMinutes, 1)) minutes"
    
} catch {
    Write-Error "Benchmarks failed with exception: $_"
    exit 1
}

# Auto-organize results
if ($AutoOrganize) {
    Write-Section "Organizing Results"
    
    try {
        if (Test-Path ".\organize-reports.ps1") {
            $organizeResult = & ".\organize-reports.ps1" $Profile 2>&1
            Write-Success "Results organized with profile suffix and timestamp: $Profile"
            Write-Host $organizeResult -ForegroundColor $Green
            
            # Extract the timestamped folder path from organize output
            $timestampedPath = $organizeResult | Where-Object { $_ -match "organized-results" } | Select-Object -First 1
            if ($timestampedPath -match "organized-results-.*") {
                $actualPath = $matches[0]
                Write-Host "• Results: .\BenchmarkDotNet.Artifacts\$actualPath" -ForegroundColor $Yellow
            }
        } else {
            Write-Warning "organize-reports.ps1 not found - skipping auto-organization"
        }
    } catch {
        Write-Warning "Failed to organize results: $_"
    }
}

Write-Section "Benchmark Run Complete"

# Display summary
Write-Host "`nSummary:" -ForegroundColor $Cyan
Write-Host "• Categories: $Categories" -ForegroundColor $Yellow
Write-Host "• Profile: $Profile" -ForegroundColor $Yellow
Write-Host "• Duration: $([math]::Round($duration.TotalMinutes, 1)) minutes" -ForegroundColor $Yellow

if ($AutoOrganize) {
    # Find the most recent organized results folder for this profile
    $searchPattern = if ($Profile -eq "") { "organized-results-*" } else { "organized-results-$Profile-*" }
    $resultsPath = Get-ChildItem -Path ".\BenchmarkDotNet.Artifacts" -Directory | 
                   Where-Object { $_.Name -like $searchPattern } | 
                   Sort-Object LastWriteTime -Descending | 
                   Select-Object -First 1
    
    if ($resultsPath) {
        Write-Host "• Results: $($resultsPath.FullName)" -ForegroundColor $Yellow
        
        # Show structure
        Write-Host "`nResults structure:" -ForegroundColor $Cyan
        Get-ChildItem -Path $resultsPath.FullName -Directory | ForEach-Object {
            $fileCount = (Get-ChildItem -Path $_.FullName -File).Count
            Write-Host "  [+] $($_.Name) ($fileCount reports)" -ForegroundColor $Green
        }
    }
}

Write-Success "Automated benchmark run completed successfully!"