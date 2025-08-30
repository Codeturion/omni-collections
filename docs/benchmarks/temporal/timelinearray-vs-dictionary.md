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
| Method                                         | Size  | Mean        | Error        | StdDev      | Median       | Min         | Max          | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------------------------- |------ |------------:|-------------:|------------:|-------------:|------------:|-------------:|------:|--------:|----------:|------------:|
| &#39;TimelineArray SetAtTime&#39;                      | 50000 |  3,852.9 ns |    697.22 ns |    716.0 ns |   3,700.0 ns |  2,800.0 ns |   5,600.0 ns |  0.48 |    0.43 |    1432 B |        0.52 |
| &#39;TimelineArray SetAtTime (ArrayPool.Shared)&#39;   | 50000 |  3,505.9 ns |    622.07 ns |    638.8 ns |   3,500.0 ns |  2,500.0 ns |   5,200.0 ns |  0.44 |    0.38 |    2056 B |        0.74 |
| &#39;Dictionary SetAtTime operation (baseline)&#39;    | 50000 | 51,730.0 ns | 50,590.42 ns | 58,260.0 ns |   6,200.0 ns |  4,000.0 ns | 138,600.0 ns |  1.00 |    0.00 |    2768 B |        1.00 |
| &#39;TimelineArray GetAtTime&#39;                      | 50000 |  3,922.2 ns |    878.80 ns |    940.3 ns |   3,700.0 ns |  2,800.0 ns |   6,100.0 ns |  0.42 |    0.33 |    2056 B |        0.74 |
| &#39;TimelineArray GetAtTime (ArrayPool.Shared)&#39;   | 50000 |  2,983.3 ns |    450.21 ns |    481.7 ns |   3,100.0 ns |  2,000.0 ns |   3,700.0 ns |  0.34 |    0.28 |     376 B |        0.14 |
| &#39;Dictionary GetAtTime operation (baseline)&#39;    | 50000 | 99,818.4 ns | 13,431.52 ns | 14,929.1 ns | 103,650.0 ns | 63,250.0 ns | 127,150.0 ns | 11.99 |    9.48 |    2344 B |        0.85 |
| &#39;TimelineArray enumeration&#39;                    | 50000 |  1,178.9 ns |    248.35 ns |    276.0 ns |   1,100.0 ns |    800.0 ns |   1,700.0 ns |  0.15 |    0.13 |    2032 B |        0.73 |
| &#39;TimelineArray enumeration (ArrayPool.Shared)&#39; | 50000 |    552.6 ns |     91.79 ns |    102.0 ns |     600.0 ns |    300.0 ns |     700.0 ns |  0.06 |    0.05 |     352 B |        0.13 |
| &#39;Dictionary enumeration (baseline)&#39;            | 50000 |  1,178.9 ns |    197.97 ns |    220.0 ns |   1,100.0 ns |    800.0 ns |   1,700.0 ns |  0.13 |    0.10 |    2032 B |        0.73 |
