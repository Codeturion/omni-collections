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
| Method                                                 | Size  | Mean      | Error      | StdDev     | Median    | Min       | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------------------- |------ |----------:|-----------:|-----------:|----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;QueueDictionary Enqueue operation&#39;                    | 50000 |  7.785 μs |  1.9524 μs |  2.2483 μs |  8.200 μs |  5.100 μs |  12.500 μs |  1.31 |    0.53 |   1.35 KB |        1.00 |
| &#39;Dictionary+Queue manual operation (baseline)&#39;         | 50000 |  6.253 μs |  1.5864 μs |  1.7633 μs |  5.400 μs |  4.600 μs |  10.300 μs |  1.00 |    0.00 |   1.35 KB |        1.00 |
| &#39;QueueDictionary TryGetValue operation&#39;                | 50000 |  4.789 μs |  1.0997 μs |  1.1767 μs |  4.500 μs |  3.100 μs |   7.900 μs |  0.82 |    0.21 |   1.35 KB |        1.00 |
| &#39;Dictionary TryGetValue operation (baseline)&#39;          | 50000 |  3.706 μs |  0.4776 μs |  0.4905 μs |  3.700 μs |  2.900 μs |   5.000 μs |  0.64 |    0.17 |   1.35 KB |        1.00 |
| &#39;QueueDictionary Dequeue operation&#39;                    | 50000 |  4.071 μs |  0.8160 μs |  0.8380 μs |  3.900 μs |  3.200 μs |   6.000 μs |  0.69 |    0.17 |   1.36 KB |        1.01 |
| &#39;Dictionary+Queue manual dequeue operation (baseline)&#39; | 50000 |  7.524 μs |  0.7586 μs |  0.7790 μs |  7.200 μs |  6.600 μs |   9.700 μs |  1.30 |    0.32 |   1.08 KB |        0.80 |
| &#39;QueueDictionary enumeration&#39;                          | 50000 | 93.470 μs | 10.7004 μs | 12.3226 μs | 88.250 μs | 80.500 μs | 125.500 μs | 15.73 |    3.59 |   1.38 KB |        1.02 |
| &#39;Dictionary enumeration (baseline)&#39;                    | 50000 | 43.325 μs |  7.9159 μs |  9.1159 μs | 41.000 μs | 31.900 μs |  60.600 μs |  7.21 |    2.31 |   1.33 KB |        0.98 |
