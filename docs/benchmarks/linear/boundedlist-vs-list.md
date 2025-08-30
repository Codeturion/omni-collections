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
| Method                                       | Size  | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;BoundedList Add&#39;                            | 50000 |  5.930 μs | 1.3440 μs | 1.5477 μs |  5.900 μs |  2.850 μs |  9.150 μs |  1.13 |    0.34 |     712 B |        0.68 |
| &#39;BoundedList Add (ArrayPool.Shared)&#39;         | 50000 |  2.558 μs | 0.5240 μs | 0.5824 μs |  2.400 μs |  1.800 μs |  3.900 μs |  0.49 |    0.16 |     736 B |        0.70 |
| &#39;List Add operation (baseline)&#39;              | 50000 |  5.426 μs | 0.9805 μs | 1.0898 μs |  4.900 μs |  4.200 μs |  8.000 μs |  1.00 |    0.00 |    1048 B |        1.00 |
| &#39;BoundedList indexer&#39;                        | 50000 |  2.042 μs | 0.3371 μs | 0.3746 μs |  2.000 μs |  1.600 μs |  2.900 μs |  0.39 |    0.12 |      64 B |        0.06 |
| &#39;BoundedList indexer (ArrayPool.Shared)&#39;     | 50000 |  2.560 μs | 0.6863 μs | 0.7903 μs |  2.450 μs |  1.500 μs |  3.900 μs |  0.48 |    0.18 |    1024 B |        0.98 |
| &#39;List indexer operation (baseline)&#39;          | 50000 |  1.817 μs | 0.2425 μs | 0.2595 μs |  1.800 μs |  1.400 μs |  2.300 μs |  0.35 |    0.08 |    1024 B |        0.98 |
| &#39;BoundedList RemoveAt&#39;                       | 50000 |  7.767 μs | 2.0170 μs | 2.1582 μs |  8.050 μs |  4.300 μs | 11.800 μs |  1.50 |    0.58 |    1024 B |        0.98 |
| &#39;BoundedList RemoveAt (ArrayPool.Shared)&#39;    | 50000 |  6.344 μs | 1.9272 μs | 2.0620 μs |  6.800 μs |  2.400 μs |  9.400 μs |  1.20 |    0.45 |    1024 B |        0.98 |
| &#39;List RemoveAt operation (baseline)&#39;         | 50000 |  7.339 μs | 3.2513 μs | 3.4789 μs |  6.600 μs |  3.700 μs | 16.100 μs |  1.41 |    0.69 |    1024 B |        0.98 |
| &#39;BoundedList enumeration&#39;                    | 50000 | 19.529 μs | 0.6383 μs | 0.6555 μs | 19.500 μs | 18.600 μs | 20.700 μs |  3.67 |    0.64 |    1024 B |        0.98 |
| &#39;BoundedList enumeration (ArrayPool.Shared)&#39; | 50000 | 20.689 μs | 0.8854 μs | 0.9474 μs | 20.450 μs | 19.400 μs | 23.000 μs |  3.94 |    0.67 |    1024 B |        0.98 |
| &#39;List enumeration (baseline)&#39;                | 50000 | 23.658 μs | 1.2124 μs | 1.3476 μs | 23.500 μs | 21.800 μs | 26.900 μs |  4.51 |    0.80 |    1024 B |        0.98 |
