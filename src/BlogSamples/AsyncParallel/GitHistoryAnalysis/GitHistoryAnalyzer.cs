// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Fachada do analisador: processo Git, pipeline e relatório JSON.
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

public sealed class GitHistoryAnalyzer(
    IEnumerable<IAuditRule> rules,
    HistoryAnalysisOptions? options = null)
{
    private readonly IAuditRule[] _rules = rules.ToArray();
    private readonly HistoryAnalysisOptions _options = options ?? new();

    public async Task<AuditReport> AnalyzeAsync(
        string repositoryPath,
        string revisionRange,
        IReadOnlyList<string>? references = null,
        CancellationToken cancellationToken = default)
    {
        var records = new GitLogProcess().ReadAsync(repositoryPath, revisionRange, cancellationToken);
        var result = await new HistoryAnalysisPipeline(_rules, _options)
            .AnalyzeAsync(records, cancellationToken);
        var context = new AuditReportContext(
            Path.GetFullPath(repositoryPath),
            revisionRange,
            (references ?? []).Order(StringComparer.Ordinal).ToArray(),
            await ReadGitVersionAsync(cancellationToken),
            DateTimeOffset.UtcNow,
            CreateConfigurationHash());

        return new AuditReport(context, result);
    }

    private string CreateConfigurationHash()
    {
        var configuration = string.Join('|', _rules.Select(rule => rule.GetType().FullName)
            .Append($"workers={_options.EffectiveParallelism}")
            .Append($"capacity={_options.ChannelCapacity}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(configuration)));
    }

    private static async Task<string> ReadGitVersionAsync(CancellationToken cancellationToken)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", "--version")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Não foi possível consultar a versão do Git.");
        var version = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return version.Trim();
    }
}
