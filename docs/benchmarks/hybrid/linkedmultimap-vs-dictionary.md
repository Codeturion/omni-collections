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
| Method                                                   | Size  | Mean       | Error     | StdDev    | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------------------------------- |------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;LinkedMultiMap Add operation&#39;                           | 50000 |  15.240 μs | 3.1213 μs | 3.5945 μs |  15.050 μs |   9.800 μs |  22.700 μs |  2.64 |    0.65 |     120 B |        1.36 |
| &#39;Dictionary&lt;K,List&lt;V&gt;&gt; Add operation (baseline)&#39;         | 50000 |   5.578 μs | 0.2749 μs | 0.2942 μs |   5.600 μs |   4.900 μs |   6.100 μs |  1.00 |    0.00 |      88 B |        1.00 |
| &#39;LinkedMultiMap indexer operation&#39;                       | 50000 |  13.440 μs | 3.8963 μs | 4.4870 μs |  13.550 μs |   6.500 μs |  21.900 μs |  2.31 |    0.72 |    1416 B |       16.09 |
| &#39;Dictionary&lt;K,List&lt;V&gt;&gt; TryGetValue operation (baseline)&#39; | 50000 |   5.530 μs | 1.4316 μs | 1.6486 μs |   4.700 μs |   3.400 μs |   9.200 μs |  0.96 |    0.25 |      88 B |        1.00 |
| &#39;LinkedMultiMap RemoveKey operation&#39;                     | 50000 |  16.625 μs | 5.4810 μs | 6.3120 μs |  17.000 μs |   5.800 μs |  25.600 μs |  3.03 |    1.07 |    1384 B |       15.73 |
| &#39;Dictionary&lt;K,List&lt;V&gt;&gt; Remove operation (baseline)&#39;      | 50000 |   6.230 μs | 1.5351 μs | 1.7679 μs |   6.150 μs |   4.000 μs |   9.800 μs |  1.11 |    0.31 |    1384 B |       15.73 |
| &#39;LinkedMultiMap enumeration&#39;                             | 50000 | 134.353 μs | 2.0473 μs | 2.2756 μs | 134.500 μs | 131.000 μs | 139.100 μs | 24.19 |    1.44 |    1416 B |       16.09 |
| &#39;Dictionary&lt;K,List&lt;V&gt;&gt; enumeration (baseline)&#39;           | 50000 |  42.059 μs | 2.7303 μs | 2.8038 μs |  40.900 μs |  38.700 μs |  49.400 μs |  7.55 |    0.66 |    1360 B |       15.45 |
