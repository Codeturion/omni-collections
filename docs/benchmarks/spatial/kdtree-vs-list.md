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
| Method                                                    | Size  | Mean         | Error       | StdDev      | Median       | Min          | Max          | Ratio  | RatioSD | Allocated  | Alloc Ratio |
|---------------------------------------------------------- |------ |-------------:|------------:|------------:|-------------:|-------------:|-------------:|-------:|--------:|-----------:|------------:|
| &#39;KDTree Insert operation&#39;                                 | 50000 |   5,383.3 ns |  1,496.1 ns |  1,600.8 ns |   4,950.0 ns |   3,600.0 ns |   9,400.0 ns |   1.53 |    0.44 |    3.14 KB |        1.87 |
| &#39;List Add operation (baseline)&#39;                           | 50000 |   3,458.8 ns |    588.4 ns |    604.2 ns |   3,400.0 ns |   2,800.0 ns |   5,100.0 ns |   1.00 |    0.00 |    1.68 KB |        1.00 |
| &#39;KDTree Nearest Neighbor Query operation&#39;                 | 50000 |  10,426.3 ns |  2,525.3 ns |  2,806.9 ns |   9,400.0 ns |   7,000.0 ns |  17,700.0 ns |   3.14 |    0.62 |    5.41 KB |        3.22 |
| &#39;List Linear Search operation (baseline)&#39;                 | 50000 | 641,986.8 ns | 27,234.6 ns | 30,271.2 ns | 642,250.0 ns | 589,350.0 ns | 693,750.0 ns | 190.02 |   30.52 | 1173.54 KB |      698.67 |
| &#39;KDTree Remove operation (NOT SUPPORTED - returns false)&#39; | 50000 |   3,088.9 ns |    978.6 ns |  1,047.1 ns |   2,650.0 ns |   1,700.0 ns |   5,700.0 ns |   0.94 |    0.42 |    1.68 KB |        1.00 |
| &#39;List Remove operation (baseline)&#39;                        | 50000 |  88,665.0 ns | 22,377.3 ns | 25,769.8 ns |  90,800.0 ns |  49,600.0 ns | 133,500.0 ns |  25.16 |    8.07 |    1.68 KB |        1.00 |
| &#39;KDTree enumeration&#39;                                      | 50000 |   1,900.0 ns |    226.7 ns |    242.5 ns |   1,900.0 ns |   1,200.0 ns |   2,300.0 ns |   0.57 |    0.09 |    1.38 KB |        0.82 |
| &#39;List enumeration (baseline)&#39;                             | 50000 |  58,447.4 ns | 13,280.2 ns | 14,760.9 ns |  51,000.0 ns |  44,500.0 ns |  96,200.0 ns |  17.16 |    4.99 |    1.38 KB |        0.82 |
| &#39;KDTree Count property&#39;                                   | 50000 |     876.5 ns |    180.6 ns |    185.5 ns |     900.0 ns |     500.0 ns |   1,200.0 ns |   0.26 |    0.07 |    1.66 KB |        0.99 |
