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
| Method                                             | Size  | Mean       | Error      | StdDev     | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------------------------- |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;GraphDictionary AddNode operation&#39;                | 50000 |  13.037 μs |  2.4121 μs |  2.6810 μs |  12.600 μs |   9.000 μs |  18.800 μs |  1.20 |    0.46 |     128 B |        0.12 |
| &#39;Dictionary+Adjacency Add operation (baseline)&#39;    | 50000 |  11.832 μs |  2.7699 μs |  3.0788 μs |  13.000 μs |   6.700 μs |  19.100 μs |  1.00 |    0.00 |    1096 B |        1.00 |
| &#39;GraphDictionary TryGetValue operation&#39;            | 50000 |   7.335 μs |  0.5117 μs |  0.5255 μs |   7.400 μs |   6.500 μs |   8.300 μs |  0.62 |    0.16 |    1096 B |        1.00 |
| &#39;Dictionary TryGetValue operation (baseline)&#39;      | 50000 |   6.079 μs |  0.4661 μs |  0.5181 μs |   6.000 μs |   5.300 μs |   7.100 μs |  0.55 |    0.15 |     424 B |        0.39 |
| &#39;GraphDictionary RemoveNode operation&#39;             | 50000 |  15.079 μs |  7.1579 μs |  7.9560 μs |  13.300 μs |   6.600 μs |  33.200 μs |  1.32 |    0.70 |     424 B |        0.39 |
| &#39;Dictionary+Adjacency Remove operation (baseline)&#39; | 50000 | 306.860 μs | 53.7185 μs | 61.8623 μs | 282.750 μs | 226.600 μs | 419.700 μs | 27.68 |   10.05 |    1384 B |        1.26 |
| &#39;GraphDictionary enumeration&#39;                      | 50000 | 130.300 μs | 11.8854 μs | 13.2106 μs | 124.200 μs | 116.700 μs | 157.800 μs | 11.73 |    3.21 |    1072 B |        0.98 |
| &#39;Dictionary enumeration (baseline)&#39;                | 50000 |  54.635 μs |  2.5498 μs |  2.6184 μs |  54.000 μs |  52.200 μs |  62.000 μs |  4.61 |    0.98 |    1024 B |        0.93 |
