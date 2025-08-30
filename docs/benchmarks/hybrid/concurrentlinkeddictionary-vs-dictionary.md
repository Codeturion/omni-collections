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
| Method                                                  | Size  | Mean       | Error      | StdDev     | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------------------------------- |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;ConcurrentLinkedDictionary TryAdd operation&#39;           | 50000 |  10.217 μs |  2.5072 μs |  2.6827 μs |   9.100 μs |   7.300 μs |  17.300 μs |  0.25 |    0.08 |    1128 B |        1.04 |
| &#39;ConcurrentDictionary TryAdd operation (baseline)&#39;      | 50000 |  41.385 μs |  4.4845 μs |  5.1644 μs |  40.150 μs |  30.700 μs |  50.900 μs |  1.00 |    0.00 |    1088 B |        1.00 |
| &#39;ConcurrentLinkedDictionary TryGetValue operation&#39;      | 50000 |   5.711 μs |  0.8692 μs |  0.9300 μs |   5.600 μs |   4.450 μs |   7.550 μs |  0.14 |    0.02 |    1384 B |        1.27 |
| &#39;ConcurrentDictionary TryGetValue operation (baseline)&#39; | 50000 |   5.147 μs |  0.4955 μs |  0.5088 μs |   5.100 μs |   4.200 μs |   6.200 μs |  0.13 |    0.02 |    1384 B |        1.27 |
| &#39;ConcurrentLinkedDictionary TryRemove operation&#39;        | 50000 |   5.305 μs |  1.6218 μs |  1.8026 μs |   4.300 μs |   3.400 μs |   7.700 μs |  0.13 |    0.04 |      64 B |        0.06 |
| &#39;ConcurrentDictionary TryRemove operation (baseline)&#39;   | 50000 |   8.435 μs |  1.7093 μs |  1.7553 μs |   7.700 μs |   6.600 μs |  12.300 μs |  0.21 |    0.06 |    1384 B |        1.27 |
| &#39;ConcurrentLinkedDictionary enumeration&#39;                | 50000 | 154.325 μs | 24.7351 μs | 28.4849 μs | 139.500 μs | 127.000 μs | 211.300 μs |  3.81 |    0.96 |    1128 B |        1.04 |
| &#39;ConcurrentDictionary enumeration (baseline)&#39;           | 50000 | 355.211 μs | 16.1310 μs | 17.2600 μs | 352.050 μs | 331.900 μs | 395.300 μs |  8.78 |    0.96 |    1424 B |        1.31 |
