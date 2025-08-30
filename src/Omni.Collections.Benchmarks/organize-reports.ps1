# PowerShell script to reorganize BenchmarkDotNet reports into cleaner structure
# Usage: .\organize-reports.ps1 [profile]
# Example: .\organize-reports.ps1 fast
# Example: .\organize-reports.ps1 medium

param(
    [string]$Profile = ""
)

$artifactsPath = ".\BenchmarkDotNet.Artifacts\results"

# Generate timestamp
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

# Determine output path based on profile
if ($Profile -eq "") {
    $outputPath = ".\BenchmarkDotNet.Artifacts\organized-results-$timestamp"
    Write-Host "Organizing benchmark reports (no profile specified) with timestamp: $timestamp" -ForegroundColor Green
} else {
    $outputPath = ".\BenchmarkDotNet.Artifacts\organized-results-$Profile-$timestamp"
    Write-Host "Organizing benchmark reports for profile: $Profile with timestamp: $timestamp" -ForegroundColor Green
}

# Create output directory if it doesn't exist
if (!(Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath
}

# Create category directories
$categories = @(
    "LinearStructureBenchmarks",
    "HybridStructureBenchmarks", 
    "GridStructureBenchmarks",
    "SpatialStructureBenchmarks",
    "ProbabilisticStructureBenchmarks",
    "ReactiveStructureBenchmarks",
    "TemporalStructureBenchmarks"
)

foreach ($category in $categories) {
    $categoryPath = "$outputPath\$category"
    if (!(Test-Path $categoryPath)) {
        New-Item -ItemType Directory -Path $categoryPath
    }
}

# Process each report file
Get-ChildItem "$artifactsPath\*.md" | ForEach-Object {
    $fileName = $_.Name
    $cleanName = ""
    $targetCategory = ""
    
    # Extract clean name based on original file name patterns
    if ($fileName -match "Omni\.Collections\.Benchmarks\.Core\.(\w+)\.(\w+)-report-github\.md") {
        $category = $matches[1]
        $benchmark = $matches[2]
        
        # Add profile suffix to filename if specified
        if ($Profile -eq "") {
            $cleanName = "$benchmark-report.md"
        } else {
            $cleanName = "$benchmark-report-$Profile.md"
        }
        $targetCategory = $category
    }
    
    if ($targetCategory -and $cleanName) {
        $destinationPath = "$outputPath\$targetCategory\$cleanName"
        Write-Host "Moving $fileName -> $targetCategory\$cleanName"
        Copy-Item $_.FullName $destinationPath -Force
    } else {
        Write-Host "Could not parse: $fileName" -ForegroundColor Yellow
    }
}

Write-Host "Reports reorganized in: $outputPath"
Write-Host ""
Write-Host "New structure:"
Get-ChildItem $outputPath -Recurse -Name | Sort-Object