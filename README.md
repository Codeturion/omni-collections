# Omni.Collections

[![NuGet Version](https://img.shields.io/nuget/v/OmniCollections.svg)](https://www.nuget.org/packages/OmniCollections/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OmniCollections.svg)](https://www.nuget.org/packages/OmniCollections/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**33 specialized .NET data structures addressing algorithmic bottlenecks**

When .NET's built-in collections hit their limits - spatial indexing, priority processing, bounded memory, streaming analytics - these structures provide the missing pieces. The core collections deliver proven algorithmic improvements; others explore useful combinations and patterns.

Some structures emerged from research curiosity, but all maintain production-quality implementation standards.

Comprehensive benchmarks because "trust me, it's faster" isn't engineering. Clean architecture because performance code shouldn't be throwaway code.

## Table of Contents

<!-- Table of Contents - Always expanded -->

- [Quick Start](#quick-start)
- [What's Inside](#whats-inside)
- [Core Data Structures](#core-data-structures)
  - [Linear Collections](#linear-collections) (6 structures)
  - [Spatial Structures](#spatial-structures) (6 structures)
  - [Hybrid Structures](#hybrid-structures) (9 structures)
  - [Probabilistic Structures](#probabilistic-structures) (6 structures)
  - [Grid Structures](#grid-structures) (3 structures)
  - [Reactive Structures](#reactive-structures) (2 structures)
  - [Temporal Structures](#temporal-structures) (1 structure)
- [Real-World Usage Examples](#real-world-usage-examples)
  - [Unity Game Development](#unity-game-development-examples)
  - [High-Throughput Web APIs](#high-throughput-web-api-processing)
- [Installation](#installation)
- [Performance Results & Benchmarking](#performance-results--benchmarking)
- [Security Considerations](#security-considerations)
- [Choosing the Right Structure](#choosing-the-right-structure)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgments](#acknowledgments)

## Quick Start

<details>
<summary>🚀 Get up and running in 2 minutes</summary>

### Installation
```bash
dotnet add package OmniCollections
```

### Hello World - FastQueue (Most Common Use Case)
```csharp
using Omni.Collections.Linear;

// Replace Queue<T> for high-throughput scenarios
var queue = FastQueue<Order>.CreateWithArrayPool(capacity: 10000);
queue.Enqueue(new Order("12345"));
var next = queue.Dequeue(); // O(1) with reduced allocations
```

### Common Scenarios - Pick Your Performance Win
```csharp
// Spatial queries (games, GIS, collision detection)
var quadTree = new QuadTree<GameObject>(worldBounds);
quadTree.Insert(position, gameObject);
var visible = quadTree.Query(cameraBounds); // O(log n) vs O(n)

// Priority processing (job queues, A* pathfinding)  
var heap = MinHeap<Task>.CreateWithArrayPool(1000);
heap.Insert(urgentTask);
var next = heap.ExtractMin(); // O(log n) priority extraction

// Bounded memory (caches, real-time systems)
var cache = new CircularDictionary<string, Data>(capacity: 1000);
cache["key"] = data; // Auto-evicts oldest when full
```

### Next Steps
- 📚 Browse [Core Data Structures](#core-data-structures) for your specific use case
- 🎮 See [Real-World Examples](#real-world-usage-examples) for Unity, web apps, and services  
- 📊 Check [Performance Claims](#benchmarking) with our benchmark results
- 🔧 [Report Issues](https://github.com/Codeturion/omni-collections/issues) or ask questions

</details>

## What's Inside

<details>
<summary>🎯 Core algorithmic improvements</summary>

**Priority processing** - MinHeap and MaxHeap provide O(log n) priority operations vs O(n log n) sorting approaches. Essential for job scheduling, pathfinding algorithms, and real-time task management.

**Spatial indexing** - QuadTree spatial queries replace O(n) scans with O(log n) lookups. SpatialHashGrid maintains O(1) insertion even with millions of objects. Power collision detection, GIS queries, and game world management.

**Bounded collections** - CircularDictionary provides automatic cache eviction with guaranteed memory bounds. BoundedList ensures predictable capacity limits for real-time systems.

**Memory-efficient operations** - ArrayPool-backed structures reduce allocation pressure for specific operations. FastQueue processes high-throughput scenarios with reduced GC impact where workload patterns align.

**Stream analytics** - TDigest calculates percentiles on unlimited streams with bounded memory. CountMinSketch tracks frequencies without memory explosion. BloomFilter provides zero-false-negative filtering.

**Hybrid access patterns** - QueueDictionary combines FIFO ordering with key lookups. LinkedDictionary maintains insertion order. These eliminate common "Dictionary plus something else" patterns.

</details>

## Core Data Structures

### Linear Collections

<details>
<summary>FastQueue, MinHeap, MaxHeap, BoundedList, PooledList, PooledStack (6 structures)</summary>

#### FastQueue<T>
```csharp
var queue = FastQueue<Order>.CreateWithArrayPool(capacity: 10000);
queue.Enqueue(order);  // O(1) amortized
var next = queue.Dequeue();  // O(1)
```
**Performance advantage:** ArrayPool variant shows reduced allocations; non-pooled version has mixed results
**When to use:** High-throughput scenarios - strongly prefer ArrayPool variant for allocation benefits

#### MinHeap<T>
```csharp
var heap = MinHeap<Task>.CreateWithArrayPool(capacity: 1000);
heap.Insert(task);  // O(log n)
var urgent = heap.ExtractMin();  // O(log n)
```
**Algorithmic advantage:** O(log n) priority operations vs O(n log n) sorting approaches
**When to use:** Priority queues, job scheduling, Dijkstra's algorithm, A* pathfinding

#### MaxHeap<T>
```csharp
var heap = MaxHeap<Priority>.CreateWithArrayPool(capacity: 1000);
heap.Insert(priority);  // O(log n)
var highest = heap.ExtractMax();  // O(log n)
```
**Algorithmic advantage:** Same O(log n) operations as MinHeap but for maximum priority
**When to use:** Top-K problems, priority scheduling with highest-first processing, heap sort

#### BoundedList<T>
```csharp
var list = new BoundedList<Event>(maxCapacity: 1000);
list.Add(event);  // O(1) with capacity guarantee
// Throws when capacity exceeded
```
**Benchmark results:** Improved insertion speed over List<T>, but slower enumeration in some cases
**When to use:** Real-time systems requiring predictable memory usage

#### PooledList<T>
```csharp
var list = PooledList<Item>.CreateWithArrayPool(capacity: 1000);
list.Add(item);  // O(1) amortized with ArrayPool backing
// Returns memory to pool on disposal
```
**Performance advantage:** Reduced allocations for Add operations; indexer and RemoveAt show mixed results
**When to use:** High-frequency list creation scenarios where Add is the primary operation

#### PooledStack<T>
```csharp
var stack = PooledStack<Item>.CreateWithArrayPool(capacity: 1000);
stack.Push(item);  // O(1) with reduced allocations
var last = stack.Pop();  // O(1) but may have higher allocations than Stack<T>
// Returns memory to pool on disposal
```
**Performance advantage:** Significantly reduced allocations for Push; Pop and Peek may have higher overhead
**When to use:** Scenarios dominated by Push operations; evaluate for your specific usage pattern

</details>

### Spatial Structures

<details>
<summary>QuadTree, OctTree, KDTree, SpatialHashGrid, TemporalSpatialHashGrid, BloomRTreeDictionary (6 structures)</summary>

#### QuadTree<T>
```csharp
var quadTree = new QuadTree<GameObject>(worldBounds);
quadTree.Insert(position, gameObject);  // O(log n) average - Point first, then item

var visible = quadTree.Query(cameraBounds);  // O(log n + k) where k = results
```
**Algorithmic advantage:** O(log n + k) spatial queries vs O(n) linear search
**When to use:** 2D spatial partitioning, 2D collision detection, 2D viewport culling

#### OctTree<T>
```csharp
var octTree = OctTree<Entity>.Create3D(
    getX: e => e.Position.X,
    getY: e => e.Position.Y,
    getZ: e => e.Position.Z,
    minSize: 1.0f);
octTree.Insert(entity);  // O(log n) average

var nearby = octTree.FindInSphere(center, radius);  // O(log n + k) where k = results
var nearest = octTree.FindNearest(targetPos);  // O(log n) nearest neighbor
```
**Algorithmic advantage:** O(log n + k) 3D spatial queries vs O(n) linear search
**When to use:** 3D spatial partitioning, 3D collision detection, frustum culling, particle systems

#### KDTree<T>
```csharp
var kdTree = KdTree<DataPoint>.Create3D(
    getX: p => p.X,
    getY: p => p.Y, 
    getZ: p => p.Z);
kdTree.Insert(point);  // O(log n) balanced insertion

var nearest = kdTree.FindNearestK(target, 5);  // O(log n) average k-nearest neighbors
```
**Algorithmic advantage:** Efficient k-nearest neighbor vs brute-force distance calculations
**When to use:** Machine learning, clustering, multi-dimensional nearest neighbor searches

#### SpatialHashGrid<T>
```csharp
var spatialGrid = new SpatialHashGrid<Entity>(cellSize: 64.0f);
spatialGrid.Insert(x, y, entity);  // O(1) average insertion
var nearby = spatialGrid.GetObjectsInRectangle(minX, minY, maxX, maxY);  // O(k) uniform performance
var collisions = spatialGrid.GetPotentialCollisions();  // O(n) collision detection
```
**Algorithmic advantage:** O(1) average case maintained regardless of data distribution
**When to use:** Uniform spatial data, collision detection, particle systems

#### TemporalSpatialHashGrid<T>
```csharp
var temporalGrid = new TemporalSpatialHashGrid<MovingEntity>(
    cellSize: 32.0f, 
    snapshotInterval: TimeSpan.FromSeconds(1),
    historyRetention: TimeSpan.FromMinutes(10));
temporalGrid.UpdateObject(entity, x, y, velocityX, velocityY);  // O(1) space-time update

// Query entities at specific time and location
var snapshot = temporalGrid.GetObjectsInRadiusAtTime(x, y, radius, timestamp);  // O(k) temporal query
var trajectory = temporalGrid.GetObjectTrajectory(entity, TimeSpan.FromMinutes(5));  // O(t) time range
```
**Algorithmic advantage:** Combines O(1) spatial hashing with temporal indexing for 4D queries
**When to use:** Motion prediction, temporal collision detection, trajectory analysis, time-based replay systems

#### BloomRTreeDictionary<TKey, TValue>
```csharp
var spatialDict = new BloomRTreeDictionary<string, Building>(
    expectedCapacity: 10000,
    falsePositiveRate: 0.01);
spatialDict.Add("building1", building, boundingRectangle);  // O(log n) with R*-tree splitting

// Bloom filter pre-screens spatial queries
var nearbyBuildings = spatialDict.FindIntersecting(searchBounds);  // O(log n + k) with negative pruning
var pointQuery = spatialDict.FindAtPoint(x, y);  // O(log n) point queries
var stats = spatialDict.Statistics;  // Track Bloom filter effectiveness
```
**Algorithmic advantage:** R-tree spatial indexing with Bloom filter negative pruning for order-of-magnitude faster queries
**When to use:** GIS applications, large-scale spatial databases, game world management with thousands of entities

</details>

### Hybrid Structures

<details>
<summary>CounterDictionary, LinkedDictionary, QueueDictionary, CircularDictionary, DequeDictionary, ConcurrentLinkedDictionary, LinkedMultiMap, GraphDictionary, PredictiveDictionary (9 structures)</summary>

*Note: Most hybrid structures provide **convenience** rather than algorithmic improvements - they combine multiple data structure capabilities with the same O(1) operations.*

#### CounterDictionary<TKey, TValue>
```csharp
var counter = new CounterDictionary<string, Product>();
counter.IncrementCount("product1");  // O(1) frequency tracking

var hotItems = counter.GetMostFrequent(10);  // Returns KeyValuePair<TKey, (TValue, long count)>
```
**Convenience advantage:** Combines dictionary with frequency counting in one structure
**When to use:** Analytics, frequency tracking, LFU cache implementations

#### LinkedDictionary<TKey, TValue>
```csharp
var linked = new LinkedDictionary<string, Config>();
linked.AddOrUpdate("setting1", config);  // O(1) with insertion order preservation

foreach (var kvp in linked) // Maintains insertion order
{
    // Process in predictable sequence
}
```
**Convenience advantage:** Dictionary operations with guaranteed iteration order
**When to use:** LRU caches, ordered configuration processing, insertion-order requirements

#### QueueDictionary<TKey, TValue>
```csharp
var queueDict = new QueueDictionary<string, Message>();
queueDict.Enqueue("msg1", message);  // O(1) with key lookup
var next = queueDict.Dequeue();      // O(1) FIFO - Returns KeyValuePair<TKey, TValue>
```
**Convenience advantage:** Combines queue and dictionary operations in one structure
**When to use:** Message routing, job queuing with key-based access

#### CircularDictionary<TKey, TValue>
```csharp
var cache = new CircularDictionary<string, Data>(capacity: 1000);
cache["key"] = data;  // O(1) - auto-evicts oldest when full
var oldest = cache.GetOldest();  // O(1) - Returns KeyValuePair<TKey, TValue>
```
**Performance advantage:** Guaranteed memory bounds with automatic eviction
**When to use:** Fixed-size caches, bounded memory scenarios

#### DequeDictionary<TKey, TValue>
```csharp
var deque = new DequeDictionary<string, Message>();
deque.PushFront("msg1", message);   // O(1) front insertion
deque.PushBack("msg2", message);    // O(1) back insertion
var first = deque.PopFront();       // O(1) front removal - Returns KeyValuePair
```
**Convenience advantage:** Deque operations with key-based lookups
**When to use:** Double-ended processing with key lookup, undo/redo systems

#### ConcurrentLinkedDictionary<TKey, TValue>
```csharp
var concurrent = new ConcurrentLinkedDictionary<string, Config>();
concurrent.Add("setting", config);  // Thread-safe with insertion order
// Lock-free reads, fine-grained write locking
```
**Performance advantage:** Thread-safe operations with insertion order preservation
**When to use:** Multi-threaded scenarios requiring ordered processing

#### LinkedMultiMap<TKey, TValue>
```csharp
var multiMap = new LinkedMultiMap<string, Tag>();
multiMap.Add("item1", tag1);
multiMap.Add("item1", tag2);  // Multiple values per key
var allTags = multiMap.GetValues("item1");  // O(1) access to value list
```
**Convenience advantage:** Native support for multiple values per key
**When to use:** Many-to-many relationships, tagging systems, grouped data

#### GraphDictionary<TKey, TValue>
```csharp
var graph = new GraphDictionary<string, User>();
graph.Add("alice", aliceData);
graph.AddEdge("alice", "bob", weight: 1.0);  // O(1) edge creation
graph.AddBidirectionalEdge("alice", "charlie");  // Automatic two-way relationship

// Built-in graph algorithms
var path = graph.FindShortestPath("alice", "david");  // O(V + E) with BFS
// Additional graph operations available: GetNeighbors, GetIncomingEdges, GetOutgoingEdges
var neighbors = graph.GetNeighbors("alice");  // O(degree) neighbor access
```
**Algorithmic advantage:** Maintains vertex-edge relationships with O(1) vertex ops and O(degree) edge ops
**When to use:** Social networks, dependency graphs, workflow engines, route planning

#### BloomDictionary<TKey, TValue>
```csharp
var bloomDict = new BloomDictionary<string, Data>(capacity: 10000, falsePositiveRate: 0.01);
bloomDict.Add("key", data);  // O(1) with Bloom filter pre-screening
if (bloomDict.ContainsKey("key"))  // O(k) fast negative lookups
{
    var data = bloomDict["key"];  // Skip expensive lookups when possible
}
```
**Performance advantage:** Pre-screening eliminates unnecessary dictionary lookups (same O(1) complexity)
**When to use:** Large dictionaries with high miss rates, database query optimization

#### PredictiveDictionary<TKey, TValue>
```csharp
var predictive = new PredictiveDictionary<string, CachedData>();
predictive["user123"] = userData;  // Tracks access patterns
// Generates predictions based on historical access sequences
var predictions = predictive.GetPredictions(contextKeys);  // Pattern-based predictions
```
**Pattern advantage:** Heuristic-based access pattern recognition with confidence scoring
**When to use:** Caching systems with predictable access patterns, prefetching optimizations

</details>

### Probabilistic Structures

<details>
<summary>BloomFilter, CountMinSketch, HyperLogLog, TDigest, DigestStreamingAnalytics (5 structures)</summary>

#### BloomFilter<T>
```csharp
var filter = new BloomFilter<string>(expectedItems: 1000000, falsePositiveRate: 0.01);
filter.Add("exists");  // O(k) where k = hash functions

if (!filter.Contains("checkThis"))  // O(k) - zero false negatives guaranteed
{
    // Definitely doesn't exist - skip expensive lookup
}
```
**Algorithmic advantage:** Sub-linear memory growth O(k) vs O(n) for exact sets
**When to use:** Negative caching, database query optimization, spell checkers

#### CountMinSketch<T>
```csharp
var sketch = new CountMinSketch(width: 1000, depth: 5);
sketch.Add("event");  // O(d) frequency tracking
long frequency = sketch.EstimateCount("event");  // O(d) frequency estimate
sketch.Merge(otherSketch);  // O(width × depth) sketch combination
```
**Algorithmic advantage:** O(1) memory vs O(n) for exact counting
**When to use:** Stream processing, frequency estimation, network monitoring, heavy hitters detection

#### HyperLogLog<T>
```csharp
var hll = new HyperLogLog<string>(bucketBits: 14);
hll.Add("unique_item");  // O(1) cardinality tracking
long cardinality = hll.EstimateCardinality();  // O(m) unique count estimate
hll.Merge(otherHLL);  // O(m) combine estimators
```
**Algorithmic advantage:** O(log log n) memory for cardinality estimation
**When to use:** Unique visitor counting, distinct value estimation, database query optimization

#### TDigest
```csharp
var digest = new Digest(compression: 100);
digest.Add(latencyMs);  // O(log n) adaptive compression

double p99 = digest.Quantile(0.99);  // O(1) percentile queries
double median = digest.Quantile(0.5);  // O(1) median
digest.Merge(otherDigest);  // O(n) combine digests
```
**Algorithmic advantage:** O(1) percentile queries with bounded memory usage through adaptive compression
**When to use:** Streaming percentile calculation, SLA monitoring, performance analytics, distributed percentile aggregation

#### DigestStreamingAnalytics<T>
```csharp
var analytics = new DigestStreamingAnalytics<ResponseTime>(
    windowSize: TimeSpan.FromMinutes(5),
    valueExtractor: r => r.Milliseconds);

analytics.Add(responseTime);  // O(log n) streaming insertion
var p99 = analytics.GetPercentile(0.99);  // O(1) real-time percentiles
```
**Algorithmic advantage:** Real-time percentiles with sliding time windows (probabilistic approximation)
**When to use:** Real-time dashboards, SLA monitoring, streaming performance analytics

</details>

### Grid Structures

<details>
<summary>BitGrid2D, LayeredGrid2D, HexGrid2D (3 structures)</summary>

#### BitGrid2D / LayeredGrid2D / HexGrid2D
```csharp
// BitGrid2D - bit-packed boolean grid
var bitGrid = new BitGrid2D(width: 1000, height: 1000);
bitGrid[x, y] = true;  // O(1) bit-packed storage

// LayeredGrid2D - multi-layer grid support
var layeredGrid = new LayeredGrid2D<int>(width: 100, height: 100, layerCount: 3);
layeredGrid[layer, x, y] = value;  // O(1) multi-layer access

// HexGrid2D - hexagonal grid operations
var hexGrid = new HexGrid2D<Entity>();
hexGrid[hexCoord] = entity;  // O(1) hexagonal coordinate access
```
**Performance advantage:** Significant memory reduction through bit-packing
**When to use:** Large boolean grids, cellular automata, collision masks

</details>

### Reactive Structures

<details>
<summary>ObservableList, ObservableHashSet (2 structures)</summary>

#### ObservableList<T>
```csharp
var list = new ObservableList<Item>();
list.CollectionChanged += OnItemsChanged;

list.Add(item);  // O(1) with change notification
```
**When to use:** MVVM patterns, UI data binding, event-driven architectures

#### ObservableHashSet<T>
```csharp
var achievements = new ObservableHashSet<string>();
achievements.ItemAdded += OnAchievementUnlocked;
achievements.CollectionChanged += OnAchievementsChanged;

achievements.Add("FirstKill");  // O(1) with change notification
```
**Convenience advantage:** HashSet operations with change notifications
**When to use:** Achievement systems, unique item tracking, reactive unique collections


</details>

### Temporal Structures

<details>
<summary>TimelineArray, TemporalSpatialGrid (2 structures)</summary>

#### TimelineArray<T>
```csharp
var timeline = new TimelineArray<Event>(capacity: 10000);
timeline.Record(eventData, timestamp);  // O(1) time-indexed storage

var events = timeline.Replay(startTime, endTime);  // O(log n) temporal range queries
```
**When to use:** Event sourcing, time-series data, temporal debugging systems

#### TemporalSpatialGrid<T>
```csharp
var temporalGrid = TemporalSpatialGrid<Entity>.CreateWithArrayPool(
    capacity: 3600,  // 1 hour at 60 FPS
    cellSize: 64.0f,
    frameDuration: 16,  // milliseconds
    autoRecord: true);

// Automatically records spatial snapshots
temporalGrid.Insert(x, y, entity);  // O(1) current frame insertion
temporalGrid.RecordSnapshot(timestamp);  // O(n) snapshot current state

// Query across time and space
var entitiesAtTime = temporalGrid.GetObjectsInRadiusAtTime(x, y, radius, timestamp);  // O(k) spatial query at time
var history = temporalGrid.ReplaySpatialHistory(startTime, endTime);  // Replay time range
var heatMap = temporalGrid.GenerateHeatMap(minX, minY, maxX, maxY, startTime, endTime, cellSize);  // Analytics
```
**Algorithmic advantage:** Combines TimelineArray with SpatialHashGrid for efficient 4D (space + time) indexing
**When to use:** Simulation replay, temporal GIS, motion tracking, game replay systems, debugging spatial behaviors over time

</details>

## Measured Performance Characteristics

**Important:** Performance benefits are operation-specific, not universal. Always benchmark your specific use case.

**Note on terminology:**
- **Algorithmic advantage**: Better Big-O complexity (e.g., O(log n) vs O(n))
- **Performance advantage**: Same Big-O but faster execution (often operation-specific)
- **Convenience advantage**: Same performance but cleaner API (combines multiple structures, atomic operations)

**ArrayPool structures:** Benefits vary by operation - some operations show dramatic improvements while others may have higher overhead. The pooled variants are optimized for specific patterns (e.g., Push for PooledStack, Add for PooledList).

### Benchmark Results Summary

<details>
<summary>📊 Detailed benchmark comparison table</summary>

*Tested on .NET 8, x64, 50K items*

| Structure | vs Baseline | Primary Advantage | Trade-offs |
|-----------|-------------|-------------------|------------|
| **Linear Collections** |
| BoundedList | Improved insertion speed | Predictable capacity | Slower enumeration |
| PooledList | Reduced Add allocations | ArrayPool memory reuse | Mixed results for other ops |
| PooledStack | Reduced Push allocations | ArrayPool memory reuse | Higher Pop/Peek overhead |
| FastQueue (ArrayPool) | Reduced allocations | Lower GC pressure | Non-pooled version mixed |
| MinHeap | O(log n) priority ops | Efficient min priority queue | Tree maintenance overhead |
| MaxHeap | O(log n) priority ops | Efficient max priority queue | Tree maintenance overhead |
| **Spatial Structures** |
| QuadTree | O(log n) vs O(n) | 2D spatial partitioning | Overhead on small datasets |
| OctTree | O(log n) vs O(n) | 3D spatial partitioning | Higher memory than QuadTree |
| KDTree | O(log n) k-NN | Multi-dimensional search | Build time overhead |
| SpatialHashGrid | O(1) average ops | Uniform performance | Memory for grid cells |
| TemporalSpatialHashGrid | O(1) + temporal indexing | 4D space-time queries | Memory for time windows |
| BloomRTreeDictionary | Bloom + R-tree | Order-of-magnitude faster queries | False positive rate |
| **Hybrid Structures** |
| CounterDictionary | Fast frequency tracking | Analytics support | Memory for counts |
| LinkedDictionary | Ordered iteration | Insertion order preserved | Memory for links |
| QueueDictionary | O(1) queue + dictionary | Combined access patterns | Dual structure overhead |
| CircularDictionary | Automatic eviction | Bounded memory | LRU overhead |
| DequeDictionary | Double-ended + lookup | Flexible access | Complex structure |
| ConcurrentLinkedDictionary | Thread-safe ordering | Concurrent access | Locking overhead |
| LinkedMultiMap | Multiple values per key | Many-to-many support | Memory for value lists |
| GraphDictionary | Graph operations | Built-in graph algorithms | Edge storage overhead |
| BloomDictionary | Fast negative lookups | Pre-screening optimization | False positive rate |
| PredictiveDictionary | Pattern-based caching | Predictive prefetching | Learning overhead |
| **Probabilistic Structures** |
| BloomFilter | Sub-linear memory | Zero false negatives | False positive rate |
| CountMinSketch | Constant memory | Stream processing | Approximate results |
| HyperLogLog | Sub-linear memory | Massive cardinality | Approximate results |
| TDigest | Bounded memory | Streaming percentiles | Compression trade-off |
| DigestStreamingAnalytics | Real-time percentiles | Sliding window analytics | Windowing complexity |
| **Grid Structures** |
| BitGrid2D | Significantly reduced memory | Space efficiency | Slower than bool[,] access |
| LayeredGrid2D | Multi-layer support | Z-axis operations | Memory for layers |
| HexGrid2D | Hexagonal operations | Game board layouts | Coordinate conversion |
| **Reactive Structures** |
| ObservableList | Change notifications | Event-driven updates | Notification overhead |
| ObservableHashSet | Unique items + notifications | Reactive unique collections | Event overhead |
| **Temporal Structures** |
| TimelineArray | Time-indexed storage | Temporal queries | Fixed capacity |
| TemporalSpatialGrid | 4D indexing | Space-time replay | Memory for snapshots |

</details>

### Complexity Guarantees

| Structure | Insert | Remove | Lookup | Special Operation |
|-----------|--------|--------|--------|-------------------|
| **Spatial** |
| QuadTree | O(log n) | O(log n) | - | Spatial query: O(log n + k) |
| OctTree | O(log n) | O(log n) | - | 3D query: O(log n + k) |
| KDTree | O(log n) | O(log n) | - | k-NN: O(log n) average |
| SpatialHashGrid | O(1) avg | O(1) avg | - | Range query: O(k) |
| TemporalSpatialHashGrid | O(1) avg | O(1) avg | - | Temporal query: O(k), Trajectory: O(t) |
| BloomRTreeDictionary | O(log n) | O(log n) | O(1) | Range query: O(log n + k) with pruning |
| **Linear** |
| FastQueue | O(1)* | O(1) | - | Enqueue/Dequeue: O(1) |
| MinHeap/MaxHeap | O(log n) | O(log n) | O(1) peek | ExtractMin/Max: O(log n) |
| BoundedList | O(1) | O(n) | O(1) | Capacity-bounded |
| PooledList | O(1)* | O(n) | O(1) | ArrayPool integration |
| PooledStack | O(1)* | O(1) | O(1) peek | Push/Pop: O(1) |
| **Hybrid** |
| CounterDictionary | O(1) | O(1) | O(1) | GetFrequency: O(1) |
| LinkedDictionary | O(1) | O(1) | O(1) | Maintains insertion order |
| QueueDictionary | O(1) | O(1) | O(1) | Dequeue: O(1) |
| CircularDictionary | O(1) | O(1) | O(1) | Auto-eviction on capacity |
| DequeDictionary | O(1) | O(1) | O(1) | AddFirst/Last: O(1) |
| ConcurrentLinkedDictionary | O(1) | O(1) | O(1) | Thread-safe operations |
| LinkedMultiMap | O(1) | O(1) | O(1) | GetValues: O(k) |
| GraphDictionary | O(1) | O(1) | O(1) | AddEdge: O(1), ShortestPath: O(V+E) |
| BloomDictionary | O(k) | O(k) | O(k) | MightContain: O(k) |
| PredictiveDictionary | O(1) | O(1) | O(1) | Prediction: O(1) |
| **Probabilistic** |
| BloomFilter | O(k) | - | O(k) | Zero false negatives |
| CountMinSketch | O(d) | - | O(d) | EstimateCount: O(d) |
| HyperLogLog | O(1) | - | - | EstimateCardinality: O(m) |
| TDigest | O(log n) | - | O(1) | Quantile query: O(1) |
| DigestStreamingAnalytics | O(log n) | - | O(1) | Quantile query: O(1) |
| **Grid** |
| BitGrid2D | O(1) | O(1) | O(1) | Bit-packed storage |
| LayeredGrid2D | O(1) | O(1) | O(1) | Layer operations: O(1) |
| HexGrid2D | O(1) | O(1) | O(1) | Neighbor queries: O(6) |
| **Reactive** |
| ObservableList | O(1)* | O(n) | O(1) | Event notification: O(s) |
| ObservableHashSet | O(1) | O(1) | O(1) | Event notification: O(s) |
| **Temporal** |
| TimelineArray | O(1) | - | O(log n) | GetRange: O(log n + k) |
| TemporalSpatialGrid | O(1) | O(1) | O(1) | Snapshot: O(n), QueryAtTime: O(k) |

*Amortized
k = number of hash functions or results
d = sketch depth
m = HyperLogLog registers
s = subscribers

## Real-World Usage Examples

### Unity Game Development Examples

<details>
<summary>Click to expand Unity examples</summary>

*Note: These are simplified examples for demonstration purposes. Production code would use job systems, burst compilation, ECS, custom update managers, or their own entry points rather than Update/FixedUpdate for performance-critical systems.*

#### Efficient Collision Detection
```csharp
// QuadTree for 2D games (platformers, top-down shooters)
public class CollisionSystem
{
    private QuadTree<Collider2D> _spatialIndex;
    
    void Start()
    {
        _spatialIndex = new QuadTree<Collider2D>(worldBounds);
        // Index all static colliders once
        foreach (var collider in staticColliders)
            _spatialIndex.Insert(collider.transform.position, collider);
    }
    
    void CheckPlayerCollisions(Player player)
    {
        // Only check nearby colliders instead of all colliders
        var nearby = _spatialIndex.Query(player.bounds);
        foreach (var collider in nearby)
        {
            if (Physics2D.OverlapBox(player.position, collider))
                HandleCollision(player, collider);
        }
    }
}

// SpatialHashGrid for uniform entities (bullets, particles)
public class BulletHellSystem
{
    private SpatialHashGrid<Bullet> _bulletGrid;
    
    void Update()
    {
        _bulletGrid.Clear();
        // Re-index all bullets each frame (very fast for uniform objects)
        foreach (var bullet in activeBullets)
            _bulletGrid.Insert(bullet.x, bullet.y, bullet);
        
        // Check collisions only in player's cell and neighbors
        var nearby = _bulletGrid.GetObjectsInRectangle(
            player.bounds.min.x, player.bounds.min.y, 
            player.bounds.max.x, player.bounds.max.y);
    }
}

// KDTree for 3D collision detection with distance metrics
public class ProximityDetectionSystem
{
    private KDTree<Enemy> _enemyIndex;
    
    void Start()
    {
        _enemyIndex = KDTree<Enemy>.Create3D();
        // Index all enemies for efficient distance queries
        foreach (var enemy in allEnemies)
            _enemyIndex.Insert(enemy.position, enemy);
    }
    
    void CheckNearbyEnemies(Player player, float detectionRadius)
    {
        // Find all enemies within detection radius
        var nearbyEnemies = _enemyIndex.RangeQuery(player.position, detectionRadius);
        foreach (var enemy in nearbyEnemies)
        {
            ProcessEnemyInteraction(player, enemy);
        }
    }
    
    Enemy FindClosestEnemy(Vector3 position)
    {
        // O(log n) nearest neighbor search
        return _enemyIndex.FindNearestNeighbor(position);
    }
}
```

| Operation | Unity Standard | Omni.Collections | Improvement |
|-----------|----------------|------------------|-------------|
| **2D Collision Detection** |
| Range Query | Linear Search: O(n) | QuadTree: O(log n) | **Dramatically faster** |
| Insert Static | List.Add | QuadTree.Insert | **Significantly faster** |
| **Uniform Object Collision** |
| Spatial Insert | Dictionary | SpatialHashGrid | **Much faster** |
| Range Query | Linear Search | SpatialHashGrid | **Considerably faster** |
| **3D Proximity Detection** |
| Nearest Neighbor | Linear Distance Check | KDTree (Euclidean) | **Extremely faster** |
| K-Nearest Search | Sorted Distance List | KDTree (k=5) | **Vastly faster** |

#### Real-World Impact
- **Dramatically faster collision queries** enable complex physics in bullet-hell games
- **Much faster spatial insertion** supports real-time dynamic object management
- **Extremely faster proximity detection** allows sophisticated AI behaviors
- **Minimal allocations** with ArrayPool integration reduces GC stutters
- **Multiple distance metrics** support various game mechanics (vision cones, blast radius)

#### AI and Pathfinding
```csharp
// MinHeap for A* pathfinding
public class AStarPathfinder
{
    private MinHeap<PathNode> _openSet;
    
    List<Vector3> FindPath(Vector3 start, Vector3 goal)
    {
        _openSet = MinHeap<PathNode>.CreateWithArrayPool(1000);
        _openSet.Insert(new PathNode(start, 0));
        
        while (!_openSet.IsEmpty)
        {
            var current = _openSet.ExtractMin();  // O(log n) vs sorting
            // Process node...
        }
    }
}

// CircularDictionary for AI decision caching
public class EnemyAI
{
    private CircularDictionary<string, AIDecision> _decisionCache;
    
    void Start()
    {
        // Cache last 100 decisions to avoid recalculation
        _decisionCache = new CircularDictionary<string, AIDecision>(100);
    }
}
```

#### Resource Management
```csharp
// PooledList for temporary allocations (particle systems, damage numbers)
public class ParticleManager
{
    void EmitBurst(Vector3 position, int count)
    {
        using (var particles = PooledList<Particle>.CreateWithArrayPool(count))
        {
            // Generate particles without allocation
            for (int i = 0; i < count; i++)
                particles.Add(CreateParticle(position));
            
            RenderParticles(particles);
        }  // Memory returned to pool automatically
    }
}

// PooledStack for undo/redo systems and state management
public class UndoRedoManager<T>
{
    void AddUndoState(T state, int maxUndoSteps = 50)
    {
        using (var undoStack = PooledStack<T>.CreateWithArrayPool(maxUndoSteps))
        {
            // Build undo chain without allocations
            undoStack.Push(state);
            
            // Process undo operations
            while (!undoStack.IsEmpty && ShouldUndo())
            {
                T previousState = undoStack.Pop();
                ApplyState(previousState);
            }
        }  // Memory returned to pool automatically
    }
}

// FastQueue for object pooling
public class ObjectPool<T> where T : Component
{
    private FastQueue<T> _available;
    
    public ObjectPool(int capacity)
    {
        _available = FastQueue<T>.CreateWithArrayPool(capacity);
        // Pre-warm pool
        for (int i = 0; i < capacity; i++)
            _available.Enqueue(CreateInstance());
    }
    
    public T Get() => _available.TryDequeue(out var obj) ? obj : CreateInstance();
    public void Return(T obj) => _available.Enqueue(obj);
}
```

#### World Generation and Grids
```csharp
// HexGrid for strategy games
public class HexMapController
{
    private HexGrid2D<Tile> _map;
    
    void GenerateMap(int radius)
    {
        _map = new HexGrid2D<Tile>(radius);
        foreach (var coord in _map.GetAllCoordinates())
        {
            _map[coord] = GenerateTile(coord);
        }
    }
    
    List<Tile> GetNeighbors(HexCoord coord)
    {
        return _map.GetNeighbors(coord);  // O(6) hex neighbors
    }
}

// BitGrid2D for fog of war or collision masks
public class FogOfWarSystem
{
    private BitGrid2D _explored;
    private BitGrid2D _visible;
    
    void Initialize(int mapWidth, int mapHeight)
    {
        _explored = new BitGrid2D(mapWidth, mapHeight);  // Minimal memory
        _visible = new BitGrid2D(mapWidth, mapHeight);
    }
    
    void RevealArea(int x, int y, int radius)
    {
        // Update visibility efficiently with bit operations
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            if (dx*dx + dy*dy <= radius*radius)
            {
                _visible[x + dx, y + dy] = true;
                _explored[x + dx, y + dy] = true;
            }
        }
    }
}
```

#### Performance-Critical Systems
```csharp
// CountMinSketch for tracking damage statistics without memory explosion
public class DamageTracker
{
    private CountMinSketch _damageBySource;
    
    void RecordDamage(string sourceId, int damage)
    {
        _damageBySource.Add(sourceId, damage);
        // Tracks millions of damage events in constant memory
    }
    
    long GetApproximateDamage(string sourceId)
    {
        return _damageBySource.EstimateCount(sourceId);
    }
}

// TimelineArray for replay system
public class ReplayRecorder
{
    private TimelineArray<GameState> _timeline;
    
    void Start()
    {
        _timeline = new TimelineArray<GameState>(30 * 60);  // 30 seconds at 60 FPS
    }
    
    void FixedUpdate()
    {
        _timeline.Record(Time.time, CaptureGameState());
    }
    
    void PlaybackFrom(float timestamp)
    {
        var states = _timeline.GetRange(timestamp - 0.5f, timestamp + 0.5f);
        // Interpolate between states for smooth playback
    }
}
```

</details>

### Spatial Query Optimization

<details>
<summary>Click to expand spatial query examples</summary>

```csharp
// Replace O(n) linear searches with O(log n) spatial queries
var locations = new QuadTree<Store>(worldBounds);
locations.Insert(store, storeLocation);

// Efficient range queries
var nearbyStores = locations.Query(userBounds);  // Scales logarithmically
```

</details>

### Memory-Conscious Queue Processing

<details>
<summary>Click to expand queue processing examples</summary>

```csharp
// Reduce allocation pressure in high-throughput scenarios
var orders = FastQueue<Order>.CreateWithArrayPool(capacity: 10000);

// Process without allocation spikes
while (orders.TryDequeue(out var order))
{
    ProcessOrder(order);  // ArrayPool reduces GC pressure
}
```

</details>

### Streaming Analytics

<details>
<summary>Click to expand streaming analytics examples</summary>

```csharp
// Track percentiles on unlimited streams with bounded memory
var latencyTracker = new TDigest(compression: 100);

foreach (var latency in stream)
{
    latencyTracker.Add(latency);
    
    if (latencyTracker.Count % 1000 == 0)
    {
        var p99 = latencyTracker.Quantile(0.99);  // O(1) query
        Console.WriteLine($"Current P99: {p99}ms");
    }
}
```

</details>

### High-Throughput Web API Processing

<details>
<summary>🚀 Real-world server performance with actual benchmark results</summary>

*Based on precision benchmarks: Intel i7-13700KF, .NET 8.0, 50K operations*

#### Request Processing Pipeline
```csharp
using Omni.Collections.Linear;

/// <summary>
/// Production web API request processor
/// FastQueue: Much faster dequeue operations with ArrayPool backing
/// MinHeap: Considerably faster priority extraction with minimal allocations
/// ArrayPool integration: Dramatic allocation reduction
/// </summary>
public class HighThroughputRequestProcessor : IDisposable
{
    private readonly FastQueue<ApiRequest> _normalQueue;
    private readonly MinHeap<PriorityRequest> _priorityQueue;
    private readonly FastQueue<ApiRequest> _deadLetterQueue;
    
    public HighThroughputRequestProcessor()
    {
        // ArrayPool backing for zero-allocation processing
        _normalQueue = FastQueue<ApiRequest>.CreateWithArrayPool(capacity: 100000);
        _priorityQueue = MinHeap<PriorityRequest>.CreateWithArrayPool(capacity: 10000);
        _deadLetterQueue = FastQueue<ApiRequest>.CreateWithArrayPool(capacity: 5000);
    }
    
    public void EnqueueRequest(ApiRequest request)
    {
        if (request.Priority > 0)
        {
            // O(log n) priority insertion - notably faster than baseline
            var priorityReq = new PriorityRequest
            {
                Request = request,
                Priority = request.Priority,
                EnqueuedAt = DateTime.UtcNow.Ticks
            };
            _priorityQueue.Insert(priorityReq);
        }
        else
        {
            // O(1) amortized - much faster than Queue<T>
            _normalQueue.Enqueue(request);
        }
    }
    
    public async Task<ProcessingStats> ProcessBatchAsync(int maxBatch = 1000)
    {
        var processed = 0;
        var errors = 0;
        var startTime = DateTime.UtcNow;
        
        // Process high-priority requests first
        // ExtractMin: Considerably faster than SortedSet baseline
        while (!_priorityQueue.IsEmpty && processed < maxBatch / 4)
        {
            var priorityReq = _priorityQueue.ExtractMin();
            
            try
            {
                await ProcessRequest(priorityReq.Request);
                processed++;
            }
            catch (Exception ex)
            {
                // Dead letter queue for retry processing
                _deadLetterQueue.Enqueue(priorityReq.Request);
                errors++;
                LogError(ex, priorityReq.Request);
            }
        }
        
        // Process normal queue - TryDequeue is non-blocking
        while (_normalQueue.TryDequeue(out var request) && processed < maxBatch)
        {
            try
            {
                await ProcessRequest(request);
                processed++;
            }
            catch (Exception ex)
            {
                _deadLetterQueue.Enqueue(request);
                errors++;
                LogError(ex, request);
            }
        }
        
        return new ProcessingStats
        {
            ProcessedCount = processed,
            ErrorCount = errors,
            ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
            QueueSizes = new QueueSizes
            {
                Normal = _normalQueue.Count,
                Priority = _priorityQueue.Count,
                DeadLetter = _deadLetterQueue.Count
            }
        };
    }
    
    private async Task ProcessRequest(ApiRequest request)
    {
        // Your business logic here
        await Task.Delay(request.EstimatedProcessingMs);
    }
    
    private void LogError(Exception ex, ApiRequest request)
    {
        // Error logging implementation
        Console.WriteLine($"Request {request.Id} failed: {ex.Message}");
    }
    
    public void Dispose()
    {
        _normalQueue?.Dispose();
        _priorityQueue?.Dispose();
        _deadLetterQueue?.Dispose();
    }
}

// Supporting classes
public class ApiRequest
{
    public string Id { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int EstimatedProcessingMs { get; set; } = 10;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

public class PriorityRequest : IComparable<PriorityRequest>
{
    public ApiRequest Request { get; set; } = null!;
    public int Priority { get; set; }
    public long EnqueuedAt { get; set; }
    
    public int CompareTo(PriorityRequest? other)
    {
        if (other == null) return 1;
        
        // Higher priority first (inverted comparison)
        var priorityCompare = other.Priority.CompareTo(Priority);
        if (priorityCompare != 0) return priorityCompare;
        
        // Earlier timestamp for tie-breaking
        return EnqueuedAt.CompareTo(other.EnqueuedAt);
    }
}

public class ProcessingStats
{
    public int ProcessedCount { get; set; }
    public int ErrorCount { get; set; }
    public double ProcessingTimeMs { get; set; }
    public QueueSizes QueueSizes { get; set; } = new();
}

public class QueueSizes
{
    public int Normal { get; set; }
    public int Priority { get; set; }
    public int DeadLetter { get; set; }
}
```

#### Performance Benchmarks (50K Operations)
*Hardware: Intel i7-13700KF, .NET 8.0*

| Operation | Standard Collection | Omni.Collections | Improvement |
|-----------|-------------------|------------------|-------------|
| **Queue Operations** |
| Enqueue | Queue<T> | FastQueue | **Much faster** |
| Dequeue | Queue<T> | FastQueue | **Significantly faster** |
| **Priority Operations** |
| Insert | SortedSet | MinHeap | **Notably faster** |
| ExtractMin | SortedSet | MinHeap | **Considerably faster** |
| **Memory Usage** |
| Allocations | Queue | FastQueue (pooled) | **Reduced for enqueue/dequeue** |
| Priority allocations | SortedSet | MinHeap (pooled) | **Reduced for insert/extract** |

#### Real-World Impact
- **Much faster request queuing** enables higher sustained throughput
- **Dramatic allocation reduction** reduces GC pressure in high-load scenarios  
- **Built-in error handling** with dead letter queue for reliability
- **Priority processing** ensures critical requests are handled first
- **ArrayPool integration** minimizes memory pressure during traffic spikes

</details>

## Installation

<details>
<summary>📦 Package installation and requirements</summary>

### Main Package (Recommended)
```bash
# Get all collections in one package
dotnet add package OmniCollections
```

### Focused Packages (Advanced)
```bash
# Install only what you need for smaller deployments
dotnet add package OmniCollections.Spatial      # Spatial indexing structures  
dotnet add package OmniCollections.Linear       # Performance-optimized linear structures
dotnet add package OmniCollections.Hybrid       # Dictionary variants
dotnet add package OmniCollections.Probabilistic # Analytics & approximation
```

### Requirements
- **.NET 8.0 or higher** (uses latest performance features)
- **No external dependencies** for core functionality  
- **System.Reactive** only needed for reactive structures (ObservableList, ObservableHashSet)

### Verify Installation
```csharp
using Omni.Collections.Linear;

// Quick test - should compile without errors
var queue = FastQueue<string>.CreateWithArrayPool(100);
queue.Enqueue("Hello Omni.Collections!");
Console.WriteLine(queue.Dequeue()); // "Hello Omni.Collections!"
```

</details>

## Performance Results & Benchmarking

### 🚀 Interactive Dashboard
**[View Live Performance Dashboard](https://codeturion.github.io/omni-collections/)** 

🎯 **Complete interactive benchmark results featuring:**
- **33 specialized data structures** with verified performance comparisons
- **4.8x average performance improvement** across all operations
- **200M+ total element operations** benchmarked for statistical significance
- **Interactive charts** with real-time filtering by category and metric type
- **Professional methodology** using BenchmarkDotNet precision profiling

**Hardware:** Intel i7-13700KF, 24 logical cores, .NET 8.0
**Methodology:** 20 iterations, statistical confidence intervals, comprehensive memory allocation tracking

### Benchmark Categories
All benchmarks compare Omni Collections against standard .NET baseline implementations:

### Linear Collections
- [FastQueue vs Queue](docs/benchmarks/linear/fastqueue-vs-queue.md)
- [MinHeap vs SortedSet](docs/benchmarks/linear/minheap-vs-sortedset.md)
- [MaxHeap vs SortedSet](docs/benchmarks/linear/maxheap-vs-sortedset.md)
- [PooledStack vs Stack](docs/benchmarks/linear/pooledstack-vs-stack.md)
- [PooledList vs List](docs/benchmarks/linear/pooledlist-vs-list.md)
- [BoundedList vs List](docs/benchmarks/linear/boundedlist-vs-list.md)

### Spatial Structures
- [QuadTree vs List](docs/benchmarks/spatial/quadtree-vs-list.md)
- [OctTree vs List](docs/benchmarks/spatial/octtree-vs-list.md)
- [KDTree vs List](docs/benchmarks/spatial/kdtree-vs-list.md)
- [KDTree Distance Metrics](docs/benchmarks/spatial/kdtree-distance-metrics.md)
- [SpatialHashGrid vs Dictionary](docs/benchmarks/spatial/spatialhashgrid-vs-dictionary.md)
- [TemporalSpatialHashGrid vs Manual](docs/benchmarks/spatial/temporalspatialhashgrid-vs-manual.md)
- [BloomRTreeDictionary vs Dictionary](docs/benchmarks/spatial/bloomrtreedictionary-vs-dictionary.md)
- [BloomRTree Scaling](docs/benchmarks/spatial/bloomrtree-scaling.md)

### Hybrid Structures
- [CounterDictionary vs Dictionary](docs/benchmarks/hybrid/counterdictionary-vs-dictionary.md)
- [LinkedDictionary vs Dictionary](docs/benchmarks/hybrid/linkeddictionary-vs-dictionary.md)
- [QueueDictionary vs Dictionary](docs/benchmarks/hybrid/queuedictionary-vs-dictionary.md)
- [CircularDictionary vs Dictionary](docs/benchmarks/hybrid/circulardictionary-vs-dictionary.md)
- [DequeDictionary vs Dictionary](docs/benchmarks/hybrid/dequedictionary-vs-dictionary.md)
- [ConcurrentLinkedDictionary vs Dictionary](docs/benchmarks/hybrid/concurrentlinkeddictionary-vs-dictionary.md)
- [LinkedMultiMap vs Dictionary](docs/benchmarks/hybrid/linkedmultimap-vs-dictionary.md)
- [GraphDictionary vs Dictionary](docs/benchmarks/hybrid/graphdictionary-vs-dictionary.md)
- [PredictiveDictionary vs Dictionary](docs/benchmarks/hybrid/predictivedictionary-vs-dictionary.md)

### Probabilistic Structures
- [BloomFilter vs HashSet](docs/benchmarks/probabilistic/bloomfilter-vs-hashset.md)
- [BloomDictionary vs Dictionary](docs/benchmarks/probabilistic/bloomdictionary-vs-dictionary.md)
- [CountMinSketch vs Dictionary](docs/benchmarks/probabilistic/countminsketch-vs-dictionary.md)
- [HyperLogLog vs HashSet](docs/benchmarks/probabilistic/hyperloglog-vs-hashset.md)
- [TDigest vs List](docs/benchmarks/probabilistic/tdigest-vs-list.md)
- [DigestStreaming vs P2Quantile](docs/benchmarks/probabilistic/digeststreaming-vs-p2quantile.md)

### Grid Structures
- [BitGrid2D vs BoolArray](docs/benchmarks/grid/bitgrid2d-vs-boolarray.md)
- [LayeredGrid2D vs Array3D](docs/benchmarks/grid/layeredgrid2d-vs-array3d.md)
- [HexGrid2D vs Dictionary](docs/benchmarks/grid/hexgrid2d-vs-dictionary.md)

### Reactive Structures
- [ObservableList vs List](docs/benchmarks/reactive/observablelist-vs-list.md)
- [ObservableHashSet vs HashSet](docs/benchmarks/reactive/observablehashset-vs-hashset.md)

### Temporal Structures
- [TimelineArray vs Dictionary](docs/benchmarks/temporal/timelinearray-vs-dictionary.md)

</details>

### Run Benchmarks Locally

Want to verify these results on your own hardware? Here's how to run the complete benchmark suite:

**Easy way (Windows):**
```bash
cd src/Omni.Collections.Benchmarks

# Run all benchmarks with precision profiling
run-benchmarks.bat all precision

# Run specific categories
run-benchmarks.bat linear precision          # FastQueue, MinHeap, etc.
run-benchmarks.bat spatial precision         # QuadTree, KDTree, etc. 
run-benchmarks.bat hybrid precision          # Dictionary variants

# Quick validation
run-benchmarks.bat all fast
```

**Manual way (cross-platform):**
```bash
cd src/Omni.Collections.Benchmarks

# Run specific categories  
dotnet run -- --precision --linear         # Linear collections
dotnet run -- --precision --spatial        # Spatial structures
dotnet run -- --precision --hybrid         # Hybrid dictionaries
dotnet run -- --precision --probabilistic  # Probabilistic structures

# Quick validation
dotnet run -- --fast --all
```

## Design Philosophy

1. **Algorithmic efficiency over micro-optimization** - Better algorithms scale better than faster loops
2. **Honest performance claims** - Benchmark results included, trade-offs documented
3. **Clean architecture** - SOLID principles, comprehensive testing, production-ready code
4. **Practical focus** - Solve real bottlenecks, not theoretical problems
5. **Transparent trade-offs** - Clear documentation of when NOT to use these structures

## Security Considerations

<details>
<summary>🔒 Hash collision attacks and security options</summary>

### Hash Collision Attacks
**The honest truth:** By default, our dictionaries prioritize performance over security.

**What this means:**
- Default mode is vulnerable to hash collision DoS attacks
- An attacker could force O(n²) behavior with crafted inputs
- This is the same trade-off as .NET's Dictionary<K,V>

**When to enable security:**
```csharp
// Internet-facing or untrusted input
var secure = new LinkedDictionary<string, Data>(
    hashOptions: SecureHashOptions.Production);

// Internal services or trusted data
var fast = new LinkedDictionary<string, Data>();  // Default, faster
```

**Performance cost:** Small throughput reduction with secure hashing
**Our recommendation:** Enable for public APIs, disable for internal processing

</details>

## Choosing the Right Structure

These structures solve specific problems - use them when you have those problems:

- **Start with .NET's built-in collections** for most scenarios - they're well-optimized and simple
- **Consider specialized structures** when you hit measurable bottlenecks (slow spatial queries, GC pressure, memory bounds)
- **ArrayPool variants** are most beneficial in high-throughput scenarios - evaluate if the complexity pays off
- **Benchmark your specific workload** - performance characteristics vary significantly based on usage patterns

The goal is solving real bottlenecks, not premature optimization.

## Contributing

<details>
<summary>🤝 Help make Omni.Collections even better</summary>

We welcome contributions! Here's how to get involved:

### 🐛 Report Issues
- **Bug Reports**: [Create an issue](https://github.com/Codeturion/omni-collections/issues/new?template=bug_report.md)
- **Feature Requests**: [Request a feature](https://github.com/Codeturion/omni-collections/issues/new?template=feature_request.md)  
- **Performance Issues**: Include benchmark results with your specific use case

### 💬 Join the Discussion
- **Questions**: [GitHub Discussions](https://github.com/Codeturion/omni-collections/discussions)
- **Ideas**: Share your use cases and performance challenges
- **Benchmarks**: Post your benchmark results with different hardware/scenarios

### 🔧 Contributing Code

#### Development Setup
```bash
git clone https://github.com/Codeturion/omni-collections.git
cd omni-collections

# Restore and build
dotnet restore
dotnet build

# Run all tests
dotnet test

# Run benchmarks (optional)
cd src/Omni.Collections.Benchmarks
dotnet run --configuration Release -- --filter "*YourStructure*"
```

#### Contribution Guidelines
- **Performance improvements** must include benchmark validation showing measurable gains
- **New structures** must solve documented real-world problems with clear algorithmic advantages
- **Maintain comprehensive test coverage** with both unit tests and benchmarks
- **Follow established code quality standards** including XML documentation and consistent naming
- **Include usage examples** demonstrating when to use the new structure

</details>

## License

MIT License - Use freely in production with attribution.

See [LICENSE](LICENSE) file for full terms.

## Acknowledgments

Architecture, design decisions, and algorithmic choices by me. AI assistance (Claude Code, ChatGPT and Gemini) helped with implementation details, documentation, and testing patterns throughout development.

---

**Bottom line:** These structures address two types of limits:
- **Algorithmic limits**: When O(n) doesn't scale (use spatial/tree structures)
- **Performance limits**: When allocations/overhead matter (use pooled/bounded variants)

Measure your actual bottlenecks before choosing specialized tools.