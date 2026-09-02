// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Execução de subprocessos com captura completa e validação de exit code.
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace BlogSamples.NativeAot.DecisionLab.Harness;

internal static class ProcessRunner
{
    public static async Task<(TimeSpan Duration, string Output)> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };

        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        var stopwatch = Stopwatch.StartNew();
        process.Start();
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        stopwatch.Stop();

        string output = string.Concat(await standardOutput, Environment.NewLine, await standardError).Trim();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} encerrou com código {process.ExitCode}:{Environment.NewLine}{output}");
        }

        return (stopwatch.Elapsed, output);
    }
}