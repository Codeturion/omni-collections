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
| &#39;MaxHeap Insert&#39;                            | 50000 |   5.180 μs | 0.8495 μs | 0.9782 μs |   4.950 μs |   3.450 μs |   7.450 μs |  1.17 |    0.27 |    1096 B |        0.96 |
| &#39;MaxHeap Insert (ArrayPool.Shared)&#39;         | 50000 |   3.892 μs | 0.8165 μs | 0.9076 μs |   3.650 μs |   2.750 μs |   6.050 μs |  0.88 |    0.27 |    1048 B |        0.92 |
| &#39;SortedSet Add operation (baseline)&#39;        | 50000 |   4.550 μs | 0.6221 μs | 0.6110 μs |   4.450 μs |   3.700 μs |   5.700 μs |  1.00 |    0.00 |    1136 B |        1.00 |
| &#39;MaxHeap PeekMax&#39;                           | 50000 |   4.389 μs | 1.0297 μs | 1.1018 μs |   4.300 μs |   2.400 μs |   7.400 μs |  0.98 |    0.20 |    1384 B |        1.22 |
| &#39;MaxHeap PeekMax (ArrayPool.Shared)&#39;        | 50000 |   2.576 μs | 0.5766 μs | 0.5922 μs |   2.500 μs |   1.700 μs |   3.800 μs |  0.59 |    0.17 |     376 B |        0.33 |
| &#39;SortedSet Max operation (baseline)&#39;        | 50000 |   4.606 μs | 0.7703 μs | 0.8242 μs |   4.550 μs |   3.000 μs |   6.600 μs |  1.03 |    0.25 |    1096 B |        0.96 |
| &#39;MaxHeap ExtractMax&#39;                        | 50000 |   5.021 μs | 0.9158 μs | 1.0179 μs |   4.600 μs |   4.000 μs |   7.100 μs |  1.11 |    0.27 |    1096 B |        0.96 |
| &#39;MaxHeap ExtractMax (ArrayPool.Shared)&#39;     | 50000 |   3.629 μs | 0.5253 μs | 0.5394 μs |   3.500 μs |   3.000 μs |   5.100 μs |  0.79 |    0.10 |     136 B |        0.12 |
| &#39;SortedSet Remove Max operation (baseline)&#39; | 50000 |   6.747 μs | 0.8892 μs | 0.9132 μs |   6.700 μs |   4.600 μs |   8.500 μs |  1.50 |    0.27 |    1384 B |        1.22 |
| &#39;MaxHeap enumeration&#39;                       | 50000 |  54.644 μs | 1.8135 μs | 1.9404 μs |  53.850 μs |  52.550 μs |  59.050 μs | 12.23 |    1.51 |    1112 B |        0.98 |
| &#39;MaxHeap enumeration (ArrayPool.Shared)&#39;    | 50000 |  56.195 μs | 2.7936 μs | 3.2171 μs |  55.950 μs |  51.900 μs |  64.200 μs | 12.48 |    1.76 |    1112 B |        0.98 |
| &#39;SortedSet enumeration (baseline)&#39;          | 50000 | 206.793 μs | 6.2414 μs | 5.8382 μs | 207.300 μs | 196.600 μs | 216.900 μs | 46.79 |    5.57 |    1352 B |        1.19 |
