// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Regras independentes para assinaturas, trailers, paths e churn.
// -----------------------------------------------------------------------

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

public interface IAuditRule
{
    IEnumerable<AuditFinding> Evaluate(FileChangeRecord record);
}

public sealed class SignatureAuditRule : IAuditRule
{
    public IEnumerable<AuditFinding> Evaluate(FileChangeRecord record)
    {
        if (record.Commit.SignatureStatus is not ("G" or "U"))
        {
            yield return new AuditFinding(
                "GIT001", AuditSeverity.Warning, record.Commit.ObjectId, null,
                $"Assinatura ausente ou não confiável: status {record.Commit.SignatureStatus}.");
        }
    }
}

public sealed class RequiredTrailerAuditRule(params string[] requiredTrailers) : IAuditRule
{
    public IEnumerable<AuditFinding> Evaluate(FileChangeRecord record)
    {
        foreach (var trailer in requiredTrailers)
        {
            if (!record.Commit.Trailers.Contains($"{trailer}:", StringComparison.OrdinalIgnoreCase))
            {
                yield return new AuditFinding(
                    "GIT002", AuditSeverity.Warning, record.Commit.ObjectId, null,
                    $"Trailer obrigatório ausente: {trailer}.");
            }
        }
    }
}

public sealed class SensitivePathAuditRule(params string[] pathPrefixes) : IAuditRule
{
    public IEnumerable<AuditFinding> Evaluate(FileChangeRecord record)
    {
        if (pathPrefixes.Any(prefix => record.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            yield return new AuditFinding(
                "GIT003", AuditSeverity.Critical, record.Commit.ObjectId, record.Path,
                "Alteração em path sensível exige correlação com revisão e pipeline.");
        }
    }
}

public sealed class HighChurnAuditRule(int threshold) : IAuditRule
{
    public IEnumerable<AuditFinding> Evaluate(FileChangeRecord record)
    {
        if (record.Churn >= threshold)
        {
            yield return new AuditFinding(
                "GIT004", AuditSeverity.Warning, record.Commit.ObjectId, record.Path,
                $"Churn de {record.Churn} linhas excede o limite de {threshold}.");
        }
    }
}
