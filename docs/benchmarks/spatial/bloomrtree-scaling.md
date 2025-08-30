```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
13th Gen Intel Core i7-13700KF, 1 CPU, 24 logical and 16 physical cores
.NET SDK 9.0.200
  [Host] : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2

Job=Precision  Jit=RyuJit  Platform=X64  
Runtime=.NET 8.0  Concurrent=False  Force=True  
Server=False  Toolchain=InProcessEmitToolchain  IterationCount=20  
LaunchCount=1  RunStrategy=Throughput  WarmupCount=10  

```
| Method                            | DataSize | Mean             | Error           | StdDev          | Median           | Min              | Max               | Gen0     | Gen1    | Allocated |
|---------------------------------- |--------- |-----------------:|----------------:|----------------:|-----------------:|-----------------:|------------------:|---------:|--------:|----------:|
| **&#39;Spatial queries with results&#39;**    | **1000**     |    **96,622.796 ns** |     **464.5784 ns** |     **535.0092 ns** |    **96,742.889 ns** |    **95,655.029 ns** |     **97,724.036 ns** |   **4.1504** |       **-** |   **66736 B** |
| &#39;Spatial queries with no results&#39; | 1000     |     3,861.439 ns |      25.9055 ns |      29.8328 ns |     3,856.373 ns |     3,805.743 ns |      3,924.564 ns |   0.8087 |       - |   12800 B |
| &#39;Repeated spatial queries&#39;        | 1000     |    82,908.310 ns |     286.5229 ns |     329.9602 ns |    82,850.806 ns |    82,208.008 ns |     83,557.727 ns |   4.8828 |       - |   77600 B |
| &#39;Get performance statistics&#39;      | 1000     |         6.150 ns |       0.0729 ns |       0.0811 ns |         6.095 ns |         6.080 ns |          6.304 ns |        - |       - |         - |
| **&#39;Spatial queries with results&#39;**    | **10000**    |   **713,309.600 ns** |   **3,050.4292 ns** |   **3,512.8788 ns** |   **711,796.387 ns** |   **708,467.188 ns** |    **719,643.652 ns** |  **25.3906** |       **-** |  **404898 B** |
| &#39;Spatial queries with no results&#39; | 10000    |     3,715.585 ns |      15.9035 ns |      16.3317 ns |     3,717.214 ns |     3,688.121 ns |      3,757.878 ns |   0.8125 |       - |   12800 B |
| &#39;Repeated spatial queries&#39;        | 10000    |   125,654.112 ns |   1,571.9664 ns |   1,747.2364 ns |   126,043.945 ns |   121,989.966 ns |    129,020.264 ns |   8.3008 |       - |  131200 B |
| &#39;Get performance statistics&#39;      | 10000    |         6.104 ns |       0.0198 ns |       0.0229 ns |         6.100 ns |         6.053 ns |          6.150 ns |        - |       - |         - |
| **&#39;Spatial queries with results&#39;**    | **100000**   | **9,833,978.281 ns** | **149,051.2252 ns** | **171,647.6126 ns** | **9,817,994.531 ns** | **9,490,478.125 ns** | **10,138,154.688 ns** | **296.8750** | **31.2500** | **4789018 B** |
| &#39;Spatial queries with no results&#39; | 100000   |     3,911.989 ns |      17.1120 ns |      19.0200 ns |     3,910.384 ns |     3,852.045 ns |      3,939.965 ns |   0.8087 |       - |   12800 B |
| &#39;Repeated spatial queries&#39;        | 100000   | 6,313,288.857 ns |  34,155.0792 ns |  37,963.2800 ns | 6,318,755.469 ns | 6,226,012.500 ns |  6,377,446.094 ns | 414.0625 | 39.0625 | 6596813 B |
| &#39;Get performance statistics&#39;      | 100000   |         6.253 ns |       0.0350 ns |       0.0403 ns |         6.270 ns |         6.140 ns |          6.295 ns |        - |       - |         - |
