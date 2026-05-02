# Benchmarks

Performance characteristics for every public collection in Omni.Collections, measured against its closest BCL equivalent.

## Quick start

```pwsh
# Quick smoke check (~1 min). Validates the suite runs; not for performance claims.
.\bench.ps1 --smoke --anyCategories=Linear

# Standard run (~5-10 min per category). The numbers you should quote.
.\bench.ps1 --anyCategories=Linear

# Rigorous run for release validation (~30-60 min per category).
.\bench.ps1 --rigorous --anyCategories=Probabilistic
```

POSIX equivalent: `./bench.sh` instead of `.\bench.ps1`.

## Profiles

| Profile | Warmup | Iterations | Launches | Total time per category | Use for |
|---|---|---|---|---|---|
| `--smoke` | 1 | 3 | 1 | ~1 min | CI smoke check, sanity tests |
| `--standard` (default) | 6 | 15 | 1 | 5-10 min | PR review, day-to-day |
| `--rigorous` | 10 | 25 | 3 | 30-60 min | Release validation, published numbers |

Smoke profile error margins are explicitly large — the iteration count is too low for statistical confidence. If you want defendable numbers, use `--standard` or `--rigorous`.

## Selection

The harness passes selection arguments straight through to BenchmarkDotNet:

```
--list flat                              # List all benchmarks and exit
--filter '*BoundedList*'                 # Match by class/method name
--anyCategories=Linear,Hybrid            # Select by [BenchmarkCategory]
--allCategories=Linear,Add               # Match all listed categories
```

## Where outputs go

Results land under `src/Omni.Collections.Benchmarks/BenchmarkDotNet.Artifacts/results/`:

- `*-report-github.md` — GitHub-flavored markdown table, ready to paste into a PR.
- `*-report.csv` — for tooling.
- `*-report.html` — browser-friendly view.

The directory is gitignored. To publish reference numbers we copy the relevant markdown into `docs/perf/` under a hardware-specific subfolder.

## Interpreting results

A typical row:

```
| Method        | N     | Mean       | Error    | StdDev   | Median     | Ratio | Allocated |
|-------------- |------ |-----------:|---------:|---------:|-----------:|------:|----------:|
| Omni_AddN     | 10000 |  26.23 us  |  0.59 us |  0.32 us |  26.00 us  |  1.16 |     400 B |
| Baseline_AddN | 10000 |  22.57 us  |  0.38 us |  0.21 us |  22.40 us  |  1.00 |     400 B |
```

- **Mean / Median**: per-invocation cost. With `[InvocationCount(1)]` this is the cost of doing the full bulk operation once. Without it, BDN auto-tunes the inner loop count.
- **Error**: half-width of the 99.9% confidence interval. If `Error / Mean > ~5%` the run probably needs more iterations to be quotable.
- **StdDev**: standard deviation across iterations. If StdDev is large relative to Median, the operation has high variance — investigate before quoting.
- **Ratio**: relative to the row marked `[Benchmark(Baseline = true)]` (BCL counterpart). 1.00 = same speed; 1.16 = 16% slower; 0.50 = twice as fast.
- **Allocated**: bytes allocated per invocation. With BDN's `[MemoryDiagnoser]`, the constant ~400 B / ~25 B baseline for stateful tests is overhead from `[IterationSetup]` collection construction, equal across both Omni and Baseline rows. The signal is the *delta*, not the absolute number.

### Sanity checks before believing a result

1. The Mean is in a physically plausible range. List indexer should be ~1-30 ns; dictionary lookup ~30-100 ns; bulk add 1-10 ns/item. Anything 1000× off is a methodology bug, not a real result.
2. Error / Mean is below ~5%. Above that, the difference between Omni and Baseline is statistical noise.
3. Ratio is consistent across sizes. A ratio that swings from 0.5 → 1.5 → 0.8 across N=1k/10k/100k usually means the benchmark is measuring something other than the operation under test.
4. Allocated rows match between Omni and Baseline for the same operation, except where the Omni claim is specifically about reduced allocation.

## What the suite does NOT do (yet)

- Cross-platform numbers. All published numbers are from Windows + .NET 8 unless otherwise noted. Linux/macOS numbers may differ — Spectre mitigations, GC heuristics, and Stopwatch resolution are platform-specific.
- ARM64 runs. Intel/AMD x64 only at present.
- Server GC vs Workstation GC sweeps.

## Methodology principles

The suite is intentionally rebuilt from scratch (Phase 1 of the v2.0 effort). The previous version had several methodological defects that made its numbers untrustworthy. Specifically the new suite guarantees:

1. **No mutation of shared state inside the measured method.** Stateful operations use `[InvocationCount(1)]` plus `[IterationSetup(Targets = ...)]` to recreate state before each timed iteration. The previous suite's `if (count < capacity) Add()` pattern would hit the capacity limit during the timed loop, so most measurements were no-op early-returns.
2. **No `Random.Next` in the measured path.** All randomness is precomputed in `[GlobalSetup]` and stored in arrays; the measured method indexes into them with a cycling counter. The previous suite called `Random.Shared.Next` inside indexer benchmarks, which dwarfed the operation cost.
3. **Out-of-process toolchain (BDN default).** Every benchmark class runs in its own forked process, so JIT / GC / static state cannot leak between classes. The previous suite used `InProcessEmitToolchain`, which shared everything.
4. **Default GC handling.** No custom `GC.Collect()` in `[IterationSetup]`. The previous suite's three GC calls before every iteration cold-started caches and silently changed measurement semantics.
5. **BDN-default warmup and iteration counts** under `--standard`. The previous suite ran 2 warmups + 5 iterations, producing 50-90% error margins.
6. **Multi-size `[Params]`** spanning two orders of magnitude. The previous suite used a single 50,000 size, giving no scaling visibility.
7. **Each `[Benchmark]` documents its claim.** The XML `<summary>` line above every benchmark method states what the comparison is meant to prove. If we can't articulate the claim, the benchmark gets cut.

## Coverage

The suite covers every public Omni.Collections type:

| Category | Classes | Compared against |
|---|---|---|
| Linear (6) | BoundedList, FastQueue, MaxHeap, MinHeap, PooledList, PooledStack | List<T>, Queue<T>, PriorityQueue<,>, Stack<T> |
| Hybrid (9) | LinkedDictionary, QueueDictionary, CounterDictionary, CircularDictionary, DequeDictionary, ConcurrentLinkedDictionary, LinkedMultiMap, GraphDictionary, PredictiveDictionary | Dictionary<K,V>, ConcurrentDictionary, Dictionary<K,List<V>> |
| Grid (3) | BitGrid2D, HexGrid2D, LayeredGrid2D | bool[,], Dictionary<(q,r),V>, int[,,] |
| Spatial (6) | QuadTree, SpatialHashGrid, KdTree, OctTree, BloomRTreeDictionary, TemporalSpatialHashGrid | List<T>+linear scan, Dictionary, SpatialHashGrid |
| Probabilistic (6) | BloomFilter, HyperLogLog, CountMinSketch, BloomDictionary, TDigest, DigestStreamingAnalytics | HashSet<T>, Dictionary<K,V>, sorted double[] |
| Reactive (2) | ObservableList, ObservableHashSet | ObservableCollection<T>, HashSet<T> |
| Temporal (1) | TimelineArray | List<(long,T)> |

Total: ~33 benchmark classes producing ~600 individual benchmarks across the three sizes.

## The two measurement patterns

**Per-op pattern** (`InvocationCount(32768)` + state-resetting `[IterationSetup]`)

Used for Add/Remove/Push/Pop/Insert/Extract operations. Each measurement exercises one mutating op against a fresh collection state. Mean reports per-op cost; Allocated reports per-op bytes. Adds ~2 ns of BDN harness overhead which appears in both Omni and Baseline rows, so the Ratio remains meaningful.

**Bulk Fill pattern** (`InvocationCount(1)` + default-or-natural starting capacity)

Used for the `Fill` benchmark category. Creates a collection from default constructor and adds N items. This is the realistic populate-from-scratch scenario where the Allocated column shows resize-amplification cost (e.g., `List<T>` growing to 100k items allocates ~2.1 MB across multiple resize copies, while `BoundedList<T>(N)` allocates a single 800 KB array).

Both patterns coexist: per-op shows steady-state cost, Fill shows realistic populate cost. Read-only operations (Indexer, Lookup, Peek, Enumerate) use BDN's default auto-tuning.

## Hardware reference

When publishing reference numbers under `docs/perf/`, include a hardware banner that BDN itself generates at the top of every report. Sample:

```
BenchmarkDotNet v0.14.0, Windows 11
AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.200
  [Host]: .NET 8.0.x ...
```

Numbers measured on different hardware are not directly comparable.
