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
| Method                                                                                      | Size  | Mean      | Error      | StdDev     | Median   | Min       | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------------------------------------------------------------------- |------ |----------:|-----------:|-----------:|---------:|----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;HyperLogLog Add operation&#39;                                                                 | 50000 | 23.465 μs | 28.4584 μs | 32.7727 μs | 3.650 μs | 2.5000 μs | 109.700 μs |  7.53 |   11.29 |    1784 B |        1.04 |
| &#39;HashSet Add operation (baseline)&#39;                                                          | 50000 |  3.083 μs |  0.2648 μs |  0.2834 μs | 3.150 μs | 2.7000 μs |   3.600 μs |  1.00 |    0.00 |    1720 B |        1.00 |
| &#39;HyperLogLog GetCardinality operation&#39;                                                      | 50000 |  3.483 μs |  1.2321 μs |  1.3183 μs | 2.850 μs | 2.2000 μs |   7.000 μs |  1.14 |    0.46 |     376 B |        0.22 |
| &#39;HashSet Count operation (baseline)&#39;                                                        | 50000 |  2.237 μs |  0.3576 μs |  0.3975 μs | 2.200 μs | 1.4000 μs |   3.000 μs |  0.73 |    0.17 |     136 B |        0.08 |
| &#39;HashSet Remove operation (baseline)&#39;                                                       | 50000 |  3.356 μs |  0.5081 μs |  0.5437 μs | 3.250 μs | 2.6000 μs |   4.600 μs |  1.10 |    0.20 |    1720 B |        1.00 |
| &#39;HyperLogLog EstimateCardinality access (NOTE: Cannot enumerate - probabilistic structure)&#39; | 50000 |  1.400 μs |  0.2078 μs |  0.2309 μs | 1.400 μs | 1.0000 μs |   1.800 μs |  0.46 |    0.10 |    1696 B |        0.99 |
| &#39;HashSet Count access (baseline)&#39;                                                           | 50000 |  1.232 μs |  0.1874 μs |  0.2083 μs | 1.300 μs | 0.9000 μs |   1.600 μs |  0.40 |    0.07 |    1696 B |        0.99 |
