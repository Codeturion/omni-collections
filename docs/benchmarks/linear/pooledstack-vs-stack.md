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
| Method                            | Size  | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;PooledStack Push&#39;                | 50000 |  3.353 μs | 0.3340 μs | 0.3430 μs |  3.300 μs |  2.600 μs |  3.900 μs |  1.05 |    0.15 |     136 B |        0.12 |
| &#39;Stack Push operation (baseline)&#39; | 50000 |  3.212 μs | 0.2642 μs | 0.2713 μs |  3.200 μs |  2.800 μs |  3.700 μs |  1.00 |    0.00 |    1096 B |        1.00 |
| &#39;PooledStack Peek&#39;                | 50000 |  1.782 μs | 0.2986 μs | 0.3067 μs |  1.800 μs |  1.200 μs |  2.400 μs |  0.56 |    0.12 |     400 B |        0.36 |
| &#39;Stack Peek operation (baseline)&#39; | 50000 |  2.147 μs | 0.2556 μs | 0.2625 μs |  2.200 μs |  1.700 μs |  2.500 μs |  0.67 |    0.11 |     112 B |        0.10 |
| &#39;PooledStack Pop&#39;                 | 50000 |  2.215 μs | 0.5143 μs | 0.5923 μs |  1.900 μs |  1.500 μs |  3.500 μs |  0.68 |    0.15 |    1072 B |        0.98 |
| &#39;Stack Pop operation (baseline)&#39;  | 50000 |  1.806 μs | 0.3066 μs | 0.3280 μs |  1.750 μs |  1.300 μs |  2.400 μs |  0.56 |    0.11 |     352 B |        0.32 |
| &#39;PooledStack enumeration&#39;         | 50000 | 20.294 μs | 0.7703 μs | 0.8242 μs | 20.250 μs | 18.700 μs | 22.000 μs |  6.38 |    0.63 |    1360 B |        1.24 |
| &#39;Stack enumeration (baseline)&#39;    | 50000 | 35.400 μs | 1.2514 μs | 1.3390 μs | 35.300 μs | 33.400 μs | 38.500 μs | 11.03 |    0.88 |    1072 B |        0.98 |
