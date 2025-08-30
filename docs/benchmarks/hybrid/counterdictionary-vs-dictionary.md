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
| Method                                              | Size  | Mean       | Error     | StdDev    | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------------------------- |------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;CounterDictionary AddOrUpdate operation&#39;           | 50000 |  12.182 μs | 2.9729 μs | 3.3044 μs |  11.650 μs |   7.950 μs |  18.550 μs |  2.07 |    0.50 |    1176 B |        0.85 |
| &#39;Dictionary manual count operation (baseline)&#39;      | 50000 |   5.824 μs | 0.6689 μs | 0.6870 μs |   5.800 μs |   5.100 μs |   7.600 μs |  1.00 |    0.00 |    1384 B |        1.00 |
| &#39;CounterDictionary GetAccessCount operation&#39;        | 50000 |   4.268 μs | 1.0433 μs | 1.0714 μs |   4.350 μs |   2.950 μs |   6.750 μs |  0.73 |    0.16 |     376 B |        0.27 |
| &#39;Dictionary GetValueOrDefault operation (baseline)&#39; | 50000 |   4.916 μs | 0.9825 μs | 1.0920 μs |   4.700 μs |   3.700 μs |   7.400 μs |  0.84 |    0.15 |     376 B |        0.27 |
| &#39;CounterDictionary Remove operation&#39;                | 50000 |   4.915 μs | 1.5963 μs | 1.8383 μs |   4.150 μs |   3.100 μs |  10.000 μs |  0.83 |    0.27 |    1336 B |        0.97 |
| &#39;Dictionary Remove operation (baseline)&#39;            | 50000 |   5.389 μs | 1.4319 μs | 1.5916 μs |   4.900 μs |   3.300 μs |   8.700 μs |  0.92 |    0.21 |     376 B |        0.27 |
| &#39;CounterDictionary enumeration&#39;                     | 50000 | 125.737 μs | 8.9251 μs | 9.9202 μs | 122.300 μs | 115.300 μs | 148.900 μs | 21.68 |    2.84 |    1360 B |        0.98 |
| &#39;Dictionary enumeration (baseline)&#39;                 | 50000 |  27.365 μs | 3.7819 μs | 4.3552 μs |  25.650 μs |  22.400 μs |  35.600 μs |  4.73 |    1.02 |    1360 B |        0.98 |
