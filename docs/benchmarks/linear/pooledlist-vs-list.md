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
| Method                               | Size  | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;PooledList Add&#39;                     | 50000 |  3.989 μs | 0.3778 μs | 0.4042 μs |  3.900 μs |  3.300 μs |  4.800 μs |  1.29 |    0.27 |      88 B |        0.08 |
| &#39;List Add operation (baseline)&#39;      | 50000 |  3.232 μs | 0.6411 μs | 0.7126 μs |  3.000 μs |  2.300 μs |  5.000 μs |  1.00 |    0.00 |    1096 B |        1.00 |
| &#39;PooledList indexer&#39;                 | 50000 |  2.144 μs | 0.4045 μs | 0.4328 μs |  2.150 μs |  1.500 μs |  2.900 μs |  0.69 |    0.20 |    1072 B |        0.98 |
| &#39;List indexer operation (baseline)&#39;  | 50000 |  1.859 μs | 0.1919 μs | 0.1970 μs |  1.800 μs |  1.500 μs |  2.300 μs |  0.59 |    0.12 |    1072 B |        0.98 |
| &#39;PooledList RemoveAt&#39;                | 50000 |  7.995 μs | 2.3274 μs | 2.6802 μs |  7.550 μs |  3.650 μs | 12.750 μs |  2.51 |    0.94 |    1072 B |        0.98 |
| &#39;List RemoveAt operation (baseline)&#39; | 50000 |  6.810 μs | 1.9460 μs | 2.2410 μs |  6.250 μs |  3.000 μs | 10.600 μs |  2.12 |    0.71 |    1360 B |        1.24 |
| &#39;PooledList enumeration&#39;             | 50000 | 20.222 μs | 0.8858 μs | 0.9478 μs | 19.950 μs | 18.800 μs | 21.900 μs |  6.53 |    1.33 |    1024 B |        0.93 |
| &#39;List enumeration (baseline)&#39;        | 50000 | 22.465 μs | 1.2310 μs | 1.4177 μs | 22.450 μs | 20.000 μs | 25.300 μs |  7.18 |    1.38 |    1072 B |        0.98 |
