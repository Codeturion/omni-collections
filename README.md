# Omni.Collections

[![NuGet Version](https://img.shields.io/nuget/v/OmniCollections.svg)](https://www.nuget.org/packages/OmniCollections/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OmniCollections.svg)](https://www.nuget.org/packages/OmniCollections/)
[![Build Status](https://github.com/Codeturion/omni-collections/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/Codeturion/omni-collections/actions)
[![GitHub Stars](https://img.shields.io/github/stars/Codeturion/omni-collections)](https://github.com/Codeturion/omni-collections)
[![GitHub Last Commit](https://img.shields.io/github/last-commit/Codeturion/omni-collections)](https://github.com/Codeturion/omni-collections)
[![License: PolyForm Noncommercial](https://img.shields.io/badge/License-PolyForm%20Noncommercial%201.0.0-blue.svg)](https://polyformproject.org/licenses/noncommercial/1.0.0/)

**32 specialized .NET data structures addressing algorithmic bottlenecks**

When .NET's built-in collections hit their limits — spatial indexing, priority processing, bounded memory, streaming analytics — these structures provide the missing pieces. Every type wins on one of five axes: speed, allocation, memory, predictability, or unique capability. Numbers and trade-offs live in [`docs/perf/`](docs/perf/).

## What's New in v2.0.0

v2.0.0 is a defensible re-foundation. Every public type now justifies its existence on a measurable axis — proven by the rebuilt benchmark suite or documented honestly when the win is capability rather than speed.

🟢 **Probabilistic correctness rework** ([PR #3](https://github.com/Codeturion/omni-collections/pull/3))

The probabilistic family (`BloomFilter`, `HyperLogLog`, `CountMinSketch`, `BloomRTreeDictionary`) had a hidden correctness bug: they hashed via `(uint)item.GetHashCode()`, which for randomized-string `GetHashCode` and arbitrary user types destroys the false-positive / cardinality / frequency math. v2.0 introduces a new `Omni.Collections.Core.Hashing.IHasher<T>` abstraction backed by **XxHash3** (default) and **Murmur3-128**, with specialized fast paths for `int`, `uint`, `long`, `ulong`, `string`, `Guid`, `byte[]`, `ReadOnlyMemory<byte>`. The math now actually holds — empirical false-positive rate stays within 2× design FPR across 50 random seeds, verified by FsCheck property tests. Speed-wise `BloomFilter.Contains` is ~2× slower than `HashSet<T>.Contains` under rigorous benchmarks; the win is the fixed-size memory budget regardless of N. `CountMinSketch`'s hardcoded `Random(42)` per-row seeds (a CVE-tier predictability bug) were replaced with deterministic SplitMix64-derived seeds.

🔧 **25 correctness fixes** ([PR #4](https://github.com/Codeturion/omni-collections/pull/4))

Stub methods that returned hardcoded `false`, `else if` short-circuits that masked same-instance collisions, `Resize` paths that didn't clear bucket arrays, `Clear` paths that leaked node references, `KDTree.FindNearest` returning the wrong default for empty trees, R-Tree `FindWorstPair` doing O(n²) repeated sorts, and 18 more. See PR #4 for the full list.

⚡ **Reactive correctness** ([PR #5](https://github.com/Codeturion/omni-collections/pull/5))

`ObservableList` / `ObservableHashSet` filtered views are now `IDisposable` and properly unsubscribe from the source on Dispose (previously leaked indefinitely). Re-entrancy guards block mutations from inside `CollectionChanged` callbacks. Notification doctrine is uniform across batch ops (one `Add` event for batched additions, one `Reset` for non-contiguous removals). The event-args pool routing seam is now in place for future allocation-free notifications.

🧪 **Test coverage rebuild** ([PR #6](https://github.com/Codeturion/omni-collections/pull/6))

+209 new tests across `PredictiveDictionary`, `ConcurrentLinkedDictionary` (including 4-thread concurrency stress with `Barrier`-synced start), `LinkedMultiMap`, `GraphDictionary`, `HexPathfinding`, plus FsCheck property tests for the probabilistic types' theoretical bounds (false-positive rate, cardinality SE, frequency upper bound). New `IClock` abstraction lets temporal tests advance time deterministically instead of `Thread.Sleep`. CI now runs the suite under three RNG seeds per PR.

📊 **Multi-target + packaging**
- Multi-targets `net8.0;netstandard2.1` for libraries, `net8.0;net6.0` for tests, `net8.0` for benchmarks.
- All sub-packages now pack `README.md` and `LICENSE`.
- SourceLink + symbol packages (`.snupkg`) for every release.
- `Microsoft.CodeAnalysis.PublicApiAnalyzers` baselines the public surface so v2.x patch releases can't accidentally break consumers.

### v1.x → v2.0 migration

**Hard breaks** (intentional):

1. **`BloomFilter<T>` etc. for custom types now require an `IHasher<T>`.** Built-in types (`int`/`long`/`string`/`Guid`/etc.) work as before; for your own types, implement `IHasher<T>` and pass it via the new constructor overloads. The error message at runtime names the supported types and points to `IHasher<T>`.
2. **`BitGrid2D.GetRowSpan(int y)` removed.** It had misleading zero-copy semantics over bit-packed storage. Replaced by `CopyRowTo(int y, Span<bool> destination)` (caller buffer) and `GetRowCopy(int y)` (explicit allocation). `LayeredGrid2D.GetRowSpan` is unchanged — its span is real.
3. **`ObservableList.RemoveAll` / `RemoveAllAsync` now fire `NotifyCollectionChangedAction.Reset`** instead of `Remove` with a starting index. Non-contiguous removals can't satisfy `INotifyCollectionChanged`'s contiguous-range contract; the previous shape would silently mis-position WPF data bindings. Per-item `ItemRemoved` events still fire on the side channel.
4. **`FastQueue<T>` renamed to `PooledQueue<T>`**, **`CircularDictionary<K,V>` renamed to `BoundedDictionary<K,V>`**. Mechanical search-and-replace. The `PooledQueue` rename also drops the "fast" claim from its XML doc — Phase 3's `ThrowIfDisposed` brought it to parity with `Queue<T>` at large N (slower at small N). The value-prop is the `ArrayPool` rental and `Span<T>` batch APIs, not raw per-op throughput.
5. **`PredictiveDictionary<K,V>` scope-narrowed**: removed `Statistics`, `GetConfidence`, `UpdateModel`, `EvictStalePatterns`, the `IClock`-injected and `SecureHashOptions`-overloaded constructors, and the time-based pattern eviction. Kept the prediction surface (`GetPredictions`, `PrefetchLikely`).
6. **`BoundingRectangle.Contains(float, float)`** documented convention: closed `[min, max]` interval. The cell-indexing math in `QuadTree`/`SpatialHashGrid`/`BloomRTreeDictionary` uses half-open `[min, max)` internally without going through `Contains`.
7. **`BloomDictionary<TKey, TValue>` removed.** The capability claim "fast miss-path short-circuit" doesn't hold under fair miss-heavy benchmarks: bloom pre-screen costs ~30 ns per lookup; the Dictionary miss probe it skips costs ~17 ns. Bloom adds latency without saving any. Memory was also worse (bloom + dict, not bloom alone). Use `BloomFilter<T>` + a separate `Dictionary<K,V>` if you genuinely need a bloom-pre-screened keyed cache and your workload profile somehow makes the math work — the library doesn't justify shipping the combo.

If you upgrade and hit a `NotSupportedException` from `Hashers.Default<T>`, that's #1 — implement `IHasher<T>`. If a test asserts `OldStartingIndex` on a `RemoveAll` event, that's #3 — switch to `Reset` handling or count `ItemRemoved` events.

📈 **Benchmarks**: methodology, profiles, and reproduction commands live in [`docs/benchmarks.md`](docs/benchmarks.md). Released reference numbers (rigorous profile, 10 warmup × 25 iter × 3 launches) ship under [`docs/perf/i7-13700KF/rigorous-v2.0.0/`](docs/perf/i7-13700KF/rigorous-v2.0.0/). Headline rigorous wins:

| Type / op | Ratio vs baseline | Comparison |
|---|---:|---|
| `KdTree.FindNearest @ N=100k` | **0.005 (~200× faster)** | vs `List<T>` linear scan |
| `QuadTree.Query @ N=100k` | **0.009 (~110× faster)** | vs `List<T>` linear scan |
| `OctTree.RadiusQuery @ N=100k` | **0.02 (~50× faster)** | vs `List<T>` linear scan |
| `ObservableList.Add @ N=100k` | **0.41 (~2.4× faster)** | vs `ObservableCollection<T>` |
| `ObservableList.Fill @ N=100k` | **0.30 (~3.3× faster)** | vs `ObservableCollection<T>` |
| `TimelineArray.GetAtTime` | **0.17-0.22 (~5× faster)** | vs `List<(long,T)>` linear scan |
| `BitGrid2D.Fill @ 1024×1024` | **0.10 (~10× faster)** + 8× less memory | vs `bool[,]` |
| `LayeredGrid2D.Fill @ 1024×1024` | **0.22 (~5× faster)** | vs `int[,,]` |
| `HyperLogLog.Add @ N=100k` | **0.60 (~40% faster)** | vs `HashSet<long>` |
| `MinHeap.Insert / MaxHeap.Insert` | **0.84-0.86 (~15% faster)** | vs `PriorityQueue<,>` |
| `BoundedList.Add` | **0.91 (~9% faster)** | vs `List<T>` |

**Honest non-wins (rigorous):** `BloomFilter.Contains` is 2× slower than `HashSet.Contains` (capability play, not speed). `BitGrid2D.Get` is 1.6× slower than `bool[,]` indexing (the win is 8× memory). `MinHeap.ExtractMin` is 1.4-1.8× slower than `PriorityQueue.Dequeue` (Insert is still faster). All Hybrid types (`LinkedDictionary`, `BoundedDictionary`, etc.) are slower than `Dictionary<K,V>` on basic ops — they're capability plays, not speed plays.

## Table of Contents

<details>
<summary>📋 Click to expand table of contents</summary>

- [What's New in v2.0.0](#whats-new-in-v200)
  - [v1.x → v2.0 migration](#v1x--v20-migration)
- [Quick Start](#quick-start)
- [Complexity Guarantees](#complexity-guarantees)
- [Performance Results & Benchmarking](#performance-results--benchmarking)
- [Core Data Structures](#core-data-structures)
  - [Linear Collections](#linear-collections) (6 structures)
  - [Spatial Structures](#spatial-structures) (6 structures)
  - [Hybrid Structures](#hybrid-structures) (9 structures)
  - [Probabilistic Structures](#probabilistic-structures) (5 structures)
  - [Grid Structures](#grid-structures) (3 structures)
  - [Reactive Structures](#reactive-structures) (2 structures)
  - [Temporal Structures](#temporal-structures) (1 structure)
- [Real-World Usage Examples](#real-world-usage-examples)
- [Installation](#installation)
- [Security Considerations](#security-considerations)
- [Choosing the Right Structure](#choosing-the-right-structure)
- [Contributing](#contributing)
- [License](#license)

</details>

## Quick Start

<details>
<summary>🚀 Get up and running in 2 minutes</summary>

### Installation
```bash
dotnet add package OmniCollections
```

### Hello World — PooledQueue (one of several reasonable defaults)
```csharp
using Omni.Collections.Linear;

// PooledQueue rents its backing buffer from ArrayPool<T>.Shared, so
// repeatedly creating short-lived queues amortizes the buffer alloc.
// For a single long-lived queue, plain Queue<T> or BoundedList<T> is fine.
var queue = PooledQueue<Order>.CreateWithArrayPool(capacity: 10000);
queue.Enqueue(new Order("12345"));
var next = queue.Dequeue();
```

### Common Scenarios — Pick Your Win
```csharp
// Spatial queries (games, GIS, collision detection)
var quadTree = new QuadTree<GameObject>(worldBounds);
quadTree.Insert(new Point(x, y), gameObject);
var visible = quadTree.Query(cameraBounds); // O(log n + k)

// Priority processing (job queues, A* pathfinding)
var heap = MinHeap<Task>.CreateWithArrayPool(initialCapacity: 1000);
heap.Insert(urgentTask);
var next = heap.ExtractMin(); // O(log n)

// Bounded memory cache with FIFO auto-eviction
var cache = new BoundedDictionary<string, Data>(capacity: 1000);
cache["key"] = data; // Auto-evicts oldest insert when full
```

### Next Steps
- 📚 Browse [Core Data Structures](#core-data-structures)
- 📊 Reproduce numbers via [Performance Results & Benchmarking](#performance-results--benchmarking)
- 🔧 [Report issues](https://github.com/Codeturion/omni-collections/issues)

</details>

## Complexity Guarantees

| Structure | Insert | Remove | Lookup | Special Operation |
|-----------|--------|--------|--------|-------------------|
| **Spatial** |
| QuadTree | O(log n) | O(log n) | - | Spatial query: O(log n + k) |
| OctTree | O(log n) | O(log n) | - | 3D query: O(log n + k) |
| KdTree | O(log n) | O(log n) | - | k-NN: O(log n) average |
| SpatialHashGrid | O(1) avg | O(1) avg | - | Range query: O(k) |
| TemporalSpatialHashGrid | O(1) avg | O(1) avg | - | Temporal query: O(k), Trajectory: O(t) |
| BloomRTreeDictionary | O(log n) | O(log n) | O(1) | Range query: O(log n + k) with pruning |
| **Linear** |
| PooledQueue | O(1)* | O(1) | - | Enqueue/Dequeue: O(1), batch via Span |
| MinHeap/MaxHeap | O(log n) | O(log n) | O(1) peek | ExtractMin/Max: O(log n) |
| BoundedList | O(1) | O(n) | O(1) | Capacity-bounded |
| PooledList | O(1)* | O(n) | O(1) | ArrayPool integration |
| PooledStack | O(1)* | O(1) | O(1) peek | Push/Pop: O(1) |
| **Hybrid** |
| CounterDictionary | O(1) | O(1) | O(1) | GetFrequency: O(1) |
| LinkedDictionary | O(1) | O(1) | O(1) | LRU access-order |
| QueueDictionary | O(1) | O(1) | O(1) | Dequeue: O(1) FIFO |
| BoundedDictionary | O(1) | O(1) | O(1) | FIFO auto-eviction at capacity |
| DequeDictionary | O(1) | O(1) | O(1) | AddFirst/Last: O(1) |
| ConcurrentLinkedDictionary | O(1) | O(1) | O(1) | Thread-safe LRU |
| LinkedMultiMap | O(1) | O(1) | O(1) | GetValues: O(k) |
| GraphDictionary | O(1) | O(1) | O(1) | AddEdge: O(1), ShortestPath: O(V+E) |
| PredictiveDictionary | O(1) | O(1) | O(1) | GetPredictions: O(1) |
| **Probabilistic** |
| BloomFilter | O(k) | - | O(k) | Zero false negatives |
| CountMinSketch | O(d) | - | O(d) | EstimateCount: O(d) |
| HyperLogLog | O(1) | - | - | EstimateCardinality: O(m) |
| Digest (TDigest) | O(log n) | - | O(1) | Quantile query: O(1) |
| DigestStreamingAnalytics | O(log n) | - | O(1) | Quantile query: O(1) |
| **Grid** |
| BitGrid2D | O(1) | O(1) | O(1) | Bit-packed storage (8× less memory) |
| LayeredGrid2D | O(1) | O(1) | O(1) | Layer operations: O(1) |
| HexGrid2D | O(1) | O(1) | O(1) | Neighbor queries: O(6) |
| **Reactive** |
| ObservableList | O(1)* | O(n) | O(1) | Event notification: O(s) |
| ObservableHashSet | O(1) | O(1) | O(1) | Event notification: O(s) |
| **Temporal** |
| TimelineArray | O(1) | - | O(log n) | Replay range: O(log n + k) |
| TemporalSpatialGrid | O(1) | O(1) | O(1) | Snapshot: O(n), QueryAtTime: O(k) |

*Amortized; k = hash functions or results; d = sketch depth; m = HyperLogLog registers; s = subscribers.

## Performance Results & Benchmarking

Released v2.0.0 reference numbers (rigorous profile, i7-13700KF) live at [`docs/perf/i7-13700KF/rigorous-v2.0.0/`](docs/perf/i7-13700KF/rigorous-v2.0.0/). Standard-profile data (broader coverage) at [`docs/perf/i7-13700KF/standard-v2.0.0/`](docs/perf/i7-13700KF/standard-v2.0.0/). Reproduce against `dev/omni-collections-v2`:

```pwsh
.\bench.ps1 --rigorous --filter '*<TypeName>Benchmarks*'   # rigorous (claim-grade)
.\bench.ps1 --filter '*<TypeName>Benchmarks*'              # standard (broader coverage)
```

Methodology: [`docs/benchmarks.md`](docs/benchmarks.md).

## Core Data Structures

### Linear Collections

<details>
<summary>PooledQueue, MinHeap, MaxHeap, BoundedList, PooledList, PooledStack (6 structures)</summary>

#### PooledQueue&lt;T&gt;
```csharp
var queue = PooledQueue<Order>.CreateWithArrayPool(capacity: 10000);
queue.Enqueue(order);                  // O(1) amortized
var next = queue.Dequeue();            // O(1)

// Span batch APIs Queue<T> doesn't have:
ReadOnlySpan<Order> burst = stackalloc Order[8];
queue.EnqueueSpan(burst);
```
- **Slower than `Queue<T>` on small N** (~1.35× per-op due to mandatory `ThrowIfDisposed` from Phase 3's correctness fix); parity at N=100k.
- **Capability:** `ArrayPool<T>` rental for the backing buffer + `EnqueueSpan` / `DequeueSpan` batch APIs.
- **Use it when** you have many short-lived queues in a hot path and want to amortize buffer allocation, or when you want span-based batch enqueue/dequeue. Plain `Queue<T>` is fine for one long-lived queue.

#### MinHeap&lt;T&gt; / MaxHeap&lt;T&gt;
```csharp
var heap = MinHeap<Task>.CreateWithArrayPool(initialCapacity: 1000);
heap.Insert(task);                     // O(log n)
var urgent = heap.ExtractMin();        // O(log n)
```
- **Insert:** ratio 0.84–0.86 (~15% faster than `PriorityQueue<,>.Enqueue`) across N=1k/10k/100k under rigorous; ~2× less memory per slot.
- **Honest non-win:** `ExtractMin` / `ExtractMax` is 1.4–1.8× slower than `PriorityQueue.Dequeue`. Insert + memory carry the type.
- **Use it when** Insert volume dominates Extract or when you want lower per-element memory.

#### BoundedList&lt;T&gt;
```csharp
var list = new BoundedList<Event>(capacity: 1000);
list.Add(evt);                         // O(1) — throws on overflow
```
- **Add:** ratio 0.91–0.93 (~7–9% faster than `List<T>.Add`) at N=1k/10k/100k. Win comes from preset array vs doubling-and-copy.
- **Use it when** the upper bound is known and you want predictable memory + slightly faster Add.

#### PooledList&lt;T&gt;
```csharp
using var list = PooledList<Item>.CreateWithArrayPool(initialCapacity: 1000);
list.Add(item);                        // O(1) amortized; backing buffer rented from ArrayPool
```
- **Per-op:** parity-ish with `List<T>` (1.0–1.27 across ops). The current `Fill` benchmark exposes single-fill-then-dispose, which doesn't show the pool win.
- **Capability:** `ArrayPool<T>` rental — buffer reuse across many short-lived lists.
- **Use it when** you create+drop many lists per second on a hot path. Plain `List<T>` is fine otherwise.

#### PooledStack&lt;T&gt;
```csharp
using var stack = PooledStack<Item>.CreateWithArrayPool(initialCapacity: 1000);
stack.Push(item);                      // O(1)
var last = stack.Pop();                // O(1)
```
- Same story as PooledList: parity-ish per-op (1.09–1.25), value-prop is buffer rental.

</details>

### Spatial Structures

<details>
<summary>QuadTree, OctTree, KdTree, SpatialHashGrid, TemporalSpatialHashGrid, BloomRTreeDictionary (6 structures)</summary>

#### QuadTree&lt;T&gt;
```csharp
var bounds = new Rectangle(0, 0, 1024, 1024);
var quadTree = new QuadTree<GameObject>(bounds);
quadTree.Insert(new Point(x, y), gameObject);    // O(log n) average

var visible = quadTree.Query(cameraBounds);       // O(log n + k)
var nearest = quadTree.FindNearest(playerPoint);  // O(log n) average
```
- **Query:** ratio 0.009 at N=100k — ~110× faster than `List<T>` linear scan. Algorithmic.
- **Cost:** Fill is 9–285× slower than `List<T>.Add` (build cost of the index). Query repays.
- **Use it when** you query 2D spatial data at N≥10k. Below N≈1k, linear scan is competitive.

#### OctTree&lt;T&gt;
```csharp
var octTree = OctTree<Entity>.Create3D(
    getX: e => e.Position.X,
    getY: e => e.Position.Y,
    getZ: e => e.Position.Z,
    minSize: 1.0f);
octTree.Insert(entity);
var nearby = octTree.FindInSphere(center, radius); // O(log n + k)
var nearest = octTree.FindNearest(targetPos);
```
- **RadiusQuery:** ratio 0.02–0.34 (3–50× faster than linear scan) across N=1k/10k/100k under rigorous.
- **Cost:** Fill is 47–329× slower than `List<T>.Add`.
- **Use it when** you query 3D points or do frustum / radius culling at N≥1k.

#### KdTree&lt;T&gt;
```csharp
var kdTree = KdTree<DataPoint>.Create3D(
    getX: p => p.X,
    getY: p => p.Y,
    getZ: p => p.Z);
kdTree.Insert(point);
var nearest = kdTree.FindNearest(target);          // O(log n) average
var topK   = kdTree.FindNearestK(target, k: 5);    // k-NN
```
- **FindNearest @ N=100k:** ratio **0.005 (~200× faster** than `List<T>` linear scan). Algorithmic.
- **Cost:** Fill is 411–1936× a `List<T>.Add` — kd-tree balancing has a real build cost. FindNearest repays it on the very first query at N≥10k.
- **Use it when** you do many k-NN or nearest-neighbor queries against a relatively static point set.

#### SpatialHashGrid&lt;T&gt;
```csharp
var grid = new SpatialHashGrid<Entity>(cellSize: 64.0f);
grid.Insert(x, y, entity);                                 // O(1) average
var nearby = grid.GetObjectsInRectangle(x0, y0, x1, y1);    // O(k)
foreach (var (a, b) in grid.GetPotentialCollisions()) { … } // O(n) collision pairs
```
- **RadiusQuery:** ratio 0.05–0.12 at N≥10k (8–20× faster than linear scan; standard-profile). Below N=1k it loses to tight-loop overhead (4.31×).
- **Use it when** point density is roughly uniform across the world (bullets, particles).

#### TemporalSpatialHashGrid&lt;T&gt;
```csharp
var temporalGrid = new TemporalSpatialHashGrid<MovingEntity>(
    cellSize:          32.0f,
    snapshotInterval:  TimeSpan.FromSeconds(1),
    historyRetention:  TimeSpan.FromMinutes(10));
temporalGrid.UpdateObject(entity, x, y, vx, vy);

var snapshot   = temporalGrid.GetObjectsInRadiusAtTime(x, y, radius, when: DateTime.UtcNow);
var trajectory = temporalGrid.GetObjectTrajectory(entity, lookBack: TimeSpan.FromMinutes(5));
```
- **RadiusQuery @ N≥10k:** ratio 0.07–0.14 (similar to plain SpatialHashGrid; small temporal-snapshot overhead).
- **Capability:** spatial query *at a past time* and per-object trajectory replay — no BCL equivalent.
- **Cost:** Fill is 27–253× a `List<T>.Add` (extra cost of snapshot indexing).

#### BloomRTreeDictionary&lt;TKey, TValue&gt;
```csharp
var spatialDict = new BloomRTreeDictionary<string, Building>(
    expectedCapacity:   10_000,
    falsePositiveRate:  0.01);
spatialDict.Add("b1", building, new BoundingRectangle(x0, y0, x1, y1));

var hits      = spatialDict.FindIntersecting(searchBounds);  // O(log n + k)
var atPoint   = spatialDict.FindAtPoint(x, y);
var stats     = spatialDict.Statistics;                       // bloom hit-rate telemetry
```
- **Slower than `Dictionary<K,V>` on basic ops:** Add ratio 30–75×, Lookup ratio 1.12–1.28 across N.
- **Capability:** spatial range/point queries on a *keyed* dictionary, with bloom-pre-screen short-circuiting misses.
- **Use it when** you need both `dict[key]` access and "what's intersecting this rectangle?" queries on the same data.

</details>

### Hybrid Structures

<details>
<summary>CounterDictionary, LinkedDictionary, QueueDictionary, BoundedDictionary, DequeDictionary, ConcurrentLinkedDictionary, LinkedMultiMap, GraphDictionary, PredictiveDictionary (9 structures)</summary>

> **All Hybrid types are slower than `Dictionary<K,V>` on basic Add/Lookup.** They ship as capability plays — LRU, LFU, FIFO, multi-value, graph, pattern-prediction, thread-safe LRU. The added structure is the value, not raw speed.

#### BoundedDictionary&lt;TKey, TValue&gt;
```csharp
var cache = new BoundedDictionary<string, Data>(capacity: 1000);
cache["key"] = data;                       // O(1) — auto-evicts oldest INSERT when full
var oldest  = cache.GetOldest();           // O(1)
```
- **Slower than `Dictionary<K,V>`:** Add ratio 1.11×, Lookup ratio 2.09× at N=100k.
- **Capability:** fixed-capacity dictionary with FIFO auto-eviction (oldest insert evicts first).
- **Use it when** you want bounded memory and an auto-evicting cache where eviction is by *insert time*. For LRU access-order eviction use `LinkedDictionary`. For unbounded FIFO with key lookup use `QueueDictionary`.

#### LinkedDictionary&lt;TKey, TValue&gt;
```csharp
var linked = new LinkedDictionary<string, Config>();
linked.AddOrUpdate("setting1", config);
foreach (var kvp in linked) { … }          // LRU access-order on iteration
```
- **Slower than `Dictionary<K,V>`:** Add ratio 2.26×, Lookup ratio 2.79× at N=100k.
- **Capability:** LRU access-order iteration — every Get bumps the key to MRU.
- **Use it when** you want an LRU cache, access-order session list, etc.

#### QueueDictionary&lt;TKey, TValue&gt;
```csharp
var queueDict = new QueueDictionary<string, Message>();
queueDict.Enqueue("msg1", message);
var next = queueDict.Dequeue();            // KeyValuePair<TKey, TValue>
```
- **Slower than `Dictionary<K,V>`:** Add ratio 2.68×, Lookup ratio 1.96× at N=100k.
- **Capability:** unbounded FIFO + key lookup in one structure.

#### DequeDictionary&lt;TKey, TValue&gt;
```csharp
var deque = new DequeDictionary<string, Message>();
deque.PushFront("msg1", a);
deque.PushBack("msg2", b);
var first = deque.PopFront();
```
- **Slower than `Dictionary<K,V>`:** Add ratio 2.24×, Lookup ratio 1.96× at N=100k.
- **Capability:** double-ended queue with key-based lookups (undo/redo, sliding windows).

#### ConcurrentLinkedDictionary&lt;TKey, TValue&gt;
```csharp
var concurrent = new ConcurrentLinkedDictionary<string, Config>();
concurrent.Add("setting", config);         // thread-safe with insertion order
```
- **Slower than (non-thread-safe) `Dictionary<K,V>`:** Add ratio 4.02×, Lookup ratio 5.06× at N=100k.
- **Capability:** **thread-safe LRU** — fine-grained write locking, lock-free reads. (A `ConcurrentDictionary<K,V>` baseline comparison is a Phase 7 follow-up.)

#### LinkedMultiMap&lt;TKey, TValue&gt;
```csharp
var multiMap = new LinkedMultiMap<string, Tag>();
multiMap.Add("item1", tag1);
multiMap.Add("item1", tag2);
var tags = multiMap.GetValues("item1");    // O(1) value list access
```
- **At N=100k:** Add ratio **0.78×** (Add wins at large N), Lookup ratio 3.14×.
- **Capability:** native multiple-values-per-key with insertion order preserved per key.

#### GraphDictionary&lt;TKey, TValue&gt;
```csharp
var graph = new GraphDictionary<string, User>();
graph.Add("alice", aliceData);
graph.AddEdge("alice", "bob", weight: 1.0);
graph.AddBidirectionalEdge("alice", "charlie");
var path = graph.FindShortestPath("alice", "david"); // O(V + E) BFS
```
- **Slower than `Dictionary<K,V>`:** AddNode ratio 1.98×, Lookup ratio 2.78× at N=100k.
- **Capability:** vertex/edge topology + per-vertex value lookup + BFS shortest-path in one structure.

#### CounterDictionary&lt;TKey, TValue&gt;
```csharp
var counter = new CounterDictionary<string, Product>();
counter.IncrementCount("product1");
var hot = counter.GetMostFrequent(10);     // KeyValuePair<TKey, (TValue, long count)>
```
- **Slower than `Dictionary<K,V>`:** Add ratio 5.88×, Lookup ratio 6.59× at N=100k.
- **Capability:** integrated frequency counter — LFU semantics, "top-K most frequent" without a side dictionary.

#### PredictiveDictionary&lt;TKey, TValue&gt;
```csharp
var predictive = new PredictiveDictionary<string, CachedData>();
predictive.AddOrUpdate("user123", userData);
predictive.TryGetValue("user123", out var v);

// Opt-in n-gram pattern recognition: ask for predictions given a context
var contextKeys = new[] { "user123", "user124" };
var predictions = predictive.GetPredictions(contextKeys);

// Explicitly prefetch the predicted keys via your value factory
int prefetched = predictive.PrefetchLikely(contextKeys, key => LoadFromBackingStore(key));
```
- **Slower than `Dictionary<K,V>`:** Add ratio 1.19×, Lookup ratio 6.76× at N=100k.
- **Capability:** n-gram access-pattern recognition you *opt into* via `PrefetchLikely`. The dictionary does not prefetch transparently — you decide when (and with which value factory) to act on `GetPredictions`. Learning is synchronous on each access; no background work.
- **Use it when** access has a sequential pattern (`A → B → C`) and you can supply a value factory that produces values cheaply enough to be worth pre-loading. Use plain `Dictionary` if you don't query `GetPredictions`.

</details>

### Probabilistic Structures

<details>
<summary>BloomFilter, CountMinSketch, HyperLogLog, Digest (TDigest), DigestStreamingAnalytics (5 structures)</summary>

#### BloomFilter&lt;T&gt;
```csharp
var filter = new BloomFilter<string>(expectedItems: 1_000_000, falsePositiveRate: 0.01);
filter.Add("exists");
if (!filter.Contains("checkThis")) { /* definitely not present */ }
```
- **Slower than `HashSet<T>`:** ContainsHit ratio 1.92–2.32×, ContainsMiss ratio 1.32–1.62× across N. Fill at N=100k is **0.63 (37% faster)** because Fill amortizes the hash work across additions.
- **Capability:** **fixed-size memory regardless of N** (HashSet is O(N)). At N=100k the bloom is ~50× smaller.
- **Use it when** memory ceiling matters more than per-op cost (negative-cache pre-screen, "have I seen this before" at billions of items).

#### CountMinSketch&lt;T&gt;
```csharp
var sketch = new CountMinSketch<string>(width: 1024, depth: 4);
sketch.Add("event");
long freq = sketch.EstimateCount("event");
sketch.Merge(otherSketch);
```
- **Add @ N=100k:** ratio **0.71 (29% faster** than `Dictionary<T,int>` baseline). Slower at small N (5–6×).
- **Capability:** ~constant 8 KB memory regardless of unique-key cardinality, vs `Dictionary<T,int>` which grows O(unique). Mergeable across shards.
- **Use it when** unique-key cardinality is huge and approximate counts (within bounded error) are acceptable.

#### HyperLogLog&lt;T&gt;
```csharp
var hll = new HyperLogLog<string>(bucketBits: 14);
hll.Add("unique_item");
long cardinality = hll.EstimateCardinality();
hll.Merge(otherHll);
```
- **Add @ N=100k:** ratio **0.60 (~40% faster** than `HashSet<long>`). Slower at small N (1.6–1.8×).
- **Memory:** constant ~4 KB regardless of N; at N=100k that's ~1500× less memory than `HashSet<long>`.
- **Capability:** distinct-count estimation with bounded error and mergeable shards.

#### Digest (TDigest)
```csharp
var digest = new Digest(compression: 100.0);
digest.Add(latencyMs);
double p99 = digest.Quantile(0.99);
double median = digest.Quantile(0.5);
digest.Merge(otherDigest);
```
- **Slower than the manual workaround** (collect + sort): 87–318× per Add vs `List<double>.Add`. The `List` workaround pays at quantile-time (`O(N log N)` Sort).
- **Capability:** streaming approximate quantiles in bounded memory; mergeable across distributed shards. **No BCL equivalent.**
- **Use it when** you need running percentiles over an unbounded stream (SLA monitoring, latency dashboards).

#### DigestStreamingAnalytics&lt;T&gt;
```csharp
var analytics = new DigestStreamingAnalytics<ResponseTime>(
    windowSize:     TimeSpan.FromMinutes(5),
    valueExtractor: r => r.Milliseconds);

analytics.Add(responseTime);
var p99 = analytics.GetPercentile(0.99);
```
- Same per-op story as `Digest`.
- **Capability:** *windowed* approximate quantile — old samples expire when their window passes.

</details>

### Grid Structures

<details>
<summary>BitGrid2D, LayeredGrid2D, HexGrid2D (3 structures)</summary>

#### BitGrid2D
```csharp
var bitGrid = new BitGrid2D(width: 1024, height: 1024);
bitGrid[x, y] = true;                    // O(1) bit-packed
bitGrid.CopyRowTo(y, destSpan);          // explicit row copy (replaces removed GetRowSpan)
```
- **Fill @ 1024²:** ratio **0.10 (~10× faster** than `bool[,]` Fill) + **8× less memory** structurally.
- **Honest non-win:** per-element `Get` is 1.6–1.75× slower than `bool[,]` indexing — bit-packing has a small per-access cost. Fill amortization wins; per-pixel sampling does not.
- **Use it when** you have a large boolean grid (fog-of-war, collision mask, cellular automaton) and care about memory or bulk-fill speed.

#### LayeredGrid2D&lt;T&gt;
```csharp
var layered = new LayeredGrid2D<int>(width: 100, height: 100, layerCount: 3);
layered[layer, x, y] = value;
```
- **Fill @ 1024²:** ratio **0.22 (~5× faster** than `int[,,]`).
- **Honest non-win:** per-element `Get` is 2.5–2.94× slower than flat-array indexing.

#### HexGrid2D&lt;T&gt;
```csharp
var hex = new HexGrid2D<Tile>();
hex[new HexCoord(q, r)] = tile;
foreach (var cell in hex.GetNeighbors(coord)) { … }   // O(6)
```
- **Slower than `int[,]`** on per-element access (Get ratio 46–67×) — hex coordinate arithmetic is not cheap.
- **Capability:** axial hex coordinates, neighbor traversal, hex pathfinding. **No BCL equivalent.**

</details>

### Reactive Structures

<details>
<summary>ObservableList, ObservableHashSet (2 structures)</summary>

#### ObservableList&lt;T&gt;
```csharp
var list = new ObservableList<Item>();
list.CollectionChanged += OnItemsChanged;
list.ItemAdded += OnItemAdded;       // side-channel per-item event
list.Add(item);                      // O(1)
```
- **Add @ N=100k:** ratio **0.41 (~2.4× faster** than `ObservableCollection<T>.Add`) under rigorous.
- **Fill @ N=100k:** ratio **0.30 (~3.3× faster)**.
- **Use it when** you bind a large collection to a UI (WPF/Avalonia/MAUI) — `ObservableCollection<T>` is the slow default, and v2.0's `RemoveAll` notification doctrine fixes WPF binding mis-positioning.

#### ObservableHashSet&lt;T&gt;
```csharp
var achievements = new ObservableHashSet<string>();
achievements.ItemAdded += OnAchievementUnlocked;
achievements.CollectionChanged += OnAchievementsChanged;
achievements.Add("FirstKill");
```
- **Slower than `HashSet<T>`:** Add ratio 1.15–1.35× across N (notification cost). `Contains` is at parity (1.01–1.07).
- **Capability:** `HashSet` semantics + `INotifyCollectionChanged` in one type. Workaround is `HashSet<T>` + manual event plumbing — this is ~15–35% slower than that workaround but free of plumbing bugs.

</details>

### Temporal Structures

<details>
<summary>TimelineArray, TemporalSpatialGrid (2 structures)</summary>

#### TimelineArray&lt;T&gt;
```csharp
var timeline = new TimelineArray<Event>(capacity: 10_000);
timeline.Record(eventData);                         // O(1) at "now"
timeline.Record(eventData, timestamp: explicitTs);  // O(1) at explicit ts

var atTime = timeline.GetAtTime(targetTimestamp);   // O(log n) binary search
var range  = timeline.Replay(startTime, endTime);   // O(log n + k)
```
- **GetAtTime:** ratio **0.17–0.22 (~5× faster** than `List<(long,T)>` linear scan) under rigorous.
- **Capability:** ring-buffer-backed temporal log with binary-search GetAtTime and forward replay.

#### TemporalSpatialGrid&lt;T&gt;
```csharp
var temporal = TemporalSpatialGrid<Entity>.CreateWithArrayPool(
    capacity:      3600,        // 1 hour at 60 FPS
    cellSize:      64.0f,
    frameDuration: 16,          // milliseconds
    autoRecord:    true);

temporal.Insert(x, y, entity);
temporal.RecordSnapshot();
var atTime = temporal.GetObjectsInRadiusAtTime(x, y, radius, ts);
var hist   = temporal.ReplaySpatialHistory(startTime, endTime);
```
- **Capability:** spatial-hash grid + per-frame snapshots → 4D (space + time) query and replay. **No BCL equivalent.**

</details>

## Real-World Usage Examples

### Unity Game Development

<details>
<summary>Click to expand Unity examples</summary>

*Simplified for illustration. Production code typically uses jobs/Burst/ECS rather than `Update`/`FixedUpdate` for hot paths.*

#### 2D Collision Detection — QuadTree
```csharp
public class CollisionSystem : MonoBehaviour
{
    private QuadTree<Collider2D> _spatialIndex;

    void Start()
    {
        var bounds = new Rectangle(0, 0, worldWidth, worldHeight);
        _spatialIndex = new QuadTree<Collider2D>(bounds);
        foreach (var col in staticColliders)
            _spatialIndex.Insert(new Point(col.transform.position.x, col.transform.position.y), col);
    }

    void CheckPlayerCollisions(Player player)
    {
        var nearby = _spatialIndex.Query(player.bounds);
        foreach (var col in nearby)
            if (Physics2D.OverlapBox(player.position, col))
                HandleCollision(player, col);
    }
}
```

#### Uniform Object Collision — SpatialHashGrid
```csharp
public class BulletHellSystem : MonoBehaviour
{
    private SpatialHashGrid<Bullet> _bulletGrid = new(cellSize: 32f);

    void Update()
    {
        _bulletGrid.Clear();
        foreach (var b in activeBullets)
            _bulletGrid.Insert(b.x, b.y, b);

        var nearby = _bulletGrid.GetObjectsInRectangle(
            player.bounds.min.x, player.bounds.min.y,
            player.bounds.max.x, player.bounds.max.y);
    }
}
```

#### A* Pathfinding — MinHeap
```csharp
public class AStarPathfinder
{
    public List<Vector3> FindPath(Vector3 start, Vector3 goal)
    {
        var openSet = MinHeap<PathNode>.CreateWithArrayPool(initialCapacity: 1024);
        openSet.Insert(new PathNode(start, fScore: 0));

        while (openSet.Count > 0)
        {
            var current = openSet.ExtractMin();
            // …expand neighbors…
        }
        return path;
    }
}
```

#### Decision Cache — BoundedDictionary
```csharp
public class EnemyAI : MonoBehaviour
{
    private BoundedDictionary<string, AIDecision> _decisionCache;

    void Start() { _decisionCache = new BoundedDictionary<string, AIDecision>(capacity: 100); }
}
```

#### Particle Burst — PooledList
```csharp
public class ParticleManager : MonoBehaviour
{
    void EmitBurst(Vector3 position, int count)
    {
        using var particles = PooledList<Particle>.CreateWithArrayPool(initialCapacity: count);
        for (int i = 0; i < count; i++)
            particles.Add(CreateParticle(position));
        RenderParticles(particles);
    } // buffer returned to ArrayPool
}
```

#### Object Pool — PooledQueue
```csharp
public class ObjectPool<T> : IDisposable where T : Component
{
    private readonly PooledQueue<T> _available;
    public ObjectPool(int capacity)
    {
        _available = PooledQueue<T>.CreateWithArrayPool(capacity);
        for (int i = 0; i < capacity; i++) _available.Enqueue(CreateInstance());
    }
    public T Get() => _available.TryDequeue(out var x) ? x : CreateInstance();
    public void Return(T obj) => _available.Enqueue(obj);
    public void Dispose() => _available.Dispose();
}
```

#### Fog of War — BitGrid2D
```csharp
public class FogOfWarSystem : MonoBehaviour
{
    private BitGrid2D _explored;
    private BitGrid2D _visible;

    void Initialize(int w, int h)
    {
        _explored = new BitGrid2D(w, h);   // 8× less memory than bool[w,h]
        _visible  = new BitGrid2D(w, h);
    }

    void RevealArea(int x, int y, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
            if (dx * dx + dy * dy <= radius * radius)
            {
                _visible[x + dx, y + dy]  = true;
                _explored[x + dx, y + dy] = true;
            }
    }
}
```

#### Damage Stats — CountMinSketch
```csharp
public class DamageTracker
{
    private readonly CountMinSketch<string> _bySource = new(width: 2048, depth: 5);

    public void Record(string sourceId, int damage)
    {
        _bySource.Add(sourceId, (uint)damage);
    }

    public long ApproxDamage(string sourceId) => _bySource.EstimateCount(sourceId);
}
```

#### Replay Recorder — TimelineArray
```csharp
public class ReplayRecorder : MonoBehaviour
{
    private TimelineArray<GameState> _timeline;

    void Start() { _timeline = new TimelineArray<GameState>(capacity: 30 * 60); }

    void FixedUpdate() { _timeline.Record(CaptureGameState()); }

    void PlaybackFrom(long ts)
    {
        foreach (var state in _timeline.Replay(ts - 500, ts + 500))
            ApplyInterpolated(state);
    }
}
```

</details>

### High-Throughput Web API Processing

<details>
<summary>🚀 Server-side example with v2.0 type names</summary>

```csharp
using Omni.Collections.Linear;

/// <summary>
/// Web API request processor:
///  - PooledQueue: ArrayPool-backed buffer for the normal lane (parity Q&lt;T&gt; perf, pool reuse).
///  - MinHeap: ~15% faster Insert than PriorityQueue&lt;T&gt;, ~2× less memory per slot.
/// </summary>
public class HighThroughputRequestProcessor : IDisposable
{
    private readonly PooledQueue<ApiRequest>     _normalQueue;
    private readonly MinHeap<PriorityRequest>    _priorityQueue;
    private readonly PooledQueue<ApiRequest>     _deadLetterQueue;

    public HighThroughputRequestProcessor()
    {
        _normalQueue     = PooledQueue<ApiRequest>.CreateWithArrayPool(capacity: 100_000);
        _priorityQueue   = MinHeap<PriorityRequest>.CreateWithArrayPool(initialCapacity: 10_000);
        _deadLetterQueue = PooledQueue<ApiRequest>.CreateWithArrayPool(capacity: 5_000);
    }

    public void EnqueueRequest(ApiRequest request)
    {
        if (request.Priority > 0)
            _priorityQueue.Insert(new PriorityRequest
            {
                Request     = request,
                Priority    = request.Priority,
                EnqueuedAt  = DateTime.UtcNow.Ticks,
            });
        else
            _normalQueue.Enqueue(request);
    }

    public async Task<ProcessingStats> ProcessBatchAsync(int maxBatch = 1000)
    {
        var processed = 0;
        var errors    = 0;
        var startTime = DateTime.UtcNow;

        while (_priorityQueue.Count > 0 && processed < maxBatch / 4)
        {
            var pri = _priorityQueue.ExtractMin();
            try { await ProcessRequest(pri.Request); processed++; }
            catch (Exception ex) { _deadLetterQueue.Enqueue(pri.Request); errors++; LogError(ex, pri.Request); }
        }

        while (_normalQueue.TryDequeue(out var req) && processed < maxBatch)
        {
            try { await ProcessRequest(req); processed++; }
            catch (Exception ex) { _deadLetterQueue.Enqueue(req); errors++; LogError(ex, req); }
        }

        return new ProcessingStats
        {
            ProcessedCount    = processed,
            ErrorCount        = errors,
            ProcessingTimeMs  = (DateTime.UtcNow - startTime).TotalMilliseconds,
            QueueSizes        = new QueueSizes
            {
                Normal     = _normalQueue.Count,
                Priority   = _priorityQueue.Count,
                DeadLetter = _deadLetterQueue.Count,
            },
        };
    }

    private async Task ProcessRequest(ApiRequest r) => await Task.Delay(r.EstimatedProcessingMs);
    private void LogError(Exception ex, ApiRequest r) => Console.WriteLine($"Request {r.Id} failed: {ex.Message}");

    public void Dispose()
    {
        _normalQueue.Dispose();
        _priorityQueue.Dispose();
        _deadLetterQueue.Dispose();
    }
}

public class ApiRequest
{
    public string   Id { get; set; } = "";
    public int      Priority { get; set; }
    public int      EstimatedProcessingMs { get; set; } = 10;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

public class PriorityRequest : IComparable<PriorityRequest>
{
    public ApiRequest Request    { get; set; } = null!;
    public int        Priority   { get; set; }
    public long       EnqueuedAt { get; set; }
    public int CompareTo(PriorityRequest? other)
    {
        if (other is null) return 1;
        var cmp = other.Priority.CompareTo(Priority);
        return cmp != 0 ? cmp : EnqueuedAt.CompareTo(other.EnqueuedAt);
    }
}

public class ProcessingStats
{
    public int    ProcessedCount   { get; set; }
    public int    ErrorCount       { get; set; }
    public double ProcessingTimeMs { get; set; }
    public QueueSizes QueueSizes   { get; set; } = new();
}
public class QueueSizes
{
    public int Normal     { get; set; }
    public int Priority   { get; set; }
    public int DeadLetter { get; set; }
}
```

</details>

### Streaming Analytics

<details>
<summary>Click to expand</summary>

```csharp
// Bounded-memory percentile stream — Digest is the BCL-less capability play.
var latencyTracker = new Digest(compression: 100.0);
foreach (var latency in stream)
{
    latencyTracker.Add(latency);
    if (latencyTracker.Count % 1000 == 0)
        Console.WriteLine($"Current P99: {latencyTracker.Quantile(0.99)} ms");
}
```

</details>

## Installation

<details>
<summary>📦 Package installation and requirements</summary>

### Main Package
```bash
dotnet add package OmniCollections
```

### Focused Packages
```bash
dotnet add package OmniCollections.Spatial         # Spatial indexing
dotnet add package OmniCollections.Linear          # Pooled / bounded linear types
dotnet add package OmniCollections.Hybrid          # Dictionary variants
dotnet add package OmniCollections.Probabilistic   # Bounded-memory analytics
```

### Requirements
- **.NET 8.0** or **netstandard2.1** (libraries multi-target both).
- No external runtime dependencies for core types.
- `System.Reactive` only required for reactive structures.

### Verify Installation
```csharp
using Omni.Collections.Linear;

var queue = PooledQueue<string>.CreateWithArrayPool(100);
queue.Enqueue("Hello Omni.Collections!");
Console.WriteLine(queue.Dequeue());
```

</details>

## Design Philosophy

1. **Algorithmic efficiency over micro-optimization** — better algorithms scale better than faster loops.
2. **Honest performance claims** — every claim is reproducible against `docs/perf/` numbers.
3. **Capability is a valid axis** — types that offer something the BCL doesn't (KNN, hex grids, time-windowed cardinality) win by existence even when slower per-op.
4. **Transparent trade-offs** — the cost is documented next to the benefit in every type's section.

## Security Considerations

<details>
<summary>🔒 Hash collision attacks and security options</summary>

### Hash Collision Attacks
By default, our dictionaries prioritize performance over collision-attack resistance — the same trade-off as `Dictionary<K,V>`. An attacker who controls keys could force O(n²) chain behavior.

```csharp
// Internet-facing or untrusted input
var secure = new LinkedDictionary<string, Data>(
    capacity:    16,
    mode:        CapacityMode.Dynamic,
    loadFactor:  0.75f,
    comparer:    null,
    hashOptions: SecureHashOptions.Production);

// Internal services or trusted data
var fast = new LinkedDictionary<string, Data>();   // default
```

**Recommendation:** enable secure-hashing for public APIs, leave default for internal processing.

</details>

## Choosing the Right Structure

These structures solve specific problems — use them when you have those problems:

- **Start with .NET's built-in collections.** They're well-tuned and simple.
- **Reach for an Omni type** only when you hit a measurable bottleneck (slow spatial queries, GC pressure on hot paths, memory bounds, missing capability like KNN or windowed quantile).
- **`ArrayPool` variants** mostly pay off when you create+drop many short-lived instances per second. For one long-lived instance the rental is overhead.
- **Benchmark your own workload** — relative ratios in `docs/perf/` are i7-13700KF rigorous; your hardware will differ.

## Contributing

<details>
<summary>🤝 How to contribute</summary>

### Report Issues
- **Bug reports:** [issues](https://github.com/Codeturion/omni-collections/issues/new?template=bug_report.md)
- **Feature requests:** [feature template](https://github.com/Codeturion/omni-collections/issues/new?template=feature_request.md)
- **Performance issues:** include benchmark output (`bench.ps1 --rigorous --filter '...'`) with your hardware.

### Discuss
- **Questions:** [GitHub Discussions](https://github.com/Codeturion/omni-collections/discussions)

### Develop
```bash
git clone https://github.com/Codeturion/omni-collections.git
cd omni-collections
dotnet restore
dotnet build -c Release
dotnet test  -c Release
.\bench.ps1 --filter "*YourType*"   # optional: standard-profile bench
```

### Contribution rules
- **Performance changes** ship with before/after numbers from `bench.ps1 --standard` (or `--rigorous` for claims).
- **New types** must justify themselves against a BCL counterpart — either faster on a clear axis, or capability the BCL doesn't have. State the cost in XML docs.
- **Maintain test coverage**, follow project style, include a usage example.

</details>

## License

[PolyForm Noncommercial 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0/)

Free for personal and non-commercial use, including forks and modifications. Commercial use requires permission.

## Acknowledgments

Architecture, design, and implementation by the author. AI coding assistants may have been used for selected development tasks (scaffolding, documentation, test coverage).

---

**Bottom line:** Omni types address two kinds of limit:
- **Algorithmic limits** — when O(n) doesn't scale (spatial / tree / probabilistic).
- **Capability limits** — when the BCL has no equivalent (KNN, hex grids, windowed quantile, thread-safe LRU, n-gram prefetch).

Measure before reaching. Numbers and reproduction commands are in `docs/perf/` and `docs/benchmarks.md`.
