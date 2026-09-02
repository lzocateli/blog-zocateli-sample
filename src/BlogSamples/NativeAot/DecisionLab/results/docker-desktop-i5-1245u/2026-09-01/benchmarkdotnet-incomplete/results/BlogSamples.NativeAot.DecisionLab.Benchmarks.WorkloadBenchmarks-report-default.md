
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat) (container)
12th Gen Intel Core i5-1245U 2.50GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


 Method         | Job            | Runtime        | Mean     | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
--------------- |--------------- |--------------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
 ProcessarCarga | .NET 10.0      | .NET 10.0      | 2.913 ms | 0.2724 ms | 0.7990 ms |  1.08 |    0.42 |   8.77 KB |        1.00 |
 ProcessarCarga | NativeAOT 10.0 | NativeAOT 10.0 |       NA |        NA |        NA |     ? |       ? |        NA |           ? |

Benchmarks with issues:
  WorkloadBenchmarks.ProcessarCarga: NativeAOT 10.0(Runtime=NativeAOT 10.0)
