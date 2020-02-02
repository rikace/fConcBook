``` ini

BenchmarkDotNet=v0.11.5, OS=macOS 10.15 (19A583) [Darwin 19.0.0]
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.0.100
  [Host] : .NET Core 2.2.7 (CoreCLR 4.6.28008.02, CoreFX 4.6.28008.03), 64bit RyuJIT
  Core   : .NET Core 2.2.7 (CoreCLR 4.6.28008.02, CoreFX 4.6.28008.03), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|        Method |     N |        Mean |      Error |     StdDev | Ratio | Rank |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|-------------- |------ |------------:|-----------:|-----------:|------:|-----:|---------:|--------:|------:|----------:|
|    **Sequential** |  **1000** |    **12.70 us** |  **0.2473 us** |  **0.3625 us** |  **1.00** |    **1** |        **-** |       **-** |     **-** |         **-** |
|               |       |             |            |            |       |      |          |         |       |           |
|      Parallel |  1000 |   131.52 us |  1.7309 us |  1.6190 us |  1.00 |    1 |  31.2500 |  1.2207 |     - |     461 B |
|               |       |             |            |            |       |      |          |         |       |           |
| ParallelDepth |  1000 |    32.16 us |  0.6371 us |  1.4380 us |  1.00 |    1 |   7.7515 |  0.0610 |     - |    4690 B |
|               |       |             |            |            |       |      |          |         |       |           |
|    **Sequential** | **10000** |   **165.02 us** |  **3.2971 us** |  **7.0265 us** |  **1.00** |    **1** |        **-** |       **-** |     **-** |         **-** |
|               |       |             |            |            |       |      |          |         |       |           |
|      Parallel | 10000 | 1,222.25 us | 24.0429 us | 33.7048 us |  1.00 |    1 | 355.4688 | 25.3906 |     - |     480 B |
|               |       |             |            |            |       |      |          |         |       |           |
| ParallelDepth | 10000 |   152.83 us |  2.9895 us |  3.7808 us |  1.00 |    1 |  76.9043 |  1.4648 |     - |   31715 B |
