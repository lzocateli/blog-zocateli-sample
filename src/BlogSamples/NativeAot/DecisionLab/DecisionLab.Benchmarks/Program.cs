// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Host do BenchmarkDotNet; cada job executa em processo separado.
// -----------------------------------------------------------------------

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

var config = ManualConfig
	.Create(DefaultConfig.Instance)
	.WithBuildTimeout(TimeSpan.FromMinutes(15));

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);