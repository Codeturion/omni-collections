```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
13th Gen Intel Core i7-13700KF, 1 CPU, 24 logical and 16 physical cores
.NET SDK 9.0.200
  [Host] : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2

Job=Precision  Jit=RyuJit  Platform=X64  
Runtime=.NET 8.0  Concurrent=False  Force=True  
Server=False  Toolchain=InProcessEmitToolchain  IterationCount=20  
LaunchCount=1  RunStrategy=Throughput  WarmupCount=10  

```
| Method                                                  | DataSize | Mean       | Error    | StdDev   | Median     | Min        | Max        | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------------------------- |--------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;KDTree Nearest Neighbor - Euclidean Distance&#39;          | 10000    |   415.4 ns | 10.13 ns | 10.40 ns |   420.1 ns |   390.5 ns |   426.5 ns |  1.00 |    0.00 | 0.0672 |      - |    1057 B |        1.00 |
| &#39;KDTree Nearest Neighbor - Manhattan Distance&#39;          | 10000    |   553.9 ns |  6.01 ns |  6.92 ns |   555.1 ns |   534.9 ns |   565.9 ns |  1.33 |    0.04 | 0.0715 |      - |    1125 B |        1.06 |
| &#39;KDTree Nearest Neighbor - Chebyshev Distance&#39;          | 10000    |   511.8 ns |  3.49 ns |  3.73 ns |   511.1 ns |   505.3 ns |   520.5 ns |  1.23 |    0.04 | 0.0610 |      - |     959 B |        0.91 |
| &#39;KDTree Nearest Neighbor - Minkowski Distance (p=1)&#39;    | 10000    |   546.9 ns | 11.24 ns | 12.95 ns |   545.0 ns |   531.2 ns |   574.1 ns |  1.31 |    0.05 | 0.0715 |      - |    1125 B |        1.06 |
| &#39;KDTree Nearest Neighbor - Minkowski Distance (p=3)&#39;    | 10000    | 1,413.5 ns | 16.95 ns | 19.52 ns | 1,409.3 ns | 1,385.0 ns | 1,450.6 ns |  3.40 |    0.10 | 0.0648 |      - |    1046 B |        0.99 |
| &#39;KDTree K-Nearest Neighbors (k=5) - Euclidean Distance&#39; | 10000    | 1,333.1 ns | 63.92 ns | 73.62 ns | 1,371.9 ns | 1,181.9 ns | 1,406.8 ns |  3.23 |    0.20 | 0.1507 |      - |    2369 B |        2.24 |
| &#39;KDTree K-Nearest Neighbors (k=5) - Manhattan Distance&#39; | 10000    | 1,538.8 ns | 26.55 ns | 30.57 ns | 1,522.9 ns | 1,507.5 ns | 1,597.4 ns |  3.71 |    0.13 | 0.1678 |      - |    2657 B |        2.51 |
| &#39;KDTree K-Nearest Neighbors (k=5) - Chebyshev Distance&#39; | 10000    | 1,402.5 ns | 28.59 ns | 32.93 ns | 1,408.7 ns | 1,360.4 ns | 1,446.7 ns |  3.36 |    0.14 | 0.1354 |      - |    2153 B |        2.04 |
| &#39;KDTree Range Query - Euclidean Distance&#39;               | 10000    | 3,549.8 ns | 60.68 ns | 64.93 ns | 3,553.0 ns | 3,451.3 ns | 3,663.5 ns |  8.57 |    0.28 | 0.8240 | 0.0076 |   12955 B |       12.26 |
| &#39;KDTree Range Query - Manhattan Distance&#39;               | 10000    | 3,770.4 ns | 51.24 ns | 59.01 ns | 3,787.4 ns | 3,672.1 ns | 3,852.3 ns |  9.06 |    0.29 | 0.6561 | 0.0038 |   10343 B |        9.79 |
| &#39;KDTree Range Query - Chebyshev Distance&#39;               | 10000    | 4,640.7 ns | 72.24 ns | 83.19 ns | 4,643.9 ns | 4,524.8 ns | 4,744.8 ns | 11.20 |    0.28 | 0.8392 | 0.0076 |   13253 B |       12.54 |
