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
| Method                                                                  | Size  | Mean       | Error      | StdDev     | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------------------------------------ |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;OctTree Range Query operation&#39;                                         | 50000 |   7.544 μs |  1.8684 μs |  1.9992 μs |   7.250 μs |   4.700 μs |  12.200 μs |  0.07 |    0.04 |     696 B |        0.40 |
| &#39;List Linear Search operation (baseline)&#39;                               | 50000 | 112.385 μs | 24.9448 μs | 28.7265 μs | 118.100 μs |  55.400 μs | 164.000 μs |  1.00 |    0.00 |    1720 B |        1.00 |
| &#39;OctTree Insert operation&#39;                                              | 50000 |   4.600 μs |  0.8728 μs |  0.9701 μs |   4.600 μs |   3.000 μs |   6.000 μs |  0.04 |    0.02 |    1720 B |        1.00 |
| &#39;List Add operation (baseline)&#39;                                         | 50000 |   3.411 μs |  0.6027 μs |  0.6699 μs |   3.200 μs |   2.500 μs |   5.200 μs |  0.03 |    0.01 |    1672 B |        0.97 |
| &#39;OctTree enumeration&#39;                                                   | 50000 |   1.533 μs |  0.1635 μs |  0.1749 μs |   1.500 μs |   1.200 μs |   1.800 μs |  0.01 |    0.00 |    1696 B |        0.99 |
| &#39;List enumeration (baseline)&#39;                                           | 50000 |   1.535 μs |  0.2148 μs |  0.2206 μs |   1.500 μs |   1.100 μs |   1.900 μs |  0.01 |    0.00 |     112 B |        0.07 |
| &#39;OctTree GetAllItems() traversal (separate from enumeration benchmark)&#39; | 50000 | 231.600 μs |  8.1981 μs |  9.1121 μs | 231.100 μs | 216.300 μs | 254.200 μs |  2.17 |    0.68 |    2936 B |        1.71 |
