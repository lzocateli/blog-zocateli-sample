// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Resultado determinístico compartilhado entre os modos de publicação.
// -----------------------------------------------------------------------

namespace BlogSamples.NativeAot.DecisionLab.Workload;

public sealed record WorkloadResult(
    int ItemCount,
    int Iterations,
    long Checksum,
    double Average,
    int P95,
    string Digest);