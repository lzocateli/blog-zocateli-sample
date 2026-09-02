// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Microbenchmark da mesma carga executada pela Minimal API.
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BlogSamples.NativeAot.DecisionLab.Workload;

namespace BlogSamples.NativeAot.DecisionLab.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
[CsvExporter]
[JsonExporterAttribute.Full]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class WorkloadBenchmarks
{
    private readonly WorkloadRequest request = WorkloadRequest.Default;

    [Benchmark]
    public WorkloadResult ProcessarCarga() => WorkloadProcessor.Process(request);
}