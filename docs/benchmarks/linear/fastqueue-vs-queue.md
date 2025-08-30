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
| Method                                     | Size  | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;FastQueue Enqueue&#39;                        | 50000 |  5.220 μs | 1.2062 μs | 1.3891 μs |  4.750 μs |  3.750 μs |  8.650 μs |  0.98 |    0.26 |    1384 B |        1.00 |
| &#39;FastQueue Enqueue (ArrayPool.Shared)&#39;     | 50000 |  2.442 μs | 0.3206 μs | 0.3564 μs |  2.400 μs |  2.000 μs |  3.200 μs |  0.47 |    0.10 |    1360 B |        0.98 |
| &#39;Queue Enqueue operation (baseline)&#39;       | 50000 |  5.367 μs | 0.8267 μs | 0.8845 μs |  5.250 μs |  4.100 μs |  7.100 μs |  1.00 |    0.00 |    1384 B |        1.00 |
| &#39;FastQueue Peek&#39;                           | 50000 |  2.597 μs | 0.3530 μs | 0.3625 μs |  2.550 μs |  1.950 μs |  3.350 μs |  0.49 |    0.09 |    1360 B |        0.98 |
| &#39;FastQueue Peek (ArrayPool.Shared)&#39;        | 50000 |  1.389 μs | 0.1631 μs | 0.1745 μs |  1.400 μs |  1.100 μs |  1.700 μs |  0.26 |    0.04 |     400 B |        0.29 |
| &#39;Queue Peek operation (baseline)&#39;          | 50000 |  3.039 μs | 0.5938 μs | 0.6354 μs |  3.100 μs |  1.500 μs |  4.100 μs |  0.58 |    0.17 |    1360 B |        0.98 |
| &#39;FastQueue Dequeue&#39;                        | 50000 |  2.889 μs | 0.4191 μs | 0.4484 μs |  2.800 μs |  2.200 μs |  3.700 μs |  0.54 |    0.08 |     352 B |        0.25 |
| &#39;FastQueue Dequeue (ArrayPool.Shared)&#39;     | 50000 |  1.389 μs | 0.2195 μs | 0.2349 μs |  1.350 μs |  1.000 μs |  1.900 μs |  0.26 |    0.05 |    1024 B |        0.74 |
| &#39;Queue Dequeue operation (baseline)&#39;       | 50000 |  2.450 μs | 0.3011 μs | 0.3222 μs |  2.500 μs |  1.400 μs |  2.800 μs |  0.47 |    0.11 |    1072 B |        0.77 |
| &#39;FastQueue enumeration&#39;                    | 50000 | 23.924 μs | 1.5706 μs | 1.7457 μs | 23.350 μs | 21.450 μs | 28.050 μs |  4.55 |    0.84 |    1360 B |        0.98 |
| &#39;FastQueue enumeration (ArrayPool.Shared)&#39; | 50000 | 21.926 μs | 1.1308 μs | 1.2569 μs | 21.900 μs | 19.800 μs | 25.000 μs |  4.21 |    0.72 |    1360 B |        0.98 |
| &#39;Queue enumeration (baseline)&#39;             | 50000 | 41.567 μs | 1.9895 μs | 2.1288 μs | 41.200 μs | 38.700 μs | 47.400 μs |  7.92 |    1.17 |    1072 B |        0.77 |
