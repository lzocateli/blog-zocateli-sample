// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Parser stateful para git log --numstat -z com metadados NUL-delimited.
// -----------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

public sealed class GitLogStreamParser
{
    public const string CommitMarker = "GIT-HISTORY-COMMIT";
    private const int CommitFieldCount = 11;

    public async IAsyncEnumerable<FileChangeRecord> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CommitRecord? commit = null;
        var fields = new List<string>(CommitFieldCount);
        var readingCommit = false;
        var awaitingRenamePaths = false;
        int? renameAdditions = null;
        int? renameDeletions = null;
        string? previousPath = null;

        await foreach (var rawToken in NullDelimitedTokenReader.ReadAsync(stream, cancellationToken))
        {
            if (RemoveFormatSeparator(rawToken) == CommitMarker)
            {
                readingCommit = true;
                fields.Clear();
                awaitingRenamePaths = false;
                continue;
            }

            if (readingCommit)
            {
                fields.Add(rawToken);
                if (fields.Count == CommitFieldCount)
                {
                    commit = CreateCommit(fields);
                    readingCommit = false;
                }

                continue;
            }

            if (commit is null || rawToken.Length == 0)
            {
                continue;
            }

            if (awaitingRenamePaths)
            {
                if (previousPath is null)
                {
                    previousPath = rawToken;
                    continue;
                }

                yield return new FileChangeRecord(
                    commit, rawToken, previousPath, renameAdditions, renameDeletions);
                awaitingRenamePaths = false;
                previousPath = null;
                continue;
            }

            var numstatToken = RemoveFormatSeparator(rawToken);
            var parts = numstatToken.Split('\t', 3);
            if (parts.Length != 3)
            {
                throw new InvalidDataException($"Registro numstat inválido: '{numstatToken}'.");
            }

            var additions = ParseCount(parts[0]);
            var deletions = ParseCount(parts[1]);
            if (parts[2].Length == 0)
            {
                awaitingRenamePaths = true;
                renameAdditions = additions;
                renameDeletions = deletions;
                continue;
            }

            yield return new FileChangeRecord(commit, parts[2], null, additions, deletions);
        }

        if (readingCommit || awaitingRenamePaths)
        {
            throw new InvalidDataException("A saída do Git terminou no meio de um registro.");
        }
    }

    private static CommitRecord CreateCommit(IReadOnlyList<string> fields) => new(
        fields[0],
        fields[1].Split(' ', StringSplitOptions.RemoveEmptyEntries),
        fields[2],
        fields[3],
        DateTimeOffset.Parse(fields[4], CultureInfo.InvariantCulture),
        fields[5],
        fields[6],
        DateTimeOffset.Parse(fields[7], CultureInfo.InvariantCulture),
        fields[8],
        fields[9],
        fields[10]);

    private static int? ParseCount(string value) => value == "-"
        ? null
        : int.Parse(value, CultureInfo.InvariantCulture);

    private static string RemoveFormatSeparator(string token) =>
        token.TrimStart('\r', '\n');
}
