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
| Method                                        | Size  | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;PredictiveDictionary indexer operation&#39;      | 50000 |  5.100 μs | 0.4673 μs | 0.4590 μs |  5.100 μs |  4.300 μs |  5.800 μs |  1.18 |    0.15 |    1384 B |        1.26 |
| &#39;Dictionary indexer operation (baseline)&#39;     | 50000 |  4.306 μs | 0.4270 μs | 0.4569 μs |  4.200 μs |  3.600 μs |  5.100 μs |  1.00 |    0.00 |    1096 B |        1.00 |
| &#39;PredictiveDictionary TryGetValue operation&#39;  | 50000 | 11.315 μs | 2.8543 μs | 3.2870 μs | 10.100 μs |  7.150 μs | 17.350 μs |  2.57 |    0.89 |    1480 B |        1.35 |
| &#39;Dictionary TryGetValue operation (baseline)&#39; | 50000 |  4.178 μs | 0.6827 μs | 0.7305 μs |  4.000 μs |  3.300 μs |  5.800 μs |  0.98 |    0.22 |    1384 B |        1.26 |
| &#39;PredictiveDictionary Remove operation&#39;       | 50000 |  5.322 μs | 1.3215 μs | 1.4140 μs |  4.750 μs |  3.800 μs |  8.300 μs |  1.25 |    0.35 |     376 B |        0.34 |
| &#39;Dictionary Remove operation (baseline)&#39;      | 50000 |  3.335 μs | 0.3626 μs | 0.3724 μs |  3.300 μs |  2.700 μs |  4.000 μs |  0.78 |    0.13 |     136 B |        0.12 |
| &#39;PredictiveDictionary enumeration&#39;            | 50000 |  1.494 μs | 0.3329 μs | 0.3418 μs |  1.500 μs |  1.000 μs |  2.100 μs |  0.35 |    0.09 |    1024 B |        0.93 |
| &#39;Dictionary enumeration (baseline)&#39;           | 50000 | 39.847 μs | 8.1416 μs | 9.0494 μs | 36.600 μs | 29.300 μs | 59.300 μs |  9.45 |    2.64 |    1360 B |        1.24 |
