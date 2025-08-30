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
| Method                                                     | Size  | Mean       | Error     | StdDev    | Median     | Min       | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------------------------------------- |------ |-----------:|----------:|----------:|-----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;DequeDictionary Add operation&#39;                            | 50000 |   9.605 μs |  1.874 μs |  2.083 μs |  10.200 μs |  5.500 μs |  12.600 μs |  1.00 |    0.28 |     424 B |        0.29 |
| &#39;Dictionary+LinkedList manual operation (baseline)&#39;        | 50000 |   9.735 μs |  1.393 μs |  1.430 μs |   9.900 μs |  7.100 μs |  13.000 μs |  1.00 |    0.00 |    1440 B |        1.00 |
| &#39;DequeDictionary TryGetValue operation&#39;                    | 50000 |   5.074 μs |  1.125 μs |  1.250 μs |   4.700 μs |  3.400 μs |   8.200 μs |  0.51 |    0.11 |      88 B |        0.06 |
| &#39;Dictionary TryGetValue operation (baseline)&#39;              | 50000 |   5.211 μs |  1.060 μs |  1.134 μs |   4.850 μs |  3.600 μs |   8.300 μs |  0.55 |    0.18 |    1384 B |        0.96 |
| &#39;DequeDictionary Remove operation&#39;                         | 50000 |   7.117 μs |  1.153 μs |  1.233 μs |   7.000 μs |  5.600 μs |  10.000 μs |  0.74 |    0.15 |    1392 B |        0.97 |
| &#39;Dictionary+LinkedList manual remove operation (baseline)&#39; | 50000 |   6.982 μs |  1.231 μs |  1.369 μs |   6.550 μs |  5.450 μs |  10.250 μs |  0.72 |    0.14 |    1392 B |        0.97 |
| &#39;DequeDictionary enumeration&#39;                              | 50000 | 130.942 μs | 28.033 μs | 31.159 μs | 124.500 μs | 91.000 μs | 217.500 μs | 13.90 |    4.23 |    1416 B |        0.98 |
| &#39;Dictionary enumeration (baseline)&#39;                        | 50000 |  41.018 μs |  5.906 μs |  6.564 μs |  38.950 μs | 32.150 μs |  58.850 μs |  4.25 |    1.11 |    1360 B |        0.94 |
