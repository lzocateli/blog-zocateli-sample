// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Pipeline Channel<T> limitado com estado local por worker.
// -----------------------------------------------------------------------

using System.Threading.Channels;

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

public sealed record HistoryAnalysisOptions(int MaxDegreeOfParallelism = 0, int ChannelCapacity = 1024)
{
    public int EffectiveParallelism => MaxDegreeOfParallelism > 0
        ? MaxDegreeOfParallelism
        : Math.Max(1, Environment.ProcessorCount);
}

public sealed record HistoryAnalysisResult(
    long RecordCount,
    long TotalAdditions,
    long TotalDeletions,
    IReadOnlyDictionary<string, long> ChangesByAuthor,
    IReadOnlyDictionary<string, long> ChangesByDirectory,
    IReadOnlyList<AuditFinding> Findings);

public sealed class HistoryAnalysisPipeline(
    IEnumerable<IAuditRule> rules,
    HistoryAnalysisOptions? options = null)
{
    private readonly IAuditRule[] _rules = rules.ToArray();
    private readonly HistoryAnalysisOptions _options = options ?? new();

    public async Task<HistoryAnalysisResult> AnalyzeAsync(
        IAsyncEnumerable<FileChangeRecord> records,
        CancellationToken cancellationToken = default)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pipelineToken = linkedCancellation.Token;
        var channel = Channel.CreateBounded<FileChangeRecord>(new BoundedChannelOptions(_options.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = _options.EffectiveParallelism == 1
        });

        var producer = ProduceAsync(records, channel.Writer, pipelineToken);
        var workers = Enumerable.Range(0, _options.EffectiveParallelism)
            .Select(_ => ConsumeAsync(channel.Reader, linkedCancellation, pipelineToken))
            .ToArray();

        await Task.WhenAll(workers.Cast<Task>().Append(producer));
        var workerResults = workers.Select(worker => worker.Result);
        return Combine(workerResults);
    }

    private static async Task ProduceAsync(
        IAsyncEnumerable<FileChangeRecord> records,
        ChannelWriter<FileChangeRecord> writer,
        CancellationToken cancellationToken)
    {
        Exception? error = null;
        try
        {
            await foreach (var record in records.WithCancellation(cancellationToken))
            {
                await writer.WriteAsync(record, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            error = exception;
            throw;
        }
        finally
        {
            writer.TryComplete(error);
        }
    }

    private async Task<WorkerResult> ConsumeAsync(
        ChannelReader<FileChangeRecord> reader,
        CancellationTokenSource linkedCancellation,
        CancellationToken cancellationToken)
    {
        var result = new WorkerResult();
        try
        {
            await foreach (var record in reader.ReadAllAsync(cancellationToken))
            {
                result.Add(record, _rules);
            }
        }
        catch
        {
            await linkedCancellation.CancelAsync();
            throw;
        }

        return result;
    }

    private static HistoryAnalysisResult Combine(IEnumerable<WorkerResult> workers)
    {
        var combined = new WorkerResult();
        foreach (var worker in workers)
        {
            combined.Merge(worker);
        }

        return combined.ToResult();
    }

    private sealed class WorkerResult
    {
        private readonly Dictionary<string, long> _authors = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _directories = new(StringComparer.Ordinal);
        private readonly List<AuditFinding> _findings = [];

        public long RecordCount { get; private set; }
        public long TotalAdditions { get; private set; }
        public long TotalDeletions { get; private set; }

        public void Add(FileChangeRecord record, IEnumerable<IAuditRule> rules)
        {
            RecordCount++;
            TotalAdditions += record.Additions ?? 0;
            TotalDeletions += record.Deletions ?? 0;
            Increment(_authors, $"{record.Commit.AuthorName} <{record.Commit.AuthorEmail}>");
            Increment(_directories, GetTopDirectory(record.Path));
            foreach (var rule in rules)
            {
                _findings.AddRange(rule.Evaluate(record));
            }
        }

        public void Merge(WorkerResult other)
        {
            RecordCount += other.RecordCount;
            TotalAdditions += other.TotalAdditions;
            TotalDeletions += other.TotalDeletions;
            MergeDictionary(_authors, other._authors);
            MergeDictionary(_directories, other._directories);
            _findings.AddRange(other._findings);
        }

        public HistoryAnalysisResult ToResult() => new(
            RecordCount,
            TotalAdditions,
            TotalDeletions,
            new SortedDictionary<string, long>(_authors, StringComparer.Ordinal),
            new SortedDictionary<string, long>(_directories, StringComparer.Ordinal),
            _findings.OrderBy(finding => finding.CommitId, StringComparer.Ordinal)
                .ThenBy(finding => finding.RuleCode, StringComparer.Ordinal)
                .ThenBy(finding => finding.Path, StringComparer.Ordinal)
                .ToArray());

        private static void Increment(Dictionary<string, long> values, string key) =>
            values[key] = values.GetValueOrDefault(key) + 1;

        private static void MergeDictionary(Dictionary<string, long> target, Dictionary<string, long> source)
        {
            foreach (var (key, value) in source)
            {
                target[key] = target.GetValueOrDefault(key) + value;
            }
        }

        private static string GetTopDirectory(string path)
        {
            var separator = path.IndexOf('/');
            return separator < 0 ? "." : path[..separator];
        }
    }
}