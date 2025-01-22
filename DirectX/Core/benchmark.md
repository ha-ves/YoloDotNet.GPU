```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2894)
12th Gen Intel Core i5-1240P, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.102
  [Host]     : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2 [AttachedDebugger]
  DefaultJob : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2


```
| Method                     | Mean     | Error     | StdDev    | Ratio | RatioSD |
|--------------------------- |---------:|----------:|----------:|------:|--------:|
| ComputeSharp_2D_KeepAspect | 3.518 ms | 0.0659 ms | 0.0733 ms |  0.80 |    0.04 |
| ComputeSharp_2D_Stretch    | 3.400 ms | 0.0678 ms | 0.0832 ms |  0.77 |    0.04 |
| ComputeSharp_3D_KeepAspect | 3.552 ms | 0.0680 ms | 0.1506 ms |  0.81 |    0.05 |
| ComputeSharp_3D_Stretch    | 3.496 ms | 0.0694 ms | 0.1465 ms |  0.80 |    0.05 |
| Base (CPU SkiaSharp)                      | 4.401 ms | 0.0879 ms | 0.2316 ms |  1.00 |    0.07 |
