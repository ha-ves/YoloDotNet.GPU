```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2894)
12th Gen Intel Core i5-1240P, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.102
  [Host]     : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2 [AttachedDebugger]
  DefaultJob : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2


```
| Method                                | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------------- |---------:|----------:|----------:|---------:|------:|--------:|----------:|------------:|
| ComputeSharp_2D_KeepAspect            | 3.479 ms | 0.0660 ms | 0.0648 ms | 3.470 ms |  0.82 |    0.05 |     114 B |        0.18 |
| ComputeSharp_2D_Stretch               | 3.267 ms | 0.0644 ms | 0.0964 ms | 3.284 ms |  0.77 |    0.05 |     114 B |        0.18 |
| ComputeSharp_3D_KeepAspect            | 3.345 ms | 0.0664 ms | 0.0840 ms | 3.337 ms |  0.79 |    0.05 |     114 B |        0.18 |
| ComputeSharp_3D_Stretch               | 3.353 ms | 0.0664 ms | 0.0909 ms | 3.352 ms |  0.79 |    0.05 |     114 B |        0.18 |
| ComputeSharp_2D_KeepAspect_NoPostCopy | 1.054 ms | 0.0233 ms | 0.0681 ms | 1.074 ms |  0.25 |    0.02 |     112 B |        0.18 |
| ComputeSharp_2D_Stretch_NoPostCopy    | 1.042 ms | 0.0208 ms | 0.0537 ms | 1.029 ms |  0.25 |    0.02 |     112 B |        0.18 |
| ComputeSharp_3D_KeepAspect_NoPostCopy | 1.264 ms | 0.0260 ms | 0.0759 ms | 1.266 ms |  0.30 |    0.03 |     113 B |        0.18 |
| ComputeSharp_3D_Stretch_NoPostCopy    | 1.268 ms | 0.0252 ms | 0.0548 ms | 1.268 ms |  0.30 |    0.02 |     113 B |        0.18 |
| Basic                                 | 4.258 ms | 0.0932 ms | 0.2748 ms | 4.217 ms |  1.00 |    0.09 |     627 B |        1.00 |
