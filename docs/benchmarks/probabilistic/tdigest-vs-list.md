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
| Method                                                                    | Size  | Mean     | Error     | StdDev    | Median   | Min      | Max      | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------------------------------------------------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| &#39;TDigest Add operation&#39;                                                   | 50000 | 6.050 μs | 0.5400 μs | 0.5545 μs | 5.950 μs | 4.950 μs | 7.250 μs |  1.58 |    0.54 |    1720 B |        1.00 |
| &#39;List Add operation (baseline)&#39;                                           | 50000 | 4.547 μs | 1.7216 μs | 1.9135 μs | 4.000 μs | 2.700 μs | 8.900 μs |  1.00 |    0.00 |    1720 B |        1.00 |
| &#39;TDigest GetQuantile operation&#39;                                           | 50000 | 3.876 μs | 0.6870 μs | 0.7636 μs | 3.550 μs | 2.950 μs | 5.750 μs |  0.96 |    0.33 |     376 B |        0.22 |
| &#39;List Sort+Percentile operation (baseline)&#39;                               | 50000 | 2.678 μs | 0.6902 μs | 0.7385 μs | 2.400 μs | 1.700 μs | 4.500 μs |  0.67 |    0.20 |    1720 B |        1.00 |
| &#39;List Remove operation (baseline)&#39;                                        | 50000 | 4.039 μs | 0.6338 μs | 0.6781 μs | 4.100 μs | 2.950 μs | 5.150 μs |  1.06 |    0.43 |    1720 B |        1.00 |
| &#39;TDigest Count access (NOTE: Cannot enumerate - probabilistic structure)&#39; | 50000 | 1.406 μs | 0.1520 μs | 0.1626 μs | 1.400 μs | 1.100 μs | 1.700 μs |  0.36 |    0.11 |    1696 B |        0.99 |
| &#39;List Count access (baseline)&#39;                                            | 50000 | 1.316 μs | 0.2833 μs | 0.3149 μs | 1.200 μs | 1.000 μs | 2.100 μs |  0.33 |    0.12 |    1696 B |        0.99 |
