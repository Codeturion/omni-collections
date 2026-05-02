#!/usr/bin/env bash
# Run Omni.Collections benchmarks. Pass through any args to BenchmarkDotNet.
#
# Examples:
#   ./bench.sh --smoke --anyCategories=Linear
#   ./bench.sh --filter '*BloomFilter*'
#   ./bench.sh --rigorous --anyCategories=Probabilistic
#   ./bench.sh --list flat
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec dotnet run --configuration Release --project "$ROOT/src/Omni.Collections.Benchmarks" -- "$@"
