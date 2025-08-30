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
| Method                                              | Size  | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------------------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;ObservableList Add operation&#39;                      | 50000 |  4.672 μs | 0.6047 μs | 0.6470 μs |  4.500 μs |  3.900 μs |  6.200 μs |  1.36 |    0.36 |    1528 B |        2.30 |
| &#39;List Add operation (baseline)&#39;                     | 50000 |  3.622 μs | 0.8018 μs | 0.8579 μs |  3.200 μs |  2.800 μs |  5.500 μs |  1.00 |    0.00 |     664 B |        1.00 |
| &#39;ObservableList indexer operation&#39;                  | 50000 |  3.182 μs | 0.5314 μs | 0.5457 μs |  3.000 μs |  2.500 μs |  4.300 μs |  0.90 |    0.22 |     376 B |        0.57 |
| &#39;List indexer operation (baseline)&#39;                 | 50000 |  3.250 μs | 0.7444 μs | 0.7965 μs |  3.150 μs |  2.300 μs |  5.000 μs |  0.94 |    0.31 |      88 B |        0.13 |
| &#39;ObservableList RemoveAt operation&#39;                 | 50000 | 10.589 μs | 1.5732 μs | 1.7486 μs |  9.900 μs |  8.700 μs | 14.500 μs |  3.04 |    0.87 |    1720 B |        2.59 |
| &#39;List RemoveAt operation (baseline)&#39;                | 50000 | 10.915 μs | 2.2268 μs | 2.5644 μs |  9.900 μs |  8.000 μs | 16.400 μs |  3.19 |    1.14 |    1720 B |        2.59 |
| &#39;ObservableList enumeration&#39;                        | 50000 | 25.218 μs | 1.9954 μs | 2.0492 μs | 25.200 μs | 20.900 μs | 29.600 μs |  7.21 |    1.55 |    1696 B |        2.55 |
| &#39;List enumeration (baseline)&#39;                       | 50000 | 22.665 μs | 1.1070 μs | 1.1368 μs | 23.100 μs | 20.600 μs | 23.900 μs |  6.49 |    1.38 |    1696 B |        2.55 |
| &#39;ObservableList Add (no event subscribers)&#39;         | 50000 |  2.035 μs | 0.2176 μs | 0.2234 μs |  2.000 μs |  1.700 μs |  2.500 μs |  0.58 |    0.15 |     376 B |        0.57 |
| &#39;ObservableList indexer (no event subscribers)&#39;     | 50000 |  2.412 μs | 0.3738 μs | 0.3839 μs |  2.400 μs |  1.600 μs |  3.100 μs |  0.68 |    0.15 |     376 B |        0.57 |
| &#39;ObservableList RemoveAt (no event subscribers)&#39;    | 50000 |  7.450 μs | 0.4747 μs | 0.5079 μs |  7.500 μs |  6.500 μs |  8.200 μs |  2.15 |    0.44 |    1696 B |        2.55 |
| &#39;ObservableList enumeration (no event subscribers)&#39; | 50000 | 24.926 μs | 1.6174 μs | 1.7978 μs | 24.400 μs | 22.700 μs | 28.800 μs |  7.15 |    1.35 |    1360 B |        2.05 |
