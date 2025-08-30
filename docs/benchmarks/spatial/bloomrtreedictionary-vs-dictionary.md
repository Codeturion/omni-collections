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
| Method                                            | MissRate | Size  | Mean       | Error      | StdDev     | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------------------------- |--------- |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| **&#39;BloomRTreeDictionary Key Lookup&#39;**                 | **0.1**      | **50000** |   **6.305 μs** |  **0.4914 μs** |  **0.5462 μs** |   **6.200 μs** |   **5.500 μs** |   **7.600 μs** |  **1.07** |    **0.14** |    **2008 B** |        **1.40** |
| &#39;Dictionary Key Lookup (baseline)&#39;                | 0.1      | 50000 |   5.906 μs |  0.4550 μs |  0.4869 μs |   5.900 μs |   5.000 μs |   7.000 μs |  1.00 |    0.00 |    1432 B |        1.00 |
| &#39;BloomRTreeDictionary Add&#39;                        | 0.1      | 50000 |  35.480 μs | 18.7417 μs | 21.5829 μs |  34.200 μs |   9.100 μs |  88.700 μs |  5.75 |    3.84 |    1768 B |        1.23 |
| &#39;Dictionary Add (baseline)&#39;                       | 0.1      | 50000 |   6.884 μs |  0.7723 μs |  0.8585 μs |   6.900 μs |   5.800 μs |   9.000 μs |  1.17 |    0.16 |    1720 B |        1.20 |
| &#39;BloomRTreeDictionary Remove&#39;                     | 0.1      | 50000 |  18.935 μs |  6.8547 μs |  7.8939 μs |  19.300 μs |   5.600 μs |  32.200 μs |  3.06 |    1.26 |    1720 B |        1.20 |
| &#39;Dictionary Remove (baseline)&#39;                    | 0.1      | 50000 |   6.524 μs |  0.6671 μs |  0.7415 μs |   6.150 μs |   5.550 μs |   7.950 μs |  1.11 |    0.13 |     376 B |        0.26 |
| &#39;BloomRTreeDictionary Spatial Intersection Query&#39; | 0.1      | 50000 |  35.505 μs | 17.8991 μs | 20.6126 μs |  40.550 μs |   6.900 μs |  73.700 μs |  5.75 |    3.46 |   18200 B |       12.71 |
| &#39;Dictionary Linear Spatial Search (baseline)&#39;     | 0.1      | 50000 | 278.215 μs | 16.1868 μs | 18.6408 μs | 271.950 μs | 254.100 μs | 306.300 μs | 47.64 |    3.98 |    1432 B |        1.00 |
| &#39;BloomRTreeDictionary Point Query&#39;                | 0.1      | 50000 |  23.045 μs | 13.4292 μs | 15.4651 μs |  18.150 μs |   8.300 μs |  57.100 μs |  3.78 |    2.47 |    6128 B |        4.28 |
| &#39;Dictionary Linear Point Search (baseline)&#39;       | 0.1      | 50000 | 267.089 μs | 14.0302 μs | 15.5945 μs | 270.900 μs | 236.700 μs | 290.800 μs | 45.52 |    5.14 |    1432 B |        1.00 |
| &#39;BloomRTreeDictionary Performance Statistics&#39;     | 0.1      | 50000 |   3.678 μs |  0.3549 μs |  0.3797 μs |   3.700 μs |   3.100 μs |   4.600 μs |  0.63 |    0.09 |     760 B |        0.53 |
| &#39;BloomRTreeDictionary Bulk Add (AddRange)&#39;        | 0.1      | 50000 |  82.610 μs | 21.1796 μs | 24.3904 μs |  75.550 μs |  52.000 μs | 130.100 μs | 14.08 |    4.07 |   10296 B |        7.19 |
|                                                   |          |       |            |            |            |            |            |            |       |         |           |             |
| **&#39;BloomRTreeDictionary Key Lookup&#39;**                 | **0.5**      | **50000** |   **6.321 μs** |  **1.1331 μs** |  **1.2594 μs** |   **6.300 μs** |   **3.800 μs** |   **8.700 μs** |  **1.11** |    **0.26** |     **136 B** |        **1.55** |
| &#39;Dictionary Key Lookup (baseline)&#39;                | 0.5      | 50000 |   6.024 μs |  0.5787 μs |  0.5943 μs |   6.100 μs |   4.500 μs |   6.800 μs |  1.00 |    0.00 |      88 B |        1.00 |
| &#39;BloomRTreeDictionary Add&#39;                        | 0.5      | 50000 |  24.150 μs |  9.9394 μs | 10.6351 μs |  21.200 μs |   9.000 μs |  52.100 μs |  3.95 |    1.74 |    4984 B |       56.64 |
| &#39;Dictionary Add (baseline)&#39;                       | 0.5      | 50000 |   6.684 μs |  0.8852 μs |  0.9839 μs |   6.700 μs |   5.100 μs |   8.600 μs |  1.15 |    0.24 |    1720 B |       19.55 |
| &#39;BloomRTreeDictionary Remove&#39;                     | 0.5      | 50000 |  10.968 μs |  4.5126 μs |  5.0158 μs |  11.600 μs |   5.000 μs |  20.800 μs |  1.82 |    0.91 |     136 B |        1.55 |
| &#39;Dictionary Remove (baseline)&#39;                    | 0.5      | 50000 |   5.472 μs |  0.7131 μs |  0.7630 μs |   5.700 μs |   3.900 μs |   6.500 μs |  0.93 |    0.12 |      88 B |        1.00 |
| &#39;BloomRTreeDictionary Spatial Intersection Query&#39; | 0.5      | 50000 |  31.675 μs | 12.2461 μs | 14.1026 μs |  33.500 μs |   7.900 μs |  49.800 μs |  5.52 |    2.52 |   10272 B |      116.73 |
| &#39;Dictionary Linear Spatial Search (baseline)&#39;     | 0.5      | 50000 | 163.835 μs | 13.5455 μs | 15.5990 μs | 166.000 μs | 120.200 μs | 185.300 μs | 28.07 |    3.83 |    1432 B |       16.27 |
| &#39;BloomRTreeDictionary Point Query&#39;                | 0.5      | 50000 |  15.755 μs |  5.6839 μs |  6.5456 μs |  16.000 μs |   7.000 μs |  27.300 μs |  2.53 |    1.22 |    1376 B |       15.64 |
| &#39;Dictionary Linear Point Search (baseline)&#39;       | 0.5      | 50000 | 155.140 μs | 12.7454 μs | 14.6777 μs | 154.400 μs | 123.500 μs | 181.400 μs | 26.61 |    4.14 |    1384 B |       15.73 |
| &#39;BloomRTreeDictionary Performance Statistics&#39;     | 0.5      | 50000 |   3.022 μs |  0.5730 μs |  0.6131 μs |   3.000 μs |   1.800 μs |   4.100 μs |  0.52 |    0.11 |      88 B |        1.00 |
| &#39;BloomRTreeDictionary Bulk Add (AddRange)&#39;        | 0.5      | 50000 |  80.513 μs | 21.2021 μs | 20.8233 μs |  83.750 μs |  36.850 μs | 110.150 μs | 13.26 |    3.62 |   10296 B |      117.00 |
|                                                   |          |       |            |            |            |            |            |            |       |         |           |             |
| **&#39;BloomRTreeDictionary Key Lookup&#39;**                 | **0.9**      | **50000** |   **3.395 μs** |  **0.6491 μs** |  **0.7215 μs** |   **3.100 μs** |   **2.600 μs** |   **5.100 μs** |  **1.16** |    **0.36** |    **1720 B** |       **12.65** |
| &#39;Dictionary Key Lookup (baseline)&#39;                | 0.9      | 50000 |   3.109 μs |  0.6053 μs |  0.6215 μs |   2.950 μs |   2.050 μs |   4.850 μs |  1.00 |    0.00 |     136 B |        1.00 |
| &#39;BloomRTreeDictionary Add&#39;                        | 0.9      | 50000 |  11.215 μs |  6.9693 μs |  7.1570 μs |   7.450 μs |   5.950 μs |  32.750 μs |  3.65 |    2.15 |     424 B |        3.12 |
| &#39;Dictionary Add (baseline)&#39;                       | 0.9      | 50000 |   3.874 μs |  0.5609 μs |  0.6235 μs |   3.700 μs |   3.100 μs |   5.100 μs |  1.30 |    0.37 |     136 B |        1.00 |
| &#39;BloomRTreeDictionary Remove&#39;                     | 0.9      | 50000 |   3.682 μs |  0.7183 μs |  0.7376 μs |   3.500 μs |   2.900 μs |   5.600 μs |  1.22 |    0.37 |     376 B |        2.76 |
| &#39;Dictionary Remove (baseline)&#39;                    | 0.9      | 50000 |   3.105 μs |  0.3503 μs |  0.3894 μs |   3.100 μs |   2.500 μs |   3.900 μs |  1.03 |    0.24 |    1720 B |       12.65 |
| &#39;BloomRTreeDictionary Spatial Intersection Query&#39; | 0.9      | 50000 |   9.180 μs |  2.1588 μs |  2.4861 μs |   9.700 μs |   3.100 μs |  13.400 μs |  3.07 |    0.76 |    1584 B |       11.65 |
| &#39;Dictionary Linear Spatial Search (baseline)&#39;     | 0.9      | 50000 |  26.826 μs |  2.2459 μs |  2.3064 μs |  26.450 μs |  23.650 μs |  31.850 μs |  8.95 |    2.11 |    1432 B |       10.53 |
| &#39;BloomRTreeDictionary Point Query&#39;                | 0.9      | 50000 |   8.053 μs |  1.9698 μs |  2.1895 μs |   8.100 μs |   4.700 μs |  12.700 μs |  2.67 |    0.89 |    1416 B |       10.41 |
| &#39;Dictionary Linear Point Search (baseline)&#39;       | 0.9      | 50000 |  26.539 μs |  1.4840 μs |  1.5879 μs |  26.400 μs |  24.500 μs |  30.900 μs |  8.85 |    1.85 |    1384 B |       10.18 |
| &#39;BloomRTreeDictionary Performance Statistics&#39;     | 0.9      | 50000 |   2.044 μs |  0.2609 μs |  0.2791 μs |   2.000 μs |   1.700 μs |   2.600 μs |  0.67 |    0.13 |     376 B |        2.76 |
| &#39;BloomRTreeDictionary Bulk Add (AddRange)&#39;        | 0.9      | 50000 |  62.080 μs | 19.2629 μs | 22.1832 μs |  60.600 μs |  32.000 μs | 117.800 μs | 20.90 |    8.58 |   36408 B |      267.71 |
