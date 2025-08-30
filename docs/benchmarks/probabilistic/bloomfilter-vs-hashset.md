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
| Method                                        | MissRate | Size  | Mean       | Error    | StdDev   | Median     | Min        | Max        | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------------------- |--------- |------ |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| **&#39;BloomFilter Add&#39;**                             | **0.1**      | **50000** | **4,047.1 ns** | **319.5 ns** | **328.1 ns** | **4,000.0 ns** | **3,500.0 ns** | **4,800.0 ns** |  **1.28** |    **0.18** |      **88 B** |        **0.05** |
| &#39;BloomFilter Add (ArrayPool.Shared)&#39;          | 0.1      | 50000 | 2,047.1 ns | 201.1 ns | 206.5 ns | 2,000.0 ns | 1,700.0 ns | 2,500.0 ns |  0.64 |    0.10 |    1720 B |        1.00 |
| &#39;HashSet Add operation (baseline)&#39;            | 0.1      | 50000 | 3,188.9 ns | 235.3 ns | 251.8 ns | 3,250.0 ns | 2,700.0 ns | 3,600.0 ns |  1.00 |    0.00 |    1720 B |        1.00 |
| &#39;BloomFilter Contains&#39;                        | 0.1      | 50000 | 4,561.1 ns | 577.1 ns | 617.5 ns | 4,600.0 ns | 3,700.0 ns | 6,000.0 ns |  1.44 |    0.27 |    1720 B |        1.00 |
| &#39;BloomFilter Contains (ArrayPool.Shared)&#39;     | 0.1      | 50000 | 3,388.2 ns | 528.8 ns | 543.0 ns | 3,300.0 ns | 2,400.0 ns | 4,500.0 ns |  1.06 |    0.17 |    1720 B |        1.00 |
| &#39;HashSet Contains operation (baseline)&#39;       | 0.1      | 50000 | 3,927.8 ns | 544.8 ns | 582.9 ns | 3,750.0 ns | 3,200.0 ns | 5,500.0 ns |  1.24 |    0.23 |      88 B |        0.05 |
| &#39;HashSet Remove operation (baseline)&#39;         | 0.1      | 50000 | 3,252.9 ns | 637.7 ns | 654.9 ns | 3,100.0 ns | 2,400.0 ns | 5,000.0 ns |  1.02 |    0.20 |      88 B |        0.05 |
| &#39;BloomFilter Count access&#39;                    | 0.1      | 50000 | 1,288.9 ns | 194.7 ns | 208.3 ns | 1,300.0 ns |   900.0 ns | 1,700.0 ns |  0.41 |    0.08 |     352 B |        0.20 |
| &#39;BloomFilter Count access (ArrayPool.Shared)&#39; | 0.1      | 50000 |   800.0 ns | 157.0 ns | 168.0 ns |   800.0 ns |   500.0 ns | 1,100.0 ns |  0.25 |    0.06 |     112 B |        0.07 |
| &#39;HashSet Count access (baseline)&#39;             | 0.1      | 50000 | 1,277.8 ns | 214.0 ns | 229.0 ns | 1,300.0 ns |   800.0 ns | 1,700.0 ns |  0.40 |    0.09 |     352 B |        0.20 |
|                                               |          |       |            |          |          |            |            |            |       |         |           |             |
| **&#39;BloomFilter Add&#39;**                             | **0.5**      | **50000** | **2,833.3 ns** | **459.0 ns** | **491.1 ns** | **2,850.0 ns** | **1,400.0 ns** | **3,500.0 ns** |  **0.91** |    **0.28** |    **1720 B** |       **19.55** |
| &#39;BloomFilter Add (ArrayPool.Shared)&#39;          | 0.5      | 50000 | 2,366.7 ns | 425.3 ns | 455.0 ns | 2,300.0 ns | 1,700.0 ns | 3,300.0 ns |  0.77 |    0.25 |    1720 B |       19.55 |
| &#39;HashSet Add operation (baseline)&#39;            | 0.5      | 50000 | 3,288.9 ns | 784.5 ns | 839.4 ns | 3,150.0 ns | 1,500.0 ns | 4,900.0 ns |  1.00 |    0.00 |      88 B |        1.00 |
| &#39;BloomFilter Contains&#39;                        | 0.5      | 50000 | 2,923.7 ns | 586.0 ns | 651.4 ns | 2,950.0 ns | 1,150.0 ns | 3,850.0 ns |  0.96 |    0.23 |     376 B |        4.27 |
| &#39;BloomFilter Contains (ArrayPool.Shared)&#39;     | 0.5      | 50000 | 2,537.5 ns | 462.7 ns | 454.4 ns | 2,600.0 ns | 1,500.0 ns | 3,000.0 ns |  0.77 |    0.18 |     376 B |        4.27 |
| &#39;HashSet Contains operation (baseline)&#39;       | 0.5      | 50000 | 3,450.0 ns | 852.5 ns | 912.2 ns | 3,400.0 ns | 1,700.0 ns | 4,700.0 ns |  1.09 |    0.32 |     376 B |        4.27 |
| &#39;HashSet Remove operation (baseline)&#39;         | 0.5      | 50000 | 2,937.5 ns | 478.8 ns | 470.3 ns | 2,800.0 ns | 2,300.0 ns | 3,800.0 ns |  0.90 |    0.25 |    1720 B |       19.55 |
| &#39;BloomFilter Count access&#39;                    | 0.5      | 50000 |   957.9 ns | 307.8 ns | 342.1 ns | 1,000.0 ns |   200.0 ns | 1,400.0 ns |  0.31 |    0.09 |    1696 B |       19.27 |
| &#39;BloomFilter Count access (ArrayPool.Shared)&#39; | 0.5      | 50000 |   668.4 ns | 150.1 ns | 166.8 ns |   600.0 ns |   400.0 ns | 1,000.0 ns |  0.22 |    0.08 |    1696 B |       19.27 |
| &#39;HashSet Count access (baseline)&#39;             | 0.5      | 50000 | 1,038.9 ns | 314.3 ns | 336.3 ns | 1,100.0 ns |   300.0 ns | 1,500.0 ns |  0.33 |    0.11 |    1696 B |       19.27 |
|                                               |          |       |            |          |          |            |            |            |       |         |           |             |
| **&#39;BloomFilter Add&#39;**                             | **0.9**      | **50000** | **1,788.2 ns** | **188.2 ns** | **193.3 ns** | **1,800.0 ns** | **1,400.0 ns** | **2,100.0 ns** |  **0.92** |    **0.22** |    **1720 B** |        **4.57** |
| &#39;BloomFilter Add (ArrayPool.Shared)&#39;          | 0.9      | 50000 | 1,921.1 ns | 422.6 ns | 469.7 ns | 1,800.0 ns | 1,300.0 ns | 3,000.0 ns |  1.00 |    0.27 |     376 B |        1.00 |
| &#39;HashSet Add operation (baseline)&#39;            | 0.9      | 50000 | 1,983.3 ns | 358.8 ns | 383.9 ns | 1,950.0 ns | 1,100.0 ns | 2,700.0 ns |  1.00 |    0.00 |     376 B |        1.00 |
| &#39;BloomFilter Contains&#39;                        | 0.9      | 50000 | 1,695.0 ns | 375.3 ns | 432.2 ns | 1,550.0 ns | 1,150.0 ns | 2,450.0 ns |  0.93 |    0.37 |     376 B |        1.00 |
| &#39;BloomFilter Contains (ArrayPool.Shared)&#39;     | 0.9      | 50000 | 1,563.2 ns | 273.6 ns | 304.1 ns | 1,600.0 ns | 1,000.0 ns | 2,100.0 ns |  0.81 |    0.25 |      88 B |        0.23 |
| &#39;HashSet Contains operation (baseline)&#39;       | 0.9      | 50000 | 1,440.0 ns | 316.7 ns | 364.8 ns | 1,250.0 ns |   900.0 ns | 2,200.0 ns |  0.77 |    0.23 |    1720 B |        4.57 |
| &#39;HashSet Remove operation (baseline)&#39;         | 0.9      | 50000 | 2,005.3 ns | 285.3 ns | 317.1 ns | 2,000.0 ns | 1,500.0 ns | 2,600.0 ns |  1.06 |    0.36 |     376 B |        1.00 |
| &#39;BloomFilter Count access&#39;                    | 0.9      | 50000 |   415.0 ns | 106.4 ns | 122.6 ns |   400.0 ns |   250.0 ns |   650.0 ns |  0.22 |    0.10 |     352 B |        0.94 |
| &#39;BloomFilter Count access (ArrayPool.Shared)&#39; | 0.9      | 50000 |   365.0 ns | 110.1 ns | 126.8 ns |   350.0 ns |   250.0 ns |   750.0 ns |  0.19 |    0.08 |    1696 B |        4.51 |
| &#39;HashSet Count access (baseline)&#39;             | 0.9      | 50000 |   435.0 ns | 110.1 ns | 126.8 ns |   500.0 ns |   200.0 ns |   600.0 ns |  0.24 |    0.09 |     352 B |        0.94 |
