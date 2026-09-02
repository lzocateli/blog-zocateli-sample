
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat) (container)
12th Gen Intel Core i5-1245U 2.50GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.400
  [Host]         : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  .NET 10.0      : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NativeAOT 10.0 : .NET 10.0.11, X64 NativeAOT x86-64-v3


 Method         | Job            | Runtime        | Mean     | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
--------------- |--------------- |--------------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
 ProcessarCarga | .NET 10.0      | .NET 10.0      | 1.083 ms | 0.0648 ms | 0.1828 ms |  1.03 |    0.24 |   8.77 KB |        1.00 |
 ProcessarCarga | NativeAOT 10.0 | NativeAOT 10.0 | 1.675 ms | 0.0747 ms | 0.2191 ms |  1.59 |    0.32 |   9.15 KB |        1.04 |
