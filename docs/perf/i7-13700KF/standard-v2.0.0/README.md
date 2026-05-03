# Omni.Collections v2.0.0 — Standard-profile reference numbers (i7-13700KF)

Hardware:

```
BenchmarkDotNet v0.15.8, Windows 11
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical / 16 physical cores
.NET SDK 9.0.200 / .NET 8.0.26 host
```

Profile: `--standard` (BDN-default warmup + iteration counts, out-of-process toolchain). Methodology: see [`docs/benchmarks.md`](../../../benchmarks.md).

Reproduce against `dev/omni-collections-v2` HEAD `097943a` (post Phase 5 merge):

```pwsh
.\bench.ps1 --filter '*<TypeName>*'
```

| File | Coverage |
|---|---|
| [`01-linear-postfix-FastQueue-BoundedList-CMS.md`](01-linear-postfix-FastQueue-BoundedList-CMS.md) | Targeted re-bench of Phase 3 + Phase 5 perf fixes |
| [`02-linear-MinMaxHeap-Pooled-Digest.md`](02-linear-MinMaxHeap-Pooled-Digest.md) | Linear (4 untested) + Probabilistic Digest |
| [`03-hybrid.md`](03-hybrid.md) | All 9 Hybrid types |
| [`04-spatial-grid-reactive-temporal-tdigest.md`](04-spatial-grid-reactive-temporal-tdigest.md) | Spatial 5 + Grid 3 + Reactive 2 + Temporal 1 + TDigest |

For interpretation tiers (🟢 / 🟠 / ❌) and the full per-type win/loss table, see `currentbenchmarks.md` at the repo root (gitignored — personal session ledger; the `Phase 6 wide standard sweep` section captures the headline findings).

## Headlines

🟢 **Algorithmic / structural wins:**
- `KdTree.FindNearest @ N=100k`: ~200× faster than `List<T>` linear scan
- `QuadTree.Query @ N=10k+`: 50-100× faster
- `OctTree.RadiusQuery`: 3-15× faster
- `BitGrid2D.Get`: ~40% faster than `bool[,]` + 8× less memory
- `BitGrid2D.Fill`: 14-50× faster
- `LayeredGrid2D.Fill`: 5-25× faster
- `ObservableList.Add`: up to 2× faster than `ObservableCollection<T>`
- `TimelineArray.GetAtTime`: ~5× faster than `List<(t,v)>` linear scan
- `BloomFilter.ContainsHit`: 28% faster (Phase 2 IHasher rework)
- `BoundedList.Add`: 9-12% faster than `List<T>.Add` (Phase 3 inlining)

🟠 **Capability-only** (slower than naïve baseline; structure is the value):
- All 9 Hybrid types: LRU / LFU / FIFO / multi-value / graph / pattern-learning
- TDigest: streaming approximate-quantile estimation
- DigestStreamingAnalytics: bounded-memory windowed quantile
- HexGrid2D: hex-grid coord math
- BloomRTreeDictionary: spatial bloom prune layer

❌ **Honest losses:**
- `MinHeap.ExtractMin / MaxHeap.ExtractMax`: 2.4× slower than .NET 8 `PriorityQueue<,>`. Insert is still 15% faster.
- `FastQueue.Dequeue/Enqueue` at small N: ~5× slower than `Queue<T>` due to mandatory `ThrowIfDisposed` (Phase 3 correctness fix). Parity at large N.
- `CountMinSketch.Add` at small N: 6× slower from XxHash3 cost. Faster at N=100k. Correctness > speed.

## Future runs

A `--rigorous` run (3 launches × 10 warmup × 25 iterations per benchmark) for the v2.0.0 final release validation is planned. Standard numbers are sufficient for the published claims; rigorous is for tighter error bars on the 🟢 wins.
