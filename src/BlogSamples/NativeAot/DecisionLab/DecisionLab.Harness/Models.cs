// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Contratos dos dados brutos e do manifesto do experimento.
// -----------------------------------------------------------------------

namespace BlogSamples.NativeAot.DecisionLab.Harness;

internal sealed record HarnessOptions(
    string RepositoryRoot,
    string OutputDirectory,
    int PublishRepetitions,
    int StartupRepetitions,
    int RandomSeed,
    bool SkipPublish);

internal sealed record PublishMeasurement(
    string Profile,
    string Category,
    int Repetition,
    double DurationMilliseconds,
    long UncompressedBytes,
    long CompressedBytes,
    bool Succeeded,
    string? Error);

internal sealed record StartupMeasurement(
    string Profile,
    int Repetition,
    int ExecutionOrder,
    double DurationMilliseconds,
    bool Succeeded,
    string? Error);

internal sealed record ExperimentManifest(
    DateTimeOffset CreatedAtUtc,
    string RepositoryCommit,
    bool RepositoryDirty,
    string SourceFingerprintSha256,
    string OperatingSystem,
    string Architecture,
    string Processor,
    long AvailableMemoryBytes,
    string DotnetInfo,
    string RuntimeIdentifier,
    string Configuration,
    string GarbageCollector,
    int PublishRepetitions,
    int StartupRepetitions,
    int RandomSeed,
    string[] Profiles,
    string[] ControlledEnvironment,
    string[] WorkloadCharacteristics,
    string Command);