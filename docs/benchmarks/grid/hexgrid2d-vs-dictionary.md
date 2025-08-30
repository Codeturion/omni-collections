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
| Method                                                 | Size  | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;HexGrid2D Set operation&#39;                              | 50000 |  5.082 μs | 0.4385 μs | 0.4503 μs |  5.000 μs |  4.500 μs |  6.000 μs |  1.27 |    0.18 |      88 B |        0.06 |
| &#39;Dictionary&lt;HexCoord,int&gt; Set operation (baseline)&#39;    | 50000 |  4.025 μs | 0.4375 μs | 0.4297 μs |  3.900 μs |  3.200 μs |  4.900 μs |  1.00 |    0.00 |    1432 B |        1.00 |
| &#39;HexGrid2D Get operation&#39;                              | 50000 |  3.471 μs | 0.4642 μs | 0.5159 μs |  3.450 μs |  2.850 μs |  4.650 μs |  0.86 |    0.18 |    1720 B |        1.20 |
| &#39;Dictionary&lt;HexCoord,int&gt; Get operation (baseline)&#39;    | 50000 |  3.876 μs | 0.7332 μs | 0.7529 μs |  3.700 μs |  3.000 μs |  5.800 μs |  1.00 |    0.26 |     664 B |        0.46 |
| &#39;HexGrid2D Remove operation&#39;                           | 50000 |  4.255 μs | 0.5574 μs | 0.6196 μs |  4.250 μs |  3.150 μs |  5.750 μs |  1.08 |    0.23 |    1720 B |        1.20 |
| &#39;Dictionary&lt;HexCoord,int&gt; Remove operation (baseline)&#39; | 50000 |  4.300 μs | 0.8408 μs | 0.8997 μs |  4.250 μs |  2.600 μs |  6.100 μs |  1.10 |    0.17 |    1432 B |        1.00 |
| &#39;HexGrid2D enumeration&#39;                                | 50000 | 67.553 μs | 1.9409 μs | 1.9932 μs | 67.200 μs | 64.400 μs | 71.800 μs | 16.94 |    1.71 |    1736 B |        1.21 |
| &#39;Dictionary&lt;HexCoord,int&gt; enumeration (baseline)&#39;      | 50000 | 42.274 μs | 5.4298 μs | 6.0352 μs | 39.700 μs | 36.300 μs | 57.200 μs | 10.70 |    2.65 |    1696 B |        1.18 |
