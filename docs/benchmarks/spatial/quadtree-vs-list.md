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
| Method                                    | Size  | Mean       | Error      | StdDev     | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------ |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;QuadTree Insert operation&#39;               | 50000 |   5.868 μs |  1.5835 μs |  1.7601 μs |   5.300 μs |   3.800 μs |   9.600 μs |  1.50 |    0.81 |   1.68 KB |        1.00 |
| &#39;List Add operation (baseline)&#39;           | 50000 |   4.306 μs |  0.8411 μs |  0.9000 μs |   4.300 μs |   2.400 μs |   6.000 μs |  1.00 |    0.00 |   1.68 KB |        1.00 |
| &#39;QuadTree Range Query operation&#39;          | 50000 |   7.412 μs |  0.6893 μs |  0.7079 μs |   7.500 μs |   6.200 μs |   9.000 μs |  1.78 |    0.48 |   2.03 KB |        1.21 |
| &#39;List Linear Search operation (baseline)&#39; | 50000 | 182.025 μs | 38.7387 μs | 44.6115 μs | 190.100 μs | 105.700 μs | 236.500 μs | 42.61 |   13.21 |   1.84 KB |        1.10 |
| &#39;QuadTree Remove operation&#39;               | 50000 |   3.867 μs |  0.8254 μs |  0.8832 μs |   3.700 μs |   2.700 μs |   5.700 μs |  0.94 |    0.33 |   1.68 KB |        1.00 |
| &#39;List RemoveAll operation (baseline)&#39;     | 50000 |  60.271 μs |  4.3745 μs |  4.8623 μs |  58.650 μs |  53.150 μs |  72.150 μs | 14.74 |    4.55 |   1.45 KB |        0.87 |
| &#39;QuadTree enumeration&#39;                    | 50000 |   1.258 μs |  0.1782 μs |  0.1981 μs |   1.200 μs |   1.000 μs |   1.700 μs |  0.30 |    0.07 |   1.66 KB |        0.99 |
| &#39;List enumeration (baseline)&#39;             | 50000 |   1.316 μs |  0.1903 μs |  0.2115 μs |   1.300 μs |   1.000 μs |   1.800 μs |  0.33 |    0.12 |   1.66 KB |        0.99 |
