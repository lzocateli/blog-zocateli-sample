// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Contrato de entrada fixo usado pela API e pelos benchmarks.
// -----------------------------------------------------------------------

namespace BlogSamples.NativeAot.DecisionLab.Workload;

public sealed record WorkloadRequest(int Seed, int ItemCount, int Iterations)
{
    public static WorkloadRequest Default { get; } = new(42, 2_048, 8);
}