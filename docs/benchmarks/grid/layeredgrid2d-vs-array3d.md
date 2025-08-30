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
| Method                                         | Size  | Mean       | Error     | StdDev    | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------------------------- |------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;LayeredGrid2D Set&#39;                            | 50000 |   2.829 μs | 0.4241 μs | 0.4356 μs |   2.900 μs |   2.100 μs |   3.600 μs |  1.03 |    0.33 |    1432 B |       16.27 |
| &#39;LayeredGrid2D Set (ArrayPool.Shared)&#39;         | 50000 |   2.575 μs | 0.2508 μs | 0.2463 μs |   2.500 μs |   2.100 μs |   3.000 μs |  0.92 |    0.25 |     664 B |        7.55 |
| &#39;int[,,] Set operation (baseline)&#39;             | 50000 |   2.889 μs | 0.6745 μs | 0.7497 μs |   2.600 μs |   2.000 μs |   4.600 μs |  1.00 |    0.00 |      88 B |        1.00 |
| &#39;LayeredGrid2D Get&#39;                            | 50000 |   2.447 μs | 0.3596 μs | 0.3693 μs |   2.400 μs |   1.900 μs |   3.000 μs |  0.89 |    0.26 |    1432 B |       16.27 |
| &#39;LayeredGrid2D Get (ArrayPool.Shared)&#39;         | 50000 |   2.100 μs | 0.3578 μs | 0.3674 μs |   2.000 μs |   1.600 μs |   2.800 μs |  0.75 |    0.19 |     376 B |        4.27 |
| &#39;int[,,] Get operation (baseline)&#39;             | 50000 |   2.581 μs | 0.2268 μs | 0.2228 μs |   2.650 μs |   2.000 μs |   2.800 μs |  0.91 |    0.21 |    1432 B |       16.27 |
| &#39;LayeredGrid2D Reset&#39;                          | 50000 |   3.444 μs | 0.3863 μs | 0.4133 μs |   3.400 μs |   2.500 μs |   4.400 μs |  1.25 |    0.33 |    1728 B |       19.64 |
| &#39;LayeredGrid2D Reset (ArrayPool.Shared)&#39;       | 50000 |   3.617 μs | 0.6524 μs | 0.6981 μs |   3.400 μs |   2.600 μs |   5.100 μs |  1.34 |    0.45 |    1728 B |       19.64 |
| &#39;int[,,] Reset operation (baseline)&#39;           | 50000 |   3.933 μs | 0.4407 μs | 0.4715 μs |   3.850 μs |   3.300 μs |   4.700 μs |  1.43 |    0.37 |    1440 B |       16.36 |
| &#39;LayeredGrid2D enumeration&#39;                    | 50000 | 228.030 μs | 6.1230 μs | 7.0512 μs | 227.400 μs | 217.000 μs | 246.900 μs | 83.12 |   19.08 |    1696 B |       19.27 |
| &#39;LayeredGrid2D enumeration (ArrayPool.Shared)&#39; | 50000 | 228.032 μs | 5.6795 μs | 6.3128 μs | 227.000 μs | 216.500 μs | 238.200 μs | 83.62 |   19.89 |    1408 B |       16.00 |
| &#39;int[,,] enumeration (baseline)&#39;               | 50000 | 226.862 μs | 2.6792 μs | 2.7513 μs | 227.350 μs | 222.350 μs | 233.850 μs | 82.39 |   20.38 |    1696 B |       19.27 |
