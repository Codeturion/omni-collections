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
| Method                                          | Size  | Mean        | Error      | StdDev     | Median      | Min         | Max         | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------------ |------ |------------:|-----------:|-----------:|------------:|------------:|------------:|------:|--------:|----------:|------------:|
| &#39;SpatialHashGrid Insert operation&#39;              | 50000 |  5,817.6 ns | 1,057.4 ns | 1,085.8 ns |  5,400.0 ns |  4,900.0 ns |  9,200.0 ns |  0.28 |    0.04 |      88 B |        0.05 |
| &#39;Dictionary Insert operation (baseline)&#39;        | 50000 | 20,605.9 ns | 1,987.5 ns | 2,041.0 ns | 20,400.0 ns | 18,300.0 ns | 26,300.0 ns |  1.00 |    0.00 |    1760 B |        1.00 |
| &#39;SpatialHashGrid Range Query operation&#39;         | 50000 |  9,394.7 ns | 1,720.5 ns | 1,912.4 ns |  8,600.0 ns |  6,900.0 ns | 13,500.0 ns |  0.45 |    0.09 |    1840 B |        1.05 |
| &#39;Dictionary Linear Search operation (baseline)&#39; | 50000 |  4,893.8 ns |   634.7 ns |   623.4 ns |  4,800.0 ns |  4,100.0 ns |  6,400.0 ns |  0.24 |    0.04 |    1720 B |        0.98 |
| &#39;SpatialHashGrid Remove operation&#39;              | 50000 |  6,523.5 ns | 1,283.8 ns | 1,318.4 ns |  6,200.0 ns |  5,300.0 ns | 10,100.0 ns |  0.32 |    0.07 |    1720 B |        0.98 |
| &#39;Dictionary Remove operation (baseline)&#39;        | 50000 |  5,594.1 ns |   734.8 ns |   754.5 ns |  5,400.0 ns |  4,500.0 ns |  7,900.0 ns |  0.27 |    0.04 |    1720 B |        0.98 |
| &#39;SpatialHashGrid enumeration&#39;                   | 50000 |  2,250.0 ns |   404.2 ns |   432.5 ns |  2,250.0 ns |    950.0 ns |  3,150.0 ns |  0.11 |    0.02 |    1408 B |        0.80 |
| &#39;Dictionary enumeration (baseline)&#39;             | 50000 | 14,833.3 ns | 2,796.1 ns | 2,991.8 ns | 15,750.0 ns |  7,400.0 ns | 18,800.0 ns |  0.75 |    0.14 |    1736 B |        0.99 |
| &#39;SpatialHashGrid Insert (non-pooled)&#39;           | 50000 |  5,675.0 ns | 1,958.2 ns | 2,255.0 ns |  5,550.0 ns |  3,000.0 ns | 10,100.0 ns |  0.26 |    0.10 |    1720 B |        0.98 |
| &#39;SpatialHashGrid Range Query (non-pooled)&#39;      | 50000 |  7,161.1 ns |   967.6 ns | 1,035.3 ns |  6,950.0 ns |  5,800.0 ns |  9,700.0 ns |  0.35 |    0.05 |    1168 B |        0.66 |
| &#39;SpatialHashGrid Remove (non-pooled)&#39;           | 50000 |  4,706.2 ns |   622.6 ns |   611.5 ns |  4,800.0 ns |  3,700.0 ns |  6,000.0 ns |  0.23 |    0.04 |    1720 B |        0.98 |
| &#39;SpatialHashGrid enumeration (non-pooled)&#39;      | 50000 |    860.5 ns |   184.6 ns |   205.2 ns |    850.0 ns |    450.0 ns |  1,250.0 ns |  0.04 |    0.01 |    1696 B |        0.96 |
