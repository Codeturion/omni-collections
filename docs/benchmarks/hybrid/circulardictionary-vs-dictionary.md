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
| Method                                            | Size  | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------------------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;CircularDictionary Add operation&#39;                | 50000 |  4.729 μs | 0.7244 μs | 0.7439 μs |  4.600 μs |  3.700 μs |  6.400 μs |  0.60 |    0.13 |    1096 B |        0.79 |
| &#39;Dictionary+manual eviction operation (baseline)&#39; | 50000 |  8.337 μs | 1.5733 μs | 1.7487 μs |  8.600 μs |  5.800 μs | 11.200 μs |  1.00 |    0.00 |    1384 B |        1.00 |
| &#39;CircularDictionary TryGetValue operation&#39;        | 50000 |  4.638 μs | 0.5412 μs | 0.5315 μs |  4.550 μs |  3.600 μs |  5.900 μs |  0.60 |    0.15 |    1384 B |        1.00 |
| &#39;Dictionary TryGetValue operation (baseline)&#39;     | 50000 |  5.434 μs | 0.7810 μs | 0.8681 μs |  5.250 μs |  4.350 μs |  7.650 μs |  0.67 |    0.12 |    1384 B |        1.00 |
| &#39;CircularDictionary Remove operation&#39;             | 50000 |  5.259 μs | 1.1579 μs | 1.1890 μs |  5.000 μs |  3.300 μs |  8.400 μs |  0.67 |    0.21 |      88 B |        0.06 |
| &#39;Dictionary Remove operation (baseline)&#39;          | 50000 |  4.041 μs | 0.7031 μs | 0.7220 μs |  3.900 μs |  3.000 μs |  5.500 μs |  0.52 |    0.18 |      88 B |        0.06 |
| &#39;CircularDictionary enumeration&#39;                  | 50000 |  6.967 μs | 0.7110 μs | 0.7608 μs |  7.050 μs |  5.500 μs |  8.100 μs |  0.88 |    0.22 |    1360 B |        0.98 |
| &#39;Dictionary enumeration (baseline)&#39;               | 50000 | 35.745 μs | 5.4653 μs | 6.2939 μs | 34.400 μs | 28.600 μs | 46.500 μs |  4.43 |    1.28 |    1360 B |        0.98 |
