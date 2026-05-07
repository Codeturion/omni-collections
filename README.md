# Omni.Collections

[![NuGet Version](https://img.shields.io/nuget/v/OmniCollections.svg)](https://www.nuget.org/packages/OmniCollections/)
[![Build Status](https://github.com/Codeturion/omni-collections/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/Codeturion/omni-collections/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**32 specialized .NET data structures with documented Big-O bounds, multi-targeted for `net8.0` and `netstandard2.1`.**

## What this library is

32 .NET data structures the BCL doesn't ship: spatial indexes, observable collections, bounded-memory probabilistic estimators, temporal queries, and keyed collections that maintain access order. All public types document Big-O time and space complexity per operation. Multi-targets `net8.0` and `netstandard2.1`; symbol packages and SourceLink ship with every release.

## Install

```bash
dotnet add package OmniCollections
```

The umbrella package pulls in all 8 sub-packages. For trimmed deployments, install only the per-namespace packages you need: `OmniCollections.Linear`, `OmniCollections.Spatial`, `OmniCollections.Hybrid`, `OmniCollections.Probabilistic`, `OmniCollections.Grid`, `OmniCollections.Reactive`, `OmniCollections.Temporal`, `OmniCollections.Core`.

## The 32 collections

Organized by namespace. Click each category to expand.

### Linear

<details><summary><strong>BoundedList, PooledList, PooledStack, PooledQueue, MinHeap, MaxHeap</strong> (6 types)</summary>

#### `BoundedList<T>` — Linear

`List<T>`-shaped collection with a hard upper-bound capacity set at construction. Throws on overflow (or call `TryAdd`).

| Operation | Time | Space |
|---|---|---|
| `Add` | O(1) | O(1) |
| `TryAdd` | O(1) | O(1) |
| `Insert(i, item)` | O(N) | O(1) |
| `Remove(item)` | O(N) | O(1) |
| `RemoveAt(i)` | O(N − i) | O(1) |
| `RemoveLast` | O(1) | O(1) |
| `this[i]` | O(1) | — |
| `Contains` | O(N) | O(1) |
| `Clear` | O(N) | — |
| Storage | — | O(capacity), fixed at construction |

```csharp
var list = new BoundedList<Sample>(capacity: 1024);
list.Add(sample);          // throws if full
if (!list.TryAdd(other)) { /* full */ }
```

**Use when** capacity is known up-front and you want zero resize overhead — real-time loops, fixed-size buffers, GC-conscious paths.
**Don't use when** capacity is unpredictable; pay the resize cost with `List<T>` and skip the throw.

#### `PooledList<T>` — Linear

`List<T>`-shaped collection whose backing array is rented from `ArrayPool<T>` and returned on `Dispose`.

| Operation | Time | Space |
|---|---|---|
| `Add` | O(1) amortized | O(1) |
| `AddRange(span)` | O(n) | O(1) |
| `Insert(i, item)` / `RemoveAt(i)` | O(N − i) | O(1) |
| `RemoveLast` | O(1) | O(1) |
| `this[i]` / `AsSpan` | O(1) | — |
| `IndexOf` / `Contains` | O(N) | O(1) |
| Storage | — | O(capacity), buffer rented from `ArrayPool<T>` |

```csharp
using var list = PooledList<Item>.CreateWithArrayPool(initialCapacity: 1000);
list.Add(item);
ReadOnlySpan<Item> view = list.AsSpan();
```

**Use when** you create and drop many short-lived lists per second — buffer rental amortizes allocation across instances.
**Don't use when** the list is long-lived; the pool rental adds overhead without payoff.

#### `PooledStack<T>` — Linear

LIFO stack whose backing array is rented from `ArrayPool<T>`. Adds `Span<T>` batch push/pop on top of the standard `Stack<T>` shape.

| Operation | Time | Space |
|---|---|---|
| `Push` | O(1) amortized | O(1) |
| `Pop` | O(1) | O(1) |
| `Peek` | O(1) | — |
| `PushSpan(span)` / `PopSpan(count)` | O(n) | O(1) |
| Storage | — | O(capacity), buffer rented from `ArrayPool<T>` |

```csharp
using var stack = PooledStack<Item>.CreateWithArrayPool(initialCapacity: 1000);
stack.Push(item);
ReadOnlySpan<Item> popped = stack.PopSpan(8);
```

**Use when** you create and drop many short-lived stacks, or want span-based batch push/pop the BCL `Stack<T>` doesn't expose.
**Don't use when** the stack is long-lived and you don't use the span APIs — `Stack<T>` is simpler.

#### `PooledQueue<T>` — Linear

FIFO queue whose backing array is rented from `ArrayPool<T>`. Adds `Span<T>` batch enqueue/dequeue on top of the standard `Queue<T>` shape.

| Operation | Time | Space |
|---|---|---|
| `Enqueue` | O(1) amortized | O(1) |
| `Dequeue` | O(1) | O(1) |
| `Peek` | O(1) | — |
| `EnqueueSpan(span)` / `DequeueSpan(count)` | O(n) | O(1) |
| Storage | — | O(capacity), buffer rented from `ArrayPool<T>` |

```csharp
using var queue = PooledQueue<Order>.CreateWithArrayPool(capacity: 10_000);
queue.Enqueue(order);
ReadOnlySpan<Order> burst = queue.DequeueSpan(8);
```

**Use when** queue instances churn (per-request, per-frame) and you want buffer reuse across them, or you need span-batch enqueue/dequeue.
**Don't use when** a single long-lived queue suffices — `Queue<T>` is simpler and at parity for that case.

#### `MinHeap<T>` — Linear

Binary min-heap over `T : IComparable<T>`. Smallest element always at the root. Optional `ArrayPool<T>` rental.

| Operation | Time | Space |
|---|---|---|
| `Insert` | O(log N) | O(1) |
| `ExtractMin` | O(log N) | O(1) |
| `PeekMin` | O(1) | — |
| `InsertRange` | O(n log N) | O(1) |
| `BuildHeap(array)` | O(N) (Floyd build) | O(1) |
| Storage | — | O(capacity) |

```csharp
var heap = MinHeap<Task>.CreateWithArrayPool(initialCapacity: 1000);
heap.Insert(task);
var urgent = heap.ExtractMin();
```

**Use when** Insert volume dominates Extract, you need linear-time bulk-build via `BuildHeap`, or you want pooled backing memory.
**Don't use when** you only need rare priority extraction over a stable set — the BCL `PriorityQueue<,>` is fine.

#### `MaxHeap<T>` — Linear

Binary max-heap over `T : IComparable<T>`. Largest element always at the root. Optional `ArrayPool<T>` rental.

| Operation | Time | Space |
|---|---|---|
| `Insert` | O(log N) | O(1) |
| `ExtractMax` | O(log N) | O(1) |
| `PeekMax` | O(1) | — |
| `InsertRange` | O(n log N) | O(1) |
| `BuildHeap(array)` | O(N) (Floyd build) | O(1) |
| Storage | — | O(capacity) |

```csharp
var heap = MaxHeap<int>.CreateWithArrayPool(initialCapacity: 1000);
heap.Insert(score);
var top = heap.ExtractMax();
```

**Use when** you need top-K / largest-first ordering — `PriorityQueue<,>` requires inverted comparers for max-heap semantics.
**Don't use when** you only ever peek; sort once and index from the end of the array.

</details>

### Spatial

<details><summary><strong>QuadTree, OctTree, KdTree, SpatialHashGrid, TemporalSpatialHashGrid, BloomRTreeDictionary</strong> (6 types)</summary>

#### `QuadTree<T>` — Spatial

2D point-keyed tree that subdivides space into four quadrants per node. Returns items intersecting an axis-aligned query rectangle.

| Operation | Time | Space |
|---|---|---|
| `Insert(point, item)` | O(log N) avg, O(N) worst (degenerate / colocated) | O(1) |
| `Remove(point, item)` | O(log N) avg | O(1) |
| `Query(rect)` | O(log N + k), k = result size | O(k) |
| `FindNearest(point)` | O(log N) avg | O(1) |
| Storage | — | O(N) |

```csharp
var tree = new QuadTree<GameObject>(new Rectangle(0, 0, 1024, 1024));
tree.Insert(new Point(x, y), gameObject);
List<GameObject> visible = tree.Query(cameraBounds);
```

**Use when** you have many 2D points and the dominant query is "what's inside this rectangle?" — collision broad phase, viewport culling, GIS overlays.
**Don't use when** points move every frame; rebuild cost outweighs query savings unless you batch-rebuild.

#### `OctTree<T>` — Spatial

3D analog of `QuadTree` — subdivides space into eight octants per node. Supports sphere, AABB, and frustum queries.

| Operation | Time | Space |
|---|---|---|
| `Insert(item)` | O(log N) avg, O(N) worst | O(1) |
| `FindInSphere(center, radius)` | O(log N + k) | O(k) |
| `FindInBounds(aabb)` | O(log N + k) | O(k) |
| `FindInFrustum(planes)` | O(log N + k) | O(k) |
| `FindNearest(point)` | O(log N) avg | O(1) |
| `Clear` | O(N) | — |
| Storage | — | O(N) |

```csharp
var tree = OctTree<Entity>.Create3D(e => e.X, e => e.Y, e => e.Z, minSize: 1.0f);
tree.Insert(entity);
List<Entity> visible = tree.FindInFrustum(camera.Frustum);
```

**Use when** you query a 3D point set by sphere / AABB / view frustum — 3D culling, range pickup, proximity AI.
**Don't use when** all points lie roughly in a 2D plane; `QuadTree` halves the bookkeeping per node.

#### `KdTree<T>` — Spatial

K-dimensional tree for nearest-neighbor and range queries on multi-dimensional points.

| Operation | Time | Space |
|---|---|---|
| `Insert` | O(log N) avg, O(N) worst (degenerate input) | O(1) |
| `FindNearest` | O(log N) avg | O(1) |
| `FindNearestK(k)` | O(k log N) avg | O(k) |
| `RangeQuery` | O(N^(1−1/d) + r) where r = result size | O(r) |
| `Clear` | O(N) | — |

```csharp
var tree = KdTree<DataPoint>.Create3D(p => p.X, p => p.Y, p => p.Z);
tree.Insert(point);
var nearest = tree.FindNearest(query);
```

**Use when** the point set is read-mostly: build once, query many times.
**Don't use when** points churn faster than you query — incremental inserts skew the tree, eventually forcing a rebalance pass.

#### `SpatialHashGrid<T>` — Spatial

Uniform-cell hash grid keyed by 2D coordinates. Each cell holds a list of items currently inside it.

| Operation | Time | Space |
|---|---|---|
| `Insert(x, y, item)` | O(1) avg | O(1) |
| `Remove(x, y, item)` | O(1) avg | O(1) |
| `GetObjectsAt(x, y)` | O(c), c = items in that cell | O(c) |
| `GetObjectsInRadius(x, y, r)` | O((r/cellSize)² · c) | O(k) |
| `GetObjectsInRectangle(...)` | O((w·h/cellSize²) · c) | O(k) |
| `GetPotentialCollisions()` | O(N + pairs) | O(pairs) |
| `Clear` | O(occupied cells) | — |
| Storage | — | O(N + occupied cells) |

```csharp
var grid = new SpatialHashGrid<Entity>(cellSize: 64.0f);
grid.Insert(x, y, entity);
foreach (var e in grid.GetObjectsInRadius(px, py, 100f)) { /* ... */ }
```

**Use when** point density is roughly uniform across the world (bullets, particles, units on an even map) and the query radius is on the order of a few cells.
**Don't use when** density is wildly skewed (most items in 1% of the world) — a single cell becomes the bottleneck. A `QuadTree` handles that better.

#### `TemporalSpatialHashGrid<T>` — Spatial

`SpatialHashGrid<T>` plus a time-stamped snapshot ring. Lets you query the grid as it existed at a prior time, replay an object's trajectory, or extrapolate from velocity.

| Operation | Time | Space |
|---|---|---|
| `UpdateObject` / `RemoveObject` | O(1) avg | O(1) |
| `GetObjectsInRadius` (live) | O((r/cellSize)² · c) | O(k) |
| `GetObjectsInRadiusAtTime(when)` | O(snapshots) + O((r/cellSize)² · c) | O(k) |
| `GetObjectTrajectory(obj, lookBack)` | O(snapshots) | O(snapshots) |
| `GetObjectsAlongPath(...)` | O(path length / cellSize · c) | O(k) |
| Storage | — | O(N + retained snapshots × snapshot size) |

```csharp
var grid = new TemporalSpatialHashGrid<Mob>(
    cellSize: 32f, snapshotInterval: TimeSpan.FromSeconds(1),
    historyRetention: TimeSpan.FromMinutes(10));
grid.UpdateObject(mob, x, y, vx, vy);
var past = grid.GetObjectsInRadiusAtTime(x, y, 50f, DateTime.UtcNow.AddMinutes(-1));
```

**Use when** you need spatial queries that look back in time (replay debugging, lag compensation, post-hoc audit).
**Don't use when** you only ever query "now" — the snapshot ring is dead weight; use `SpatialHashGrid<T>`.

#### `BloomRTreeDictionary<TKey, TValue>` — Spatial

Keyed dictionary whose values also live in an R-tree indexed by per-entry bounding rectangles. A bloom filter pre-screens key lookups against the negative path.

| Operation | Time | Space |
|---|---|---|
| `Add(key, value, bounds)` | O(log N) avg (R-tree insert) | O(1) |
| `this[key]` (get) | O(1) avg | — |
| `TryGetValue(key)` | O(1) avg | O(1) |
| `Remove(key)` | O(log N) avg | O(1) |
| `FindIntersecting(bounds)` | O(log N + k) | O(k) |
| `FindContained(bounds)` | O(log N + k) | O(k) |
| `FindAtPoint(x, y)` | O(log N + k) | O(k) |
| `Clear` | O(N) | — |
| Storage | — | O(N) for dict + R-tree + bloom bits |

```csharp
var dict = new BloomRTreeDictionary<string, Building>(
    expectedCapacity: 10_000, falsePositiveRate: 0.01);
dict.Add("b1", building, new BoundingRectangle(x0, y0, x1, y1));
var hits = dict.FindIntersecting(searchBounds);
```

**Use when** the same data needs both `dict[key]` access and "what's intersecting this region?" queries.
**Don't use when** you only need one of the two access patterns — pay only for what you use with a plain `Dictionary` or a standalone R-tree.

</details>

### Hybrid

<details><summary><strong>BoundedDictionary, LinkedDictionary, QueueDictionary, DequeDictionary, CounterDictionary, GraphDictionary, LinkedMultiMap, ConcurrentLinkedDictionary, PredictiveDictionary</strong> (9 types)</summary>

#### `BoundedDictionary<TKey, TValue>` — Hybrid (FIFO eviction)

Fixed-capacity dictionary backed by a ring buffer. When full, the next `Add` evicts the oldest *insert* (FIFO order, not access order).

| Operation | Time | Space |
|---|---|---|
| `Add` / `this[key] =` | O(1) avg, O(N) worst (collision) | O(1) |
| `TryGetValue` | O(1) avg — does not touch eviction order | O(1) |
| `ContainsKey` | O(1) avg | O(1) |
| `Remove` | O(1) avg | O(1) |
| `GetOldest` / `GetNewest` | O(1) avg | — |
| `Clear` | O(capacity) | — |
| Storage | — | O(capacity), fixed at construction |

```csharp
var cache = new BoundedDictionary<string, Data>(capacity: 1000);
cache["k"] = data;                      // evicts oldest insert when full
var oldest = cache.GetOldest();
```

**Use when** you want a bounded cache and eviction by *insert age* is correct (rolling event log, recent-N buffer).
**Don't use when** you want LRU semantics — reuse should reset the eviction clock; use `LinkedDictionary` instead.

#### `LinkedDictionary<TKey, TValue>` — Hybrid (LRU)

`Dictionary<K,V>` that maintains an access-order linked list. Reads move the entry to the front; the back is the eviction candidate when used as a fixed-capacity LRU cache.

| Operation | Time | Space |
|---|---|---|
| `AddOrUpdate` | O(1) avg, O(N) worst (collision) | O(1) |
| `TryGetValue` | O(1) avg — *mutates LRU order* | O(1) |
| `ContainsKey` | O(1) avg — does not touch LRU order | O(1) |
| `this[key]` (get) | O(1) avg — *mutates LRU order* | — |
| `Remove` | O(1) avg | O(1) |
| `Clear` | O(N) | — |
| Storage | — | O(N), one entry + two list pointers per key |

```csharp
var lru = new LinkedDictionary<string, byte[]>(capacity: 1000, CapacityMode.Fixed);
lru.AddOrUpdate("k", payload);
if (lru.TryGetValue("k", out var v)) { /* "k" is now most-recently-used */ }
```

**Use when** you want a single-threaded LRU cache without the `MemoryCache` weight class.
**Don't use when** iteration must not perturb recency — `ContainsKey` is the side-effect-free probe; `TryGetValue` and the indexer both touch order.

#### `QueueDictionary<TKey, TValue>` — Hybrid (FIFO)

FIFO queue with O(1) key lookup against the same backing storage. `Dequeue` removes from the head; key access does not reorder.

| Operation | Time | Space |
|---|---|---|
| `Enqueue(key, value)` | O(1) avg | O(1) |
| `Dequeue` | O(1) | O(1) |
| `TryDequeue` | O(1) | O(1) |
| `PeekFront` / `PeekBack` | O(1) | — |
| `TryGetValue` | O(1) avg — does not touch FIFO order | O(1) |
| `ContainsKey` | O(1) avg | O(1) |
| `Remove(key)` | O(1) avg | O(1) |
| `Clear` | O(N) | — |
| Storage | — | O(N) |

```csharp
var q = new QueueDictionary<string, Message>();
q.Enqueue("msg1", message);
var head = q.Dequeue();          // KeyValuePair<string, Message>
var lookup = q["msg1"];          // throws if Dequeue already removed it
```

**Use when** you process items in FIFO order but also need to dedupe / look up by key (job queues with idempotent IDs, message inboxes).
**Don't use when** you only need queue semantics; `Queue<T>` is leaner without the dictionary overhead.

#### `DequeDictionary<TKey, TValue>` — Hybrid (Deque)

Double-ended queue with O(1) key lookup. Push and pop on either end; explicit `MoveToFront` / `MoveToBack` for re-prioritizing.

| Operation | Time | Space |
|---|---|---|
| `PushFront(key, value)` | O(1) avg | O(1) |
| `PushBack(key, value)` | O(1) avg | O(1) |
| `PopFront` / `PopBack` | O(1) | O(1) |
| `PeekFront` / `PeekBack` | O(1) | — |
| `TryGetValue` | O(1) avg — does not touch order | O(1) |
| `MoveToFront(key)` | O(1) avg | O(1) |
| `MoveToBack(key)` | O(1) avg | O(1) |
| `Remove(key)` | O(1) avg | O(1) |
| `Clear` | O(N) | — |
| Storage | — | O(N) |

```csharp
var dq = new DequeDictionary<string, Step>();
dq.PushFront("a", first);
dq.PushBack("b", last);
var oldest = dq.PopFront();
```

**Use when** you need both ends of a queue and key-based lookup (undo/redo stacks, sliding windows, bidirectional cursors).
**Don't use when** you only push/pop one end; `QueueDictionary` halves the pointer bookkeeping.

#### `CounterDictionary<TKey, TValue>` — Hybrid (LFU)

Dictionary that tracks per-key access frequency in a frequency-bucket linked list. Supports "top-K most frequent" and "least frequent" in time linear in K, not N.

| Operation | Time | Space |
|---|---|---|
| `AddOrUpdate` | O(1) avg | O(1) |
| `TryGetValue` | O(1) avg — *mutates frequency count* | O(1) |
| `TryPeek` | O(1) avg — does not touch counts | O(1) |
| `IncrementCount(key)` | O(1) avg | O(1) |
| `GetMostFrequent(k)` | O(k) | O(k) |
| `GetLeastFrequent(k)` | O(k) | O(k) |
| `RemoveLeastFrequent` | O(1) | O(1) |
| `Remove(key)` | O(1) avg | O(1) |
| `Clear` | O(N) | — |
| Storage | — | O(N + distinct frequency values) |

```csharp
var counter = new CounterDictionary<string, Product>();
counter.AddOrUpdate("p1", product);
counter.IncrementCount("p1");
foreach (var kv in counter.GetMostFrequent(10)) { /* hot keys */ }
```

**Use when** you need LFU eviction or "top-K most accessed" without sorting all keys.
**Don't use when** you only need raw counts — a `Dictionary<TKey, int>` is half the size and faster per-op.

#### `GraphDictionary<TKey, TValue>` — Hybrid (graph + value store)

Vertex-keyed dictionary plus weighted directed edges. BFS shortest-path, distance-bounded reachability, and Tarjan SCC built on top.

| Operation | Time | Space |
|---|---|---|
| `Add(key, value)` / `TryGetValue` | O(1) avg | O(1) |
| `Remove(key)` | O(deg(key)) | O(1) |
| `AddEdge` / `RemoveEdge` / `HasEdge` | O(1) avg | O(1) |
| `GetNeighbors(key)` | O(deg(key)) | O(deg) |
| `FindShortestPath` (unweighted BFS) | O(V + E) | O(V) |
| `FindNodesWithinDistance` | O(V + E) | O(V) |
| `FindStronglyConnectedComponents` | O(V + E) | O(V) |
| Storage | — | O(V + E) |

```csharp
var g = new GraphDictionary<string, User>();
g.Add("alice", aliceUser);
g.AddEdge("alice", "bob", weight: 1.0);
var path = g.FindShortestPath("alice", "carol");
```

**Use when** vertex values, topology, and graph algorithms need to live in one structure (social graphs, dependency DAGs, route maps).
**Don't use when** you have only edges, no per-vertex values; use a plain adjacency `Dictionary<TKey, List<TKey>>`.

#### `LinkedMultiMap<TKey, TValue>` — Hybrid (multi-value)

Dictionary where each key maps to an *ordered* list of values. Append per key is O(1); insertion order within a key is preserved.

| Operation | Time | Space |
|---|---|---|
| `Add(key, value)` | O(1) avg | O(1) |
| `this[key]` (get) | O(1) avg, returns `IReadOnlyList<TValue>` | — |
| `TryGetValues(key)` | O(1) avg | O(1) |
| `RemoveKey(key)` | O(values for that key) | O(1) |
| `Remove(key, value)` | O(values for that key) | O(1) |
| `ContainsKey` | O(1) avg | O(1) |
| `Contains(key, value)` | O(values for that key) | O(1) |
| `GetValueCount(key)` | O(1) | — |
| `Clear` | O(total values) | — |
| Storage | — | O(keys + total values) |

```csharp
var mm = new LinkedMultiMap<string, Tag>();
mm.Add("photo1", tag1);
mm.Add("photo1", tag2);
IReadOnlyList<Tag> tags = mm["photo1"];
```

**Use when** one key naturally maps to many values and per-key insertion order matters (tag bags, event streams partitioned by topic).
**Don't use when** values per key are unbounded and you query "give me the i-th value" — the per-key list is singly linked, so positional access is O(i).

#### `ConcurrentLinkedDictionary<TKey, TValue>` — Hybrid (thread-safe LRU)

Thread-safe `LinkedDictionary` variant. Per-bucket fine-grained locks for writes; reads acquire the bucket lock briefly to update the access timestamp used for eviction ordering.

| Operation | Time | Space |
|---|---|---|
| `AddOrUpdate` | O(1) avg | O(1) |
| `TryGetValue` | O(1) avg — *mutates per-node access timestamp* | O(1) |
| `ContainsKey` | O(1) avg — *mutates per-node access timestamp* | O(1) |
| `TryRemove` | O(1) avg | O(1) |
| `this[key]` (get) | O(1) avg — *mutates per-node access timestamp* | — |
| `Clear` | O(N) under write lock | — |
| Storage | — | O(N) + one lock per bucket |

```csharp
var cache = new ConcurrentLinkedDictionary<string, byte[]>(
    capacity: 10_000, CapacityMode.Fixed);
cache.AddOrUpdate("k", payload);
if (cache.TryGetValue("k", out var v)) { /* concurrent-safe */ }
```

**Use when** multiple threads share an LRU cache and you don't want one global lock around it.
**Don't use when** you only have one writer thread — `LinkedDictionary` skips the lock cost.

#### `PredictiveDictionary<TKey, TValue>` — Hybrid (n-gram prefetch)

Dictionary that records short n-gram access patterns. `GetPredictions(context)` returns keys likely to follow; `PrefetchLikely(context, factory)` hydrates them through a caller-supplied value factory. Pattern learning happens synchronously on each access.

| Operation | Time | Space |
|---|---|---|
| `AddOrUpdate` | O(1) avg + O(1) pattern update | O(1) |
| `TryGetValue` | O(1) avg + O(1) pattern update | O(1) |
| `this[key]` (get) | O(1) avg + O(1) pattern update | — |
| `GetPredictions(context)` | O(p), p = predictions for that context | O(p) |
| `PrefetchLikely(context, factory)` | O(p · factory cost) | O(p) |
| `Remove` | O(1) avg | O(1) |
| `Clear` | O(N + patterns) | — |
| Storage | — | O(N + capped pattern table) |

```csharp
var d = new PredictiveDictionary<string, CachedRow>();
d.AddOrUpdate("user123", row);
var ctx = new[] { "user123", "user124" };
int prefetched = d.PrefetchLikely(ctx, key => LoadFromDb(key));
```

**Use when** access has a sequential pattern (`A → B → C`) and the value factory is cheap enough that prefetching pays off.
**Don't use when** access is random or the factory is expensive — pattern bookkeeping is dead weight; use `Dictionary<K,V>`.

</details>

### Probabilistic

<details><summary><strong>BloomFilter, CountMinSketch, HyperLogLog, Digest, DigestStreamingAnalytics</strong> (5 types)</summary>

#### `BloomFilter<T>` — Probabilistic

Space-efficient set membership test with a tuneable false-positive rate and zero false negatives.

| Operation | Time | Space |
|---|---|---|
| `Add` | O(k), k = hash-function count | O(1) |
| `Contains` | O(k) | O(1) |
| `AddRange(span)` | O(n·k) | O(1) |
| `Clear` | O(m), m = bit-array size | — |
| Storage | — | O(m) bits, m = ⌈−N·ln(p) / (ln 2)²⌉ |

Where `N` is `expectedItems` and `p` is the configured false-positive rate. Memory does not grow with items inserted — undersized filters degrade FPR, they do not allocate.

```csharp
var filter = new BloomFilter<string>(expectedItems: 1_000_000, falsePositiveRate: 0.01);
filter.Add("token");
if (filter.Contains("other")) { /* probably yes; verify against source if it matters */ }
```

**Use when** the set is large enough that a `HashSet<T>` is too expensive to hold in RAM, and false positives are acceptable.
**Don't use when** the set fits in `HashSet<T>` comfortably — `HashSet.Contains` is faster per op and false-positive-free.

#### `CountMinSketch<T>` — Probabilistic

Frequency sketch with bounded over-estimation. Width and depth set the error envelope: estimated count is between true count and true count + ε·N with probability 1 − δ.

| Operation | Time | Space |
|---|---|---|
| `Add(item)` / `Add(item, count)` | O(d), d = depth | O(1) |
| `EstimateCount(item)` | O(d) | O(1) |
| `EstimateFrequency(item)` | O(d) | O(1) |
| `IsHeavyHitter(item, threshold)` | O(d) | O(1) |
| `Merge(other)` | O(width · depth) | O(1) |
| `Scale(factor)` | O(width · depth) | — |
| `Clear` | O(width · depth) | — |
| Storage | — | O(width · depth) `uint` cells |

```csharp
var sketch = new CountMinSketch<string>(width: 1024, depth: 4);
sketch.Add("event");
uint approx = sketch.EstimateCount("event");
sketch.Merge(otherShard);
```

**Use when** unique-key cardinality is huge and approximate counts (with one-sided over-estimate error) are acceptable — telemetry, top-K heavy hitters, log analysis. Mergeable across shards.
**Don't use when** exact counts matter; a `Dictionary<T, int>` is exact and simpler when it fits in RAM.

#### `HyperLogLog<T>` — Probabilistic

Cardinality estimator. Estimates distinct-element count with bounded relative error using ~`2^bucketBits` bytes of storage regardless of N.

| Operation | Time | Space |
|---|---|---|
| `Add(item)` | O(1) | O(1) |
| `AddRange(span)` | O(n) | O(1) |
| `EstimateCardinality()` | O(m) on first call after change, O(1) cached | O(1) |
| `Merge(other)` | O(m) | O(1) |
| `EstimateUnion(other)` / `EstimateIntersection(other)` | O(m) | O(1) |
| `Clear` | O(m) | — |
| Storage | — | O(m) bytes, m = 2^bucketBits |

Standard error is ~1.04 / √m.

```csharp
var hll = new HyperLogLog<string>(bucketBits: 14);
hll.Add("user-id-42");
long unique = hll.EstimateCardinality();
hll.Merge(otherShard);
```

**Use when** you need distinct-count over an unbounded stream and approximate is fine — unique-visitor count, distinct-IP, distinct-URL.
**Don't use when** you need exact cardinality — `HashSet<T>.Count` is exact when memory permits.

#### `Digest` — Probabilistic (TDigest)

Streaming approximate quantile sketch. Compresses observations into a bounded set of weighted centroids; quantile error is small near tails (P99, P999) and slightly larger near the median.

| Operation | Time | Space |
|---|---|---|
| `Add(value)` / `Add(value, weight)` | O(log c) avg, c = centroid count | O(1) amortized |
| `Quantile(q)` / `Percentile(p)` | O(log c) | O(1) |
| `Cdf(x)` | O(log c) | O(1) |
| `Merge(other)` | O(c₁ + c₂) | O(1) |
| `Compress` | O(c log c) | — |
| `Clone` | O(c) | O(c) |
| `Clear` | O(c) | — |
| Storage | — | O(compression), typically c ≤ ~`compression` centroids |

```csharp
var d = new Digest(compression: 100.0);
d.Add(latencyMs);
double p99 = d.Quantile(0.99);
d.Merge(otherShard);
```

**Use when** you need running percentiles over an unbounded stream in bounded memory — SLA monitoring, latency dashboards, distributed shards merged at query time.
**Don't use when** the dataset is small enough to sort in memory; sort + index is exact and simpler.

#### `DigestStreamingAnalytics<T>` — Probabilistic (windowed)

`Digest` over a sliding time window. Old observations expire when their window passes; quantile queries operate on the live window only.

| Operation | Time | Space |
|---|---|---|
| `Add(item, ts?)` | O(log c) amortized | O(1) |
| `AddRange(items)` | O(n log c) | O(1) |
| `GetPercentile(p)` | O(log c) | O(1) |
| `GetPercentiles([...])` | O(p · log c) | O(p) |
| `GetAnalytics()` | O(log c) | O(1) |
| `Merge(other)` | O(c₁ + c₂) | O(1) |
| `Clear` | O(c) | — |
| Storage | — | O(compression) for the digest + bounded recent-value buffer |

```csharp
var stream = new DigestStreamingAnalytics<Sample>(
    windowSize: TimeSpan.FromMinutes(5),
    valueExtractor: s => s.Milliseconds);
stream.Add(sample);
double p99 = stream.GetPercentile(99.0);
```

**Use when** you want "P99 over the last 5 minutes" rather than lifetime — SLO dashboards, alert thresholds, anomaly detection.
**Don't use when** you want lifetime quantiles — `Digest` is leaner without the windowing machinery.

</details>

### Grid

<details><summary><strong>BitGrid2D, LayeredGrid2D, HexGrid2D</strong> (3 types)</summary>

#### `BitGrid2D` — Grid

Boolean 2D grid backed by a bit-packed `ulong` array — one bit per cell. Bulk set operations (`And`, `Or`, `Xor`, area fill) operate on whole 64-bit words.

| Operation | Time | Space |
|---|---|---|
| `this[x, y]` (get/set) | O(1) | O(1) |
| `Toggle(x, y)` | O(1) | O(1) |
| `FillArea(x, y, w, h, value)` | O(w · h / 64) | — |
| `SetAll(value)` | O(W·H / 64) | — |
| `And` / `Or` / `Xor` (other) | O(W·H / 64) | — |
| `CountSetBits` | O(W·H / 64) | O(1) |
| `CopyRowTo(y, span)` | O(W) | O(1) |
| `EnumerateSetBits` | O(set bits + W·H / 64) | O(1) |
| Storage | — | ⌈W · H / 8⌉ bytes |

```csharp
using var fog = new BitGrid2D(width: 1024, height: 1024);
fog[x, y] = true;
fog.Or(otherMask);
```

**Use when** the grid is large and Boolean — fog-of-war, collision masks, cellular automata, bitmap font glyphs.
**Don't use when** values are non-Boolean; use `LayeredGrid2D<T>` or a flat `T[]`.

#### `LayeredGrid2D<T>` — Grid

2D grid with N parallel layers stored as a single contiguous `T[]`. Per-layer fill, copy, and span access. Indexer with two coordinates targets layer 0; the three-coordinate indexer takes a layer.

| Operation | Time | Space |
|---|---|---|
| `this[x, y]` (layer 0) | O(1) | — |
| `this[layer, x, y]` | O(1) | — |
| `FillArea(x, y, w, h, value)` (layer 0) | O(w · h) | — |
| `FillLayerArea(layer, x, y, w, h, v)` | O(w · h) | — |
| `FillLayer(layer, value)` | O(W · H) | — |
| `ClearLayer(layer)` | O(W · H) | — |
| `CopyLayer(src, dst)` | O(W · H) | — |
| `GetRowSpan(y)` (layer 0) | O(1) | — |
| `GetLayerRowSpan(layer, y)` | O(1) | — |
| Storage | — | O(W · H · layerCount), one contiguous buffer |

```csharp
using var map = new LayeredGrid2D<int>(width: 256, height: 256, layerCount: 3);
map[layer: 0, x: 5, y: 7] = terrainId;
map.FillLayer(layer: 1, value: 0);
```

**Use when** several aligned 2D grids share a coordinate system (terrain + decoration + collision; foreground/background tiles) and you want them in one allocation.
**Don't use when** layers have different sizes or coordinate systems — they don't share the contiguous buffer.

#### `HexGrid2D<T>` — Grid

Sparse hexagonal grid keyed by axial `HexCoord(q, r)`. Supports neighbor lookup (six directions), distance, ring traversal, line interpolation, and A* pathfinding via a caller-supplied movement-cost function.

| Operation | Time | Space |
|---|---|---|
| `this[coord]` (get/set) / `Set` / `Contains` / `Remove` | O(1) avg | — |
| `GetNeighbors(coord)` | O(6) | O(6) |
| `GetWithinDistance(center, d)` | O(d²) | O(d²) |
| `GetRing(center, d)` / `GetLine(start, end)` | O(d) / O(distance) | O(d) / O(distance) |
| `FindPath(start, goal, costFn)` | O((V + E) log V) A* | O(V) |
| `GetReachable(start, points, costFn)` | O((V + E) log V) bounded by `points` | O(V) |
| Storage | — | O(N) cells |

```csharp
var hex = new HexGrid2D<Tile>();
hex[new HexCoord(q: 0, r: 0)] = startTile;
foreach (var c in hex.GetNeighbors(new HexCoord(0, 0))) { /* six neighbors */ }
var path = hex.FindPath(start, goal, costFn: c => c.Tile.MoveCost);
```

**Use when** the gameplay grid is hexagonal — strategy games, board games, hex-based simulation. Axial coordinate math is built in.
**Don't use when** the grid is rectangular; the hex coordinate transforms are dead weight.

</details>

### Reactive

<details><summary><strong>ObservableList, ObservableHashSet</strong> (2 types)</summary>

#### `ObservableList<T>` — Reactive

`List<T>` plus `INotifyCollectionChanged` and `INotifyPropertyChanged`. Side-channel events (`ItemAdded`, `ItemInserted`, `ItemRemovedAt`, `ItemReplaced`, `ListCleared`) fire alongside the standard events. Re-entrant mutations from inside event handlers are blocked.

| Operation | Time | Space |
|---|---|---|
| `Add` | O(1) amortized + O(s) notify | O(1) |
| `AddRange` | O(n) + one batched notify | O(1) |
| `Insert(i, item)` / `RemoveAt(i)` | O(N − i) + O(s) | O(1) |
| `Remove(item)` | O(N) + O(s) | O(1) |
| `RemoveAll(predicate)` / `Clear` / `BatchUpdate` | O(N) + one `Reset` notify | — |
| `this[i]` (get / set) | O(1) (set: + O(s)) | — |
| Storage | — | O(N) |

```csharp
var list = new ObservableList<Item>();
list.CollectionChanged += (s, e) => RefreshUi(e);
list.Add(item);                                     // single Add notification
list.BatchUpdate(l => { l.Add(a); l.Add(b); });     // single Reset notification
```

**Use when** a UI framework (WPF / Avalonia / MAUI) binds to the collection and you want batch mutations without per-item notification storms.
**Don't use when** there are no subscribers — the bookkeeping has no payoff.

#### `ObservableHashSet<T>` — Reactive

`HashSet<T>` plus `INotifyCollectionChanged` and `INotifyPropertyChanged`. Set algebra (`UnionWith`, `IntersectWith`, `ExceptWith`, `SymmetricExceptWith`) batches notifications.

| Operation | Time | Space |
|---|---|---|
| `Add(item)` / `Remove(item)` | O(1) avg + O(s) notify | O(1) |
| `AddRange` / `RemoveWhere` | O(n) + one batched notify | O(1) |
| `Contains(item)` | O(1) avg | — |
| `UnionWith` | O(other.Count) + one notify | O(1) |
| `IntersectWith` / `ExceptWith` / `SymmetricExceptWith` | O(N + other.Count) + one notify | O(1) |
| `Clear` | O(N) + one `Reset` | — |
| Storage | — | O(N) |

```csharp
var achievements = new ObservableHashSet<string>();
achievements.CollectionChanged += (s, e) => Persist(e);
achievements.Add("first-blood");
```

**Use when** a UI or persistence layer needs to observe set membership changes without losing set algebra (`UnionWith` etc.).
**Don't use when** there are no subscribers — `HashSet<T>` is leaner.

</details>

### Temporal

<details><summary><strong>TimelineArray</strong> (1 type)</summary>

#### `TimelineArray<T>` — Temporal

Fixed-capacity ring buffer of `(timestamp, T)` pairs. Records are written at "now" or at an explicit timestamp; queries find the snapshot at a target time via binary search over the live window.

| Operation | Time | Space |
|---|---|---|
| `Record(snapshot)` / `Record(snapshot, ts)` | O(1) | O(1) |
| `GetAtTime(ts)` / `RewindTo` / `JumpForward` / `JumpBackward` | O(log N) | O(1) |
| `Replay(start, end)` | O(log N + k) | O(1) streaming |
| `ReplayAtFps(start, fps)` | O(log N + frames) | O(1) streaming |
| `GetTimeWindow(start, duration)` | O(log N + k) | O(1) streaming |
| Storage | — | O(capacity), buffer optionally rented from `ArrayPool<T>` |

```csharp
using var timeline = TimelineArray<GameState>.CreateWithArrayPool(capacity: 1800);
timeline.Record(state);
GameState? past = timeline.GetAtTime(targetTimestamp);
foreach (var s in timeline.Replay(start, end)) { /* ... */ }
```

**Use when** you record per-frame or per-tick state and need to query "what did it look like at time T" — replay buffers, lag compensation, deterministic rollback netcode.
**Don't use when** you only care about the latest value; a single field beats a ring buffer.

</details>

## Complexity reference

Summary across all 32 types. `*` denotes amortized; `c` is centroid / cell count, `m` is bucket / register count, `k` is result size, `d` is sketch depth, `s` is subscriber count.

| Type | Add / Insert | Lookup / Query | Remove | Iterate | Space |
|---|---|---|---|---|---|
| **Linear** |
| `BoundedList<T>` | O(1) | O(1) index, O(N) Contains | O(N − i) | O(N) | O(capacity) |
| `PooledList<T>` | O(1)* | O(1) index, O(N) Contains | O(N − i) | O(N) | O(capacity) |
| `PooledStack<T>` | O(1)* Push | O(1) Peek | O(1) Pop | O(N) | O(capacity) |
| `PooledQueue<T>` | O(1)* Enqueue | O(1) Peek | O(1) Dequeue | O(N) | O(capacity) |
| `MinHeap<T>` | O(log N) Insert | O(1) PeekMin | O(log N) ExtractMin | O(N) | O(capacity) |
| `MaxHeap<T>` | O(log N) Insert | O(1) PeekMax | O(log N) ExtractMax | O(N) | O(capacity) |
| **Spatial** |
| `QuadTree<T>` | O(log N) avg | O(log N + k) Query | O(log N) avg | O(N) | O(N) |
| `OctTree<T>` | O(log N) avg | O(log N + k) Sphere/AABB/Frustum | — | O(N) | O(N) |
| `KdTree<T>` | O(log N) avg | O(log N) Nearest, O(N^(1−1/d) + k) Range | — | O(N) | O(N) |
| `SpatialHashGrid<T>` | O(1) avg | O((r/cell)² · c) Radius | O(1) avg | O(N) | O(N + cells) |
| `TemporalSpatialHashGrid<T>` | O(1) avg | O((r/cell)² · c) live, + O(snapshots) at past time | O(1) avg | O(N) | O(N + snapshots) |
| `BloomRTreeDictionary<TKey,TValue>` | O(log N) avg | O(1) by key, O(log N + k) by region | O(log N) avg | O(N) | O(N) |
| **Hybrid** |
| `BoundedDictionary<TKey,TValue>` | O(1) avg | O(1) avg | O(1) avg | O(N) | O(capacity) |
| `LinkedDictionary<TKey,TValue>` | O(1) avg | O(1) avg (*mutates LRU on `TryGetValue` / get*) | O(1) avg | O(N) | O(N) |
| `QueueDictionary<TKey,TValue>` | O(1) avg Enqueue | O(1) avg by key | O(1) Dequeue, O(1) avg by key | O(N) | O(N) |
| `DequeDictionary<TKey,TValue>` | O(1) avg Push{Front,Back} | O(1) avg by key | O(1) Pop{Front,Back}, O(1) avg by key | O(N) | O(N) |
| `CounterDictionary<TKey,TValue>` | O(1) avg | O(1) avg (*count++ on `TryGetValue`*) | O(1) avg | O(N) | O(N + freq buckets) |
| `GraphDictionary<TKey,TValue>` | O(1) avg vertex / edge | O(V+E) ShortestPath, O(V+E) SCC | O(deg) vertex, O(1) avg edge | O(V+E) | O(V + E) |
| `LinkedMultiMap<TKey,TValue>` | O(1) avg | O(1) avg key, O(values) per-key membership | O(values) per key | O(keys + values) | O(keys + values) |
| `ConcurrentLinkedDictionary<TKey,TValue>` | O(1) avg | O(1) avg (*mutates access timestamp*) | O(1) avg | O(N) | O(N + 1 lock per bucket) |
| `PredictiveDictionary<TKey,TValue>` | O(1) avg + pattern update | O(1) avg + pattern update; O(p) GetPredictions | O(1) avg | O(N) | O(N + capped patterns) |
| **Probabilistic** |
| `BloomFilter<T>` | O(k) | O(k) Contains (one-sided FP) | — | — | O(m) bits |
| `CountMinSketch<T>` | O(d) | O(d) Estimate | — | — | O(width · depth) |
| `HyperLogLog<T>` | O(1) | O(m) first call, O(1) cached | — | — | O(m) bytes |
| `Digest` | O(log c)* | O(log c) Quantile | — | — | O(compression) |
| `DigestStreamingAnalytics<T>` | O(log c)* | O(log c) Percentile | — | — | O(compression + window buffer) |
| **Grid** |
| `BitGrid2D` | O(1) set | O(1) get | — | O(W·H) | ⌈W·H / 8⌉ bytes |
| `LayeredGrid2D<T>` | O(1) set | O(1) get | — | O(W·H·layers) | O(W·H·layers) |
| `HexGrid2D<T>` | O(1) avg set | O(1) avg get; O((V+E) log V) FindPath | O(1) avg | O(N) | O(N) |
| **Reactive** |
| `ObservableList<T>` | O(1)* + O(s) | O(1) index, O(N) Contains | O(N − i) + O(s) | O(N) | O(N) |
| `ObservableHashSet<T>` | O(1) avg + O(s) | O(1) avg | O(1) avg + O(s) | O(N) | O(N) |
| **Temporal** |
| `TimelineArray<T>` | O(1) Record | O(log N) GetAtTime | — | O(log N + k) Replay | O(capacity) |

## Benchmarks

Reference benchmark numbers live at [`docs/perf/i7-13700KF/`](docs/perf/i7-13700KF/) — both `--standard` and `--rigorous` BenchmarkDotNet profiles, machine-tagged. Reproduce on your hardware with `.\bench.ps1 --rigorous --filter '*<TypeName>Benchmarks*'`. Methodology at [`docs/benchmarks.md`](docs/benchmarks.md).

## Compatibility

Libraries multi-target `net8.0;netstandard2.1`. Tests cover `net8.0` and `net6.0`. Public surface is baselined with `Microsoft.CodeAnalysis.PublicApiAnalyzers` — patch releases will not break consumers without an explicit major-version bump. SourceLink and `.snupkg` symbol packages ship with every release.

## License

[MIT](LICENSE). Free for any use — personal, commercial, fork, modify, redistribute — provided the copyright notice and license text remain in derivative works.

## Contributing

Issues and pull requests welcome — file against [`dev/omni-collections-v2`](https://github.com/Codeturion/omni-collections/tree/dev/omni-collections-v2). Performance-affecting PRs need before/after numbers from `bench.ps1 --standard` (or `--rigorous` for headline claims).
