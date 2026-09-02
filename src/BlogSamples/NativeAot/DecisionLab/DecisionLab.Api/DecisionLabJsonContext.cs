// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Metadados JSON gerados em build para evitar descoberta dinâmica de tipos.
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;
using BlogSamples.NativeAot.DecisionLab.Workload;

namespace BlogSamples.NativeAot.DecisionLab.Api;

[JsonSerializable(typeof(ReadyResponse))]
[JsonSerializable(typeof(WorkloadRequest))]
[JsonSerializable(typeof(WorkloadResult))]
internal sealed partial class DecisionLabJsonContext : JsonSerializerContext;