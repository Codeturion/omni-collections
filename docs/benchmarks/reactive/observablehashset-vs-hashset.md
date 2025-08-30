```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
13th Gen Intel Core i7-13700KF, 1 CPU, 24 logical and 16 physical cores
.NET SDK 9.0.200
  [Host] : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2

Job=Precision  Jit=RyuJit  Platform=X64  
Runtime=.NET 8.0  Concurrent=False  Force=True  
Server=False  Toolchain=InProcessEmitToolchain  InvocationCount=1  
IterationCount=20  LaunchCount=1  RunStrategy=Throughput  
UnrollFactor=1  WarmupCount=10  

```
| Method                                             | Size  | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------------------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;ObservableHashSet Add&#39;                            | 50000 |  5.747 μs | 0.5574 μs | 0.5724 μs |  5.700 μs |  4.500 μs |  6.900 μs |  1.70 |    0.46 |     184 B |        0.11 |
| &#39;ObservableHashSet Add (EventPool.Shared)&#39;         | 50000 |  2.765 μs | 0.3229 μs | 0.3316 μs |  2.700 μs |  2.200 μs |  3.500 μs |  0.83 |    0.26 |    1792 B |        1.04 |
| &#39;HashSet Add operation (baseline)&#39;                 | 50000 |  3.747 μs | 0.9366 μs | 1.0410 μs |  3.700 μs |  2.200 μs |  5.600 μs |  1.00 |    0.00 |    1720 B |        1.00 |
| &#39;ObservableHashSet Contains&#39;                       | 50000 |  3.094 μs | 0.2655 μs | 0.2727 μs |  3.100 μs |  2.700 μs |  3.800 μs |  0.92 |    0.25 |    1432 B |        0.83 |
| &#39;ObservableHashSet Contains (EventPool.Shared)&#39;    | 50000 |  1.821 μs | 0.3640 μs | 0.3738 μs |  1.750 μs |  1.350 μs |  2.750 μs |  0.54 |    0.19 |    1696 B |        0.99 |
| &#39;HashSet Contains operation (baseline)&#39;            | 50000 |  2.688 μs | 0.4201 μs | 0.4314 μs |  2.700 μs |  1.900 μs |  3.600 μs |  0.81 |    0.28 |     376 B |        0.22 |
| &#39;ObservableHashSet Remove&#39;                         | 50000 |  6.276 μs | 0.7085 μs | 0.7276 μs |  6.100 μs |  5.200 μs |  7.500 μs |  1.85 |    0.48 |    1760 B |        1.02 |
| &#39;ObservableHashSet Remove (EventPool.Shared)&#39;      | 50000 |  4.989 μs | 0.7274 μs | 0.7783 μs |  4.750 μs |  3.700 μs |  6.600 μs |  1.45 |    0.39 |    1736 B |        1.01 |
| &#39;HashSet Remove operation (baseline)&#39;              | 50000 |  3.050 μs | 0.5284 μs | 0.5190 μs |  3.150 μs |  2.200 μs |  3.900 μs |  0.93 |    0.33 |     416 B |        0.24 |
| &#39;ObservableHashSet enumeration&#39;                    | 50000 | 36.803 μs | 4.3721 μs | 4.8596 μs | 35.650 μs | 32.150 μs | 47.050 μs | 10.68 |    3.67 |    1696 B |        0.99 |
| &#39;ObservableHashSet enumeration (EventPool.Shared)&#39; | 50000 | 32.671 μs | 3.0845 μs | 3.1676 μs | 32.200 μs | 28.900 μs | 41.700 μs |  9.74 |    2.75 |    1696 B |        0.99 |
| &#39;HashSet enumeration (baseline)&#39;                   | 50000 | 33.233 μs | 3.7632 μs | 4.0266 μs | 31.600 μs | 29.450 μs | 42.550 μs |  9.76 |    2.89 |    1696 B |        0.99 |
