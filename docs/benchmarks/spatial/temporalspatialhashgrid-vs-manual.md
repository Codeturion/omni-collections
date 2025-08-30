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
| Method                                                       | Size  | Mean       | Error      | StdDev     | Median    | Min       | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------------------------- |------ |-----------:|-----------:|-----------:|----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;TemporalSpatialHashGrid Add operation&#39;                      | 50000 |  10.500 μs |  1.9816 μs |  2.0350 μs | 10.200 μs |  8.100 μs |  15.300 μs |  1.60 |    0.39 |    1720 B |        1.20 |
| &#39;Dict&lt;long, SpatialHashGrid&lt;T&gt;&gt; Add operation (baseline)&#39;    | 50000 |   7.289 μs |  2.4404 μs |  2.7125 μs |  6.400 μs |  4.800 μs |  14.200 μs |  1.00 |    0.00 |    1432 B |        1.00 |
| &#39;TemporalSpatialHashGrid QueryAtTime operation&#39;              | 50000 |   9.705 μs |  1.4586 μs |  1.6212 μs |  9.600 μs |  7.400 μs |  12.600 μs |  1.47 |    0.47 |    1840 B |        1.28 |
| &#39;Dict&lt;long, SpatialHashGrid&lt;T&gt;&gt; Query operation (baseline)&#39;  | 50000 |   4.900 μs |  1.8834 μs |  2.0934 μs |  3.700 μs |  2.800 μs |  10.500 μs |  0.73 |    0.33 |      88 B |        0.06 |
| &#39;TemporalSpatialHashGrid Remove operation&#39;                   | 50000 |   5.484 μs |  1.8982 μs |  2.1098 μs |  4.400 μs |  3.000 μs |   9.700 μs |  0.81 |    0.36 |    1720 B |        1.20 |
| &#39;Dict&lt;long, SpatialHashGrid&lt;T&gt;&gt; Remove operation (baseline)&#39; | 50000 |   4.094 μs |  0.5262 μs |  0.5631 μs |  4.000 μs |  3.300 μs |   5.300 μs |  0.65 |    0.22 |    1720 B |        1.20 |
| &#39;TemporalSpatialHashGrid enumeration&#39;                        | 50000 |   3.561 μs |  0.3600 μs |  0.3852 μs |  3.500 μs |  2.900 μs |   4.200 μs |  0.55 |    0.15 |    1720 B |        1.20 |
| &#39;Dict&lt;long, SpatialHashGrid&lt;T&gt;&gt; enumeration (baseline)&#39;      | 50000 | 107.679 μs | 19.1187 μs | 21.2504 μs | 99.000 μs | 85.700 μs | 160.400 μs | 16.24 |    5.50 |    1432 B |        1.00 |
