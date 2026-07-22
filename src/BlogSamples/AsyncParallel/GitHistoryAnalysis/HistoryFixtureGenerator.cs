// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Fixture determinística para medições com 500 mil registros.
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

public static class HistoryFixtureGenerator
{
    public const int DefaultRecordCount = 500_000;

    public static async IAsyncEnumerable<FileChangeRecord> GenerateAsync(
        int count = DefaultRecordCount,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var commit = new CommitRecord(
                $"fixture-{index / 5:x}", [], $"Autor {index % 32}", $"autor{index % 32}@example.com",
                DateTimeOffset.UnixEpoch.AddMinutes(index / 5), "CI", "ci@example.com",
                DateTimeOffset.UnixEpoch.AddMinutes(index / 5), "G", $"Mudança {index / 5}",
                "Reviewed-by: Equipe");
            yield return new FileChangeRecord(
                commit, $"src/componente-{index % 64}/arquivo-{index % 2048}.cs", null,
                index % 101, index % 29);

            if ((index & 4095) == 0)
            {
                await Task.Yield();
            }
        }
    }
}
