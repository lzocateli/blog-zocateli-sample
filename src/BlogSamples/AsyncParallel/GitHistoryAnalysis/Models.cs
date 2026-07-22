// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Modelos imutáveis usados pela extração e análise do histórico Git.
// -----------------------------------------------------------------------

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

public sealed record CommitRecord(
    string ObjectId,
    IReadOnlyList<string> ParentIds,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset AuthorDate,
    string CommitterName,
    string CommitterEmail,
    DateTimeOffset CommitterDate,
    string SignatureStatus,
    string Subject,
    string Trailers);

public sealed record FileChangeRecord(
    CommitRecord Commit,
    string Path,
    string? PreviousPath,
    int? Additions,
    int? Deletions)
{
    public bool IsBinary => Additions is null || Deletions is null;

    public int Churn => (Additions ?? 0) + (Deletions ?? 0);
}

public enum AuditSeverity
{
    Information,
    Warning,
    Critical
}

public sealed record AuditFinding(
    string RuleCode,
    AuditSeverity Severity,
    string CommitId,
    string? Path,
    string Evidence);
