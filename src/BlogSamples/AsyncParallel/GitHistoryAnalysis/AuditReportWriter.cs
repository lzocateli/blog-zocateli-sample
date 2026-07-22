// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Serialização JSON estável com contexto reproduzível da execução.
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

public sealed record AuditReportContext(
    string RepositoryPath,
    string RevisionRange,
    IReadOnlyList<string> References,
    string GitVersion,
    DateTimeOffset AnalyzedAt,
    string ConfigurationHash);

public sealed record AuditReport(AuditReportContext Context, HistoryAnalysisResult Analysis);

public sealed class AuditReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task WriteAsync(
        Stream destination,
        AuditReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        await JsonSerializer.SerializeAsync(destination, report, Options, cancellationToken);
        await destination.WriteAsync("\n"u8.ToArray(), cancellationToken);
    }
}
