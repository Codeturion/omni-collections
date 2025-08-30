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
| Method                                           | Distribution | Size  | Mean       | Error      | StdDev     | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------------- |------------- |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| **&#39;DigestStreaming AddValue operation&#39;**             | **Normal**       | **50000** | **8,129.4 ns** | **1,002.4 ns** | **1,029.4 ns** | **8,200.0 ns** | **6,100.0 ns** | **9,900.0 ns** |  **1.69** |    **0.43** |    **1432 B** |        **3.81** |
| &#39;P2 Quantile Estimator Add operation (baseline)&#39; | Normal       | 50000 | 4,988.9 ns | 1,263.2 ns | 1,351.6 ns | 4,700.0 ns | 3,100.0 ns | 8,200.0 ns |  1.00 |    0.00 |     376 B |        1.00 |
| &#39;DigestStreaming GetPercentile95 operation&#39;      | Normal       | 50000 | 5,743.8 ns |   627.6 ns |   616.4 ns | 5,850.0 ns | 4,400.0 ns | 6,800.0 ns |  1.24 |    0.37 |     712 B |        1.89 |
| &#39;P2 Quantile GetPercentile operation (baseline)&#39; | Normal       | 50000 | 3,588.2 ns |   579.1 ns |   594.6 ns | 3,400.0 ns | 2,500.0 ns | 5,200.0 ns |  0.75 |    0.21 |    1720 B |        4.57 |
| &#39;DigestStreaming Remove operation&#39;               | Normal       | 50000 | 2,115.8 ns |   468.7 ns |   521.0 ns | 1,900.0 ns | 1,600.0 ns | 3,400.0 ns |  0.44 |    0.14 |    1720 B |        4.57 |
| &#39;P2 Quantile Remove operation (baseline)&#39;        | Normal       | 50000 | 2,816.7 ns |   495.8 ns |   530.5 ns | 2,800.0 ns | 1,900.0 ns | 4,100.0 ns |  0.59 |    0.15 |    1720 B |        4.57 |
| &#39;DigestStreaming enumeration&#39;                    | Normal       | 50000 | 1,788.9 ns |   293.6 ns |   314.2 ns | 1,850.0 ns | 1,200.0 ns | 2,200.0 ns |  0.38 |    0.10 |    1696 B |        4.51 |
| &#39;P2 Quantile enumeration (baseline)&#39;             | Normal       | 50000 | 1,791.2 ns |   225.9 ns |   232.0 ns | 1,850.0 ns | 1,150.0 ns | 2,050.0 ns |  0.38 |    0.11 |     352 B |        0.94 |
|                                                  |              |       |            |            |            |            |            |            |       |         |           |             |
| **&#39;DigestStreaming AddValue operation&#39;**             | **Uniform**      | **50000** | **4,470.6 ns** |   **553.8 ns** |   **568.7 ns** | **4,400.0 ns** | **3,500.0 ns** | **5,600.0 ns** |  **1.66** |    **0.32** |    **1432 B** |        **3.81** |
| &#39;P2 Quantile Estimator Add operation (baseline)&#39; | Uniform      | 50000 | 2,752.9 ns |   401.7 ns |   412.5 ns | 2,700.0 ns | 2,200.0 ns | 3,700.0 ns |  1.00 |    0.00 |     376 B |        1.00 |
| &#39;DigestStreaming GetPercentile95 operation&#39;      | Uniform      | 50000 | 5,152.6 ns | 1,585.0 ns | 1,761.7 ns | 5,400.0 ns | 1,800.0 ns | 8,500.0 ns |  2.06 |    0.66 |     376 B |        1.00 |
| &#39;P2 Quantile GetPercentile operation (baseline)&#39; | Uniform      | 50000 | 2,688.9 ns |   837.1 ns |   895.7 ns | 2,500.0 ns | 1,500.0 ns | 4,600.0 ns |  1.01 |    0.33 |      88 B |        0.23 |
| &#39;DigestStreaming Remove operation&#39;               | Uniform      | 50000 | 2,081.6 ns |   406.9 ns |   452.2 ns | 2,050.0 ns | 1,250.0 ns | 2,950.0 ns |  0.79 |    0.18 |     376 B |        1.00 |
| &#39;P2 Quantile Remove operation (baseline)&#39;        | Uniform      | 50000 | 2,043.8 ns |   210.2 ns |   206.5 ns | 2,000.0 ns | 1,700.0 ns | 2,500.0 ns |  0.77 |    0.10 |    1720 B |        4.57 |
| &#39;DigestStreaming enumeration&#39;                    | Uniform      | 50000 | 1,372.2 ns |   385.7 ns |   412.7 ns | 1,500.0 ns |   400.0 ns | 2,000.0 ns |  0.53 |    0.16 |     352 B |        0.94 |
| &#39;P2 Quantile enumeration (baseline)&#39;             | Uniform      | 50000 | 1,284.2 ns |   313.5 ns |   348.4 ns | 1,300.0 ns |   500.0 ns | 1,700.0 ns |  0.51 |    0.12 |    1696 B |        4.51 |
|                                                  |              |       |            |            |            |            |            |            |       |         |           |             |
| **&#39;DigestStreaming AddValue operation&#39;**             | **Exponential**  | **50000** | **3,757.9 ns** |   **607.5 ns** |   **675.2 ns** | **3,600.0 ns** | **2,800.0 ns** | **5,300.0 ns** |  **1.66** |    **0.34** |    **1720 B** |        **0.97** |
| &#39;P2 Quantile Estimator Add operation (baseline)&#39; | Exponential  | 50000 | 2,315.8 ns |   443.1 ns |   492.5 ns | 2,100.0 ns | 1,700.0 ns | 3,500.0 ns |  1.00 |    0.00 |    1768 B |        1.00 |
| &#39;DigestStreaming GetPercentile95 operation&#39;      | Exponential  | 50000 | 2,185.0 ns |   344.2 ns |   396.4 ns | 2,000.0 ns | 1,700.0 ns | 3,000.0 ns |  0.99 |    0.31 |     376 B |        0.21 |
| &#39;P2 Quantile GetPercentile operation (baseline)&#39; | Exponential  | 50000 | 1,735.0 ns |   239.5 ns |   275.8 ns | 1,750.0 ns | 1,300.0 ns | 2,500.0 ns |  0.78 |    0.21 |    1720 B |        0.97 |
| &#39;DigestStreaming Remove operation&#39;               | Exponential  | 50000 | 1,310.0 ns |   270.1 ns |   311.0 ns | 1,300.0 ns |   800.0 ns | 1,900.0 ns |  0.59 |    0.19 |     376 B |        0.21 |
| &#39;P2 Quantile Remove operation (baseline)&#39;        | Exponential  | 50000 | 1,760.0 ns |   320.5 ns |   369.1 ns | 1,600.0 ns | 1,300.0 ns | 2,600.0 ns |  0.80 |    0.21 |      88 B |        0.05 |
| &#39;DigestStreaming enumeration&#39;                    | Exponential  | 50000 |   475.0 ns |   131.8 ns |   151.7 ns |   400.0 ns |   300.0 ns |   800.0 ns |  0.20 |    0.06 |     640 B |        0.36 |
| &#39;P2 Quantile enumeration (baseline)&#39;             | Exponential  | 50000 |   685.0 ns |   149.8 ns |   172.5 ns |   750.0 ns |   400.0 ns | 1,000.0 ns |  0.31 |    0.10 |     352 B |        0.20 |
