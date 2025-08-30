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
| Method                                        | Size  | Mean      | Error      | StdDev     | Median    | Min       | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------------------- |------ |----------:|-----------:|-----------:|----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| &#39;LinkedDictionary Add operation&#39;              | 50000 |  9.026 μs |  3.2020 μs |  3.5590 μs |  8.100 μs |  5.500 μs |  17.500 μs |  1.59 |    0.79 |    1096 B |        0.79 |
| &#39;Dictionary Add operation (baseline)&#39;         | 50000 |  5.978 μs |  0.9747 μs |  1.0429 μs |  5.600 μs |  4.700 μs |   8.400 μs |  1.00 |    0.00 |    1384 B |        1.00 |
| &#39;LinkedDictionary TryGetValue operation&#39;      | 50000 |  5.245 μs |  1.4227 μs |  1.5813 μs |  5.250 μs |  2.950 μs |   8.550 μs |  0.86 |    0.26 |    1096 B |        0.79 |
| &#39;Dictionary TryGetValue operation (baseline)&#39; | 50000 |  4.717 μs |  0.5508 μs |  0.5894 μs |  4.650 μs |  4.000 μs |   6.000 μs |  0.81 |    0.15 |    1384 B |        1.00 |
| &#39;LinkedDictionary Remove operation&#39;           | 50000 |  4.372 μs |  0.8265 μs |  0.8844 μs |  4.100 μs |  3.500 μs |   6.500 μs |  0.75 |    0.20 |     424 B |        0.31 |
| &#39;Dictionary Remove operation (baseline)&#39;      | 50000 |  4.800 μs |  0.8197 μs |  0.9110 μs |  4.600 μs |  3.500 μs |   6.600 μs |  0.83 |    0.21 |    1384 B |        1.00 |
| &#39;LinkedDictionary enumeration&#39;                | 50000 | 73.878 μs | 18.9026 μs | 20.2256 μs | 71.050 μs | 50.600 μs | 119.100 μs | 12.76 |    4.16 |    1360 B |        0.98 |
| &#39;Dictionary enumeration (baseline)&#39;           | 50000 | 49.989 μs | 13.9212 μs | 14.8955 μs | 46.200 μs | 33.900 μs |  85.900 μs |  8.56 |    2.82 |    1360 B |        0.98 |
