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
| Method                                     | Size  | Mean       | Error     | StdDev    | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------- |------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;BitGrid2D Set&#39;                            | 50000 |   2.922 μs | 0.5685 μs | 0.6083 μs |   2.750 μs |   1.800 μs |   4.100 μs |  1.22 |    0.33 |    1384 B |       10.18 |
| &#39;BitGrid2D Set (ArrayPool.Shared)&#39;         | 50000 |   2.528 μs | 0.4881 μs | 0.5222 μs |   2.500 μs |   1.500 μs |   3.800 μs |  1.04 |    0.26 |     376 B |        2.76 |
| &#39;bool[,] Set operation (baseline)&#39;         | 50000 |   2.468 μs | 0.3313 μs | 0.3683 μs |   2.400 μs |   1.800 μs |   3.000 μs |  1.00 |    0.00 |     136 B |        1.00 |
| &#39;BitGrid2D Get&#39;                            | 50000 |   2.611 μs | 0.6049 μs | 0.6724 μs |   2.400 μs |   1.800 μs |   4.000 μs |  1.08 |    0.33 |    1720 B |       12.65 |
| &#39;BitGrid2D Get (ArrayPool.Shared)&#39;         | 50000 |   1.988 μs | 0.3134 μs | 0.3219 μs |   1.900 μs |   1.500 μs |   2.800 μs |  0.82 |    0.19 |    1720 B |       12.65 |
| &#39;bool[,] Get operation (baseline)&#39;         | 50000 |   2.288 μs | 0.3058 μs | 0.3140 μs |   2.200 μs |   1.800 μs |   2.800 μs |  0.94 |    0.23 |     136 B |        1.00 |
| &#39;BitGrid2D Reset&#39;                          | 50000 |   3.617 μs | 0.6777 μs | 0.7252 μs |   3.450 μs |   2.950 μs |   5.550 μs |  1.51 |    0.42 |    1720 B |       12.65 |
| &#39;BitGrid2D Reset (ArrayPool.Shared)&#39;       | 50000 |   3.267 μs | 0.4956 μs | 0.5303 μs |   3.250 μs |   2.400 μs |   4.200 μs |  1.36 |    0.31 |    1720 B |       12.65 |
| &#39;bool[,] Reset operation (baseline)&#39;       | 50000 |   3.647 μs | 0.4943 μs | 0.5076 μs |   3.600 μs |   2.800 μs |   4.600 μs |  1.50 |    0.36 |    1720 B |       12.65 |
| &#39;BitGrid2D enumeration&#39;                    | 50000 | 141.990 μs | 2.7246 μs | 3.1377 μs | 141.300 μs | 137.800 μs | 147.900 μs | 58.63 |    8.65 |    1408 B |       10.35 |
| &#39;BitGrid2D enumeration (ArrayPool.Shared)&#39; | 50000 | 138.556 μs | 1.6165 μs | 1.6600 μs | 138.850 μs | 134.250 μs | 140.650 μs | 56.75 |    8.87 |    1696 B |       12.47 |
| &#39;bool[,] enumeration (baseline)&#39;           | 50000 | 117.013 μs | 2.1734 μs | 2.4157 μs | 116.750 μs | 113.450 μs | 122.350 μs | 48.45 |    7.55 |    1696 B |       12.47 |
