// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Comparação reproduzível entre sequencial, Parallel.ForEachAsync e Channel.
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

public sealed record PerformanceMeasurement(
    string Strategy,
    int RecordCount,
    TimeSpan Elapsed,
    double RecordsPerSecond,
    long AllocatedBytes,
    long ApproximatePeakWorkingSetBytes,
    long ChurnChecksum);

public static class HistoryPerformanceComparison
{
    public static async Task<IReadOnlyList<PerformanceMeasurement>> RunAsync(
        int recordCount = HistoryFixtureGenerator.DefaultRecordCount,
        int maxDegreeOfParallelism = 0,
        int channelCapacity = 1024,
        CancellationToken cancellationToken = default)
    {
        var parallelism = maxDegreeOfParallelism > 0
            ? maxDegreeOfParallelism
            : Math.Max(1, Environment.ProcessorCount);

        return
        [
            await MeasureSequentialAsync(recordCount, cancellationToken),
            await MeasureParallelAsync(recordCount, parallelism, cancellationToken),
            await MeasureChannelAsync(recordCount, parallelism, channelCapacity, cancellationToken)
        ];
    }

    private static Task<PerformanceMeasurement> MeasureSequentialAsync(
        int count,
        CancellationToken cancellationToken) => MeasureAsync("sequential", count, async () =>
        {
            long checksum = 0;
            await foreach (var record in HistoryFixtureGenerator.GenerateAsync(count, cancellationToken))
            {
                checksum += CpuBoundAnalysis(record);
            }

            return checksum;
        });

    private static Task<PerformanceMeasurement> MeasureParallelAsync(
        int count,
        int parallelism,
        CancellationToken cancellationToken) => MeasureAsync("parallel-foreach-async", count, async () =>
        {
            long checksum = 0;
            await Parallel.ForEachAsync(
                HistoryFixtureGenerator.GenerateAsync(count, cancellationToken),
                new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = cancellationToken },
                (record, _) =>
                {
                    Interlocked.Add(ref checksum, CpuBoundAnalysis(record));
                    return ValueTask.CompletedTask;
                });
            return checksum;
        });

    private static Task<PerformanceMeasurement> MeasureChannelAsync(
        int count,
        int parallelism,
        int channelCapacity,
        CancellationToken cancellationToken) => MeasureAsync("bounded-channel", count, async () =>
        {
            var rule = new ChecksumAuditRule();
            await new HistoryAnalysisPipeline([rule], new(parallelism, channelCapacity))
                .AnalyzeAsync(HistoryFixtureGenerator.GenerateAsync(count, cancellationToken), cancellationToken);
            return rule.Checksum;
        });

    private static async Task<PerformanceMeasurement> MeasureAsync(
        string strategy,
        int count,
        Func<Task<long>> action)
    {
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        var checksum = await action();
        stopwatch.Stop();
        var process = Process.GetCurrentProcess();
        return new PerformanceMeasurement(
            strategy,
            count,
            stopwatch.Elapsed,
            count / stopwatch.Elapsed.TotalSeconds,
            GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore,
            process.PeakWorkingSet64,
            checksum);
    }

    private static long CpuBoundAnalysis(FileChangeRecord record)
    {
        var value = record.Churn;
        for (var iteration = 0; iteration < 16; iteration++)
        {
            value = HashCode.Combine(value, record.Path.Length, iteration);
        }

        return value;
    }

    private sealed class ChecksumAuditRule : IAuditRule
    {
        private long _checksum;
        public long Checksum => Interlocked.Read(ref _checksum);

        public IEnumerable<AuditFinding> Evaluate(FileChangeRecord record)
        {
            Interlocked.Add(ref _checksum, CpuBoundAnalysis(record));
            return [];
        }
    }
}