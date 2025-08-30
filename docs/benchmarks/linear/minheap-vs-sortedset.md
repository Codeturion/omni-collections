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
| Method                                      | Size  | Mean       | Error     | StdDev    | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------------------- |------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;MinHeap Insert&#39;                            | 50000 |   5.617 μs | 1.0123 μs | 1.0832 μs |   5.600 μs |   4.300 μs |   7.300 μs |  1.31 |    0.32 |    1048 B |        0.92 |
| &#39;MinHeap Insert (ArrayPool.Shared)&#39;         | 50000 |   3.425 μs | 0.7303 μs | 0.8410 μs |   3.100 μs |   2.500 μs |   5.500 μs |  0.79 |    0.26 |    1096 B |        0.96 |
| &#39;SortedSet Add operation (baseline)&#39;        | 50000 |   4.359 μs | 0.8328 μs | 0.8552 μs |   4.300 μs |   3.000 μs |   6.800 μs |  1.00 |    0.00 |    1136 B |        1.00 |
| &#39;MinHeap PeekMin&#39;                           | 50000 |   4.359 μs | 0.9219 μs | 0.9467 μs |   4.000 μs |   3.000 μs |   6.600 μs |  1.02 |    0.26 |      88 B |        0.08 |
| &#39;MinHeap PeekMin (ArrayPool.Shared)&#39;        | 50000 |   2.076 μs | 0.4960 μs | 0.5093 μs |   2.000 μs |   1.400 μs |   3.300 μs |  0.48 |    0.10 |     424 B |        0.37 |
| &#39;SortedSet Min operation (baseline)&#39;        | 50000 |   4.432 μs | 0.7330 μs | 0.7527 μs |   4.250 μs |   3.050 μs |   5.850 μs |  1.05 |    0.26 |    1096 B |        0.96 |
| &#39;MinHeap ExtractMin&#39;                        | 50000 |   4.806 μs | 0.7979 μs | 0.8537 μs |   4.500 μs |   3.800 μs |   6.600 μs |  1.15 |    0.30 |    1096 B |        0.96 |
| &#39;MinHeap ExtractMin (ArrayPool.Shared)&#39;     | 50000 |   3.276 μs | 0.3603 μs | 0.3700 μs |   3.300 μs |   2.200 μs |   4.000 μs |  0.77 |    0.13 |     472 B |        0.42 |
| &#39;SortedSet Remove Min operation (baseline)&#39; | 50000 |   6.469 μs | 0.7461 μs | 0.7328 μs |   6.600 μs |   5.100 μs |   7.800 μs |  1.49 |    0.28 |    1096 B |        0.96 |
| &#39;MinHeap enumeration&#39;                       | 50000 |  21.272 μs | 1.4474 μs | 1.5487 μs |  21.200 μs |  19.300 μs |  24.600 μs |  4.97 |    0.80 |    1072 B |        0.94 |
| &#39;MinHeap enumeration (ArrayPool.Shared)&#39;    | 50000 |  21.706 μs | 1.7530 μs | 1.8002 μs |  21.600 μs |  19.500 μs |  25.900 μs |  5.15 |    1.16 |    1072 B |        0.94 |
| &#39;SortedSet enumeration (baseline)&#39;          | 50000 | 203.882 μs | 4.4863 μs | 4.6071 μs | 202.500 μs | 196.700 μs | 217.600 μs | 48.31 |    8.74 |    1352 B |        1.19 |
