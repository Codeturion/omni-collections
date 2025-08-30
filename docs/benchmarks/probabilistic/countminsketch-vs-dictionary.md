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
| Method                                                                                | Size  | Mean      | Error     | StdDev    | Median    | Min        | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------------------------------------------------------------- |------ |----------:|----------:|----------:|----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| &#39;CountMinSketch Add operation&#39;                                                        | 50000 |  4.272 μs | 0.4153 μs | 0.4443 μs |  4.200 μs |  3.6000 μs |  5.200 μs |  1.11 |    0.21 |    1720 B |       19.55 |
| &#39;Dictionary Increment operation (baseline)&#39;                                           | 50000 |  4.025 μs | 0.9868 μs | 0.9692 μs |  3.800 μs |  2.8000 μs |  7.300 μs |  1.00 |    0.00 |      88 B |        1.00 |
| &#39;CountMinSketch GetEstimate operation&#39;                                                | 50000 |  3.959 μs | 0.6377 μs | 0.6548 μs |  4.100 μs |  2.6000 μs |  5.100 μs |  1.03 |    0.28 |    1720 B |       19.55 |
| &#39;Dictionary TryGetValue operation (baseline)&#39;                                         | 50000 |  3.744 μs | 0.3827 μs | 0.3759 μs |  3.700 μs |  3.0000 μs |  4.700 μs |  0.97 |    0.24 |    1720 B |       19.55 |
| &#39;Dictionary Decrement operation (baseline)&#39;                                           | 50000 |  4.176 μs | 0.8093 μs | 0.8311 μs |  4.200 μs |  2.9000 μs |  6.000 μs |  1.09 |    0.23 |    1720 B |       19.55 |
| &#39;CountMinSketch TotalCount access (NOTE: Cannot enumerate - probabilistic structure)&#39; | 50000 |  1.111 μs | 0.1500 μs | 0.1605 μs |  1.100 μs |  0.8000 μs |  1.400 μs |  0.28 |    0.05 |     352 B |        4.00 |
| &#39;Dictionary Count access (baseline)&#39;                                                  | 50000 | 60.422 μs | 4.6696 μs | 4.9964 μs | 58.650 μs | 54.8000 μs | 70.700 μs | 15.65 |    3.19 |     440 B |        5.00 |
