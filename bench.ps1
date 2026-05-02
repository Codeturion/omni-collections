<#
  Run Omni.Collections benchmarks. Pass through any args to BenchmarkDotNet.

  Examples:
    .\bench.ps1 --smoke --anyCategories=Linear
    .\bench.ps1 --filter '*BloomFilter*'
    .\bench.ps1 --rigorous --anyCategories=Probabilistic
    .\bench.ps1 --list flat
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
& dotnet run --configuration Release --project "$root\src\Omni.Collections.Benchmarks" -- @args
