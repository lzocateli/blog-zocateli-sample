// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Executa um único processo Git e expõe os registros em streaming.
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

public sealed class GitLogProcess
{
    private const string Format =
        "GIT-HISTORY-COMMIT%x00%H%x00%P%x00%aN%x00%aE%x00%aI%x00%cN%x00%cE%x00%cI%x00%G?%x00%s%x00%(trailers:only,unfold=true)%x00";

    public async IAsyncEnumerable<FileChangeRecord> ReadAsync(
        string repositoryPath,
        string revisionRange,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateArguments(repositoryPath, revisionRange);
        using var process = new Process { StartInfo = CreateStartInfo(repositoryPath, revisionRange) };
        if (!process.Start())
        {
            throw new InvalidOperationException("Não foi possível iniciar o processo Git.");
        }

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
        });

        await foreach (var record in new GitLogStreamParser().ParseAsync(
            process.StandardOutput.BaseStream, cancellationToken))
        {
            yield return record;
        }

        await process.WaitForExitAsync(cancellationToken);
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new GitProcessException(process.ExitCode, stderr);
        }
    }

    private static ProcessStartInfo CreateStartInfo(string repositoryPath, string revisionRange)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in new[]
        {
            "-C", repositoryPath, "log", "--topo-order", "--find-renames",
            "--numstat", "-z", $"--format={Format}", revisionRange, "--"
        })
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static void ValidateArguments(string repositoryPath, string revisionRange)
    {
        if (!Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException(repositoryPath);
        }

        if (string.IsNullOrWhiteSpace(revisionRange) || revisionRange[0] == '-' || revisionRange.Contains('\0'))
        {
            throw new ArgumentException("O intervalo de revisões é inválido.", nameof(revisionRange));
        }
    }
}

public sealed class GitProcessException(int exitCode, string standardError)
    : Exception($"Git encerrou com código {exitCode}: {standardError.Trim()}")
{
    public int ExitCode { get; } = exitCode;

    public string StandardError { get; } = standardError;
}
