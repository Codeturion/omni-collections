# Omni.Collections v2.0.0 — Rigorous-profile reference numbers (i7-13700KF)

Hardware:

```
BenchmarkDotNet v0.15.8, Windows 11
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical / 16 physical cores
.NET SDK 9.0.200 / .NET 8.0.26 host
```

Profile: `--rigorous` = WarmupCount=10, IterationCount=25, LaunchCount=3 (vs standard's 6 / 15 / 1). Methodology: see [`docs/benchmarks.md`](../../../benchmarks.md). These are the **published numbers** for v2.0.0 release.

Reproduce against `dev/omni-collections-v2`:

```pwsh
.\bench.ps1 --rigorous --filter '*<TypeName>Benchmarks*'
```

| File | Coverage |
|---|---|
| [`01-spatial-grid-reactive-temporal-bounded.md`](01-spatial-grid-reactive-temporal-bounded.md) | KdTree, QuadTree, OctTree, BitGrid2D, LayeredGrid2D, ObservableList, TimelineArray, BoundedList |
| [`02-bloom-hll-blomdict-heaps.md`](02-bloom-hll-blomdict-heaps.md) | BloomFilter, HyperLogLog, BloomDictionary, MinHeap, MaxHeap |

## What rigorous changed vs standard

The user pushed back on standard-only data for v2.0.0 publish. They were right: rigorous's extra warmup + 3-launch averaging produced materially different numbers in several places, including some claim flips. Documenting both sets honestly:

### 🟢 Confirmed wins (rigorous = standard, within noise)

| Type / op | Rigorous Ratio | Verdict |
|---|---:|---|
| `KdTree.FindNearest @ N=1k / 10k / 100k` | 0.29 / 0.04 / **0.005** | ~200× faster at N=100k. Real. |
| `QuadTree.Query @ N=1k / 10k / 100k` | 1.35 / **0.02** / **0.009** | 50-100× faster at N≥10k. Real. |
| `OctTree.RadiusQuery @ N=1k / 10k / 100k` | 0.34 / **0.07** / **0.02** | 3-15× faster at N≥10k. Real. |
| `TimelineArray.GetAtTime` | 0.19 / 0.17 / 0.22 | ~5× faster across N. Real. |
| `ObservableList.Add @ N=1k / 10k / 100k` | 0.49 / 0.50 / **0.41** | Up to 2.4× faster. Real. |
| `ObservableList.Fill @ N=1k / 10k / 100k` | 0.62 / 0.33 / **0.30** | 1.6-3.3× faster. Real. |
| `BitGrid2D.Fill @ 64×64 / 256×256 / 1024×1024` | 0.51 / 0.15 / **0.10** | 2-10× faster. Real (less dramatic than standard's 14-50× claim — standard exaggerated). |
| `LayeredGrid2D.Fill @ 64×64 / 256×256 / 1024×1024` | 0.82 / 0.40 / 0.22 | 1.2-5× faster. Real. |
| `BoundedList.Add @ N=1k / 10k / 100k` | 0.91 / 0.91 / 0.93 | 7-9% faster. Real. |
| `MinHeap.Insert / MaxHeap.Insert` | 0.84-0.86 | 14-16% faster than `PriorityQueue.Enqueue`. Real. |
| `HyperLogLog.Add @ N=100k` | **0.60** | 40% faster (standard claimed 62%; rigorous reins it in but the win still holds). |

### ❌ Standard claims FLIPPED by rigorous

When the BCL baseline benefits more from extra warmup than Omni does, ratios flip. The standard-time "wins" below are **not real**:

| Type / op | Standard Ratio | Rigorous Ratio | Reality |
|---|---:|---:|---|
| `BloomFilter.ContainsHit` | 0.74 (28% faster) | **1.88-2.23 (2× slower)** | Standard had cold-JIT baseline at ~20 ns; rigorous warms `HashSet<long>.Contains` to ~7-8 ns. BloomFilter is **slower** than `HashSet` for Contains. The win is bounded memory (capability), not speed. |
| `BloomFilter.ContainsMiss` | ~0.71 | **1.32-1.58** | Same story. |
| `BitGrid2D.Get` | 0.56-0.70 (~40% faster) | **1.55-1.75 (1.6× slower)** | Standard had `bool[,]` indexer JIT-cold at ~1 ns; rigorous warms it to ~0.33 ns. BitGrid2D's bit-extraction logic is **slower** than direct `bool[,]` access. The win is **8× less memory** (structural), not speed. |
| `LayeredGrid2D.Get` | 0.96-1.09 (parity) | **2.51-2.94 (2.5-3× slower)** | Same — parity claim doesn't survive. |
| `MinHeap.Fill / MaxHeap.Fill` | 2.4-3.0× slower | **0.84-0.98 (parity)** | Standard exaggerated regression; rigorous shows parity to slightly faster. |
| `MinHeap.ExtractMin / MaxHeap.ExtractMax` | 2.4× slower | **1.36-1.83 (1.4-1.8× slower)** | Less bad than standard but still slower than .NET 8's `PriorityQueue.Dequeue`. |

### 🟠 Capability-only (slower than naïve baseline; structure is the value)

These were already capability-only in the standard report; rigorous just confirms them.

- `BloomDictionary.LookupHit` Ratio 3.07-3.74 — bounded false-positive rate, not speed.
- `BloomFilter.Fill` Ratio 1.58-3.42 — bounded memory.
- `HyperLogLog.Add @ small N` Ratio 1.58-1.84 — constant memory cardinality.
- All 9 Hybrid types — see standard reports under `../standard-v2.0.0/`. The capability story (LRU / LFU / FIFO / multi-value / graph / pattern) holds regardless of profile.

## Methodology takeaway

For sub-microsecond operations, **standard profile is not safe for release claims**. JIT tier-1 compilation may not have stabilized in 6 warmup + 15 iterations. Rigorous (10 / 25 / 3-launch) is the bar for v2.0.0+ published numbers.

The standard reports under `../standard-v2.0.0/` remain valid for PR-time before/after deltas during development — same conditions on both sides — but don't quote them as release claims.
