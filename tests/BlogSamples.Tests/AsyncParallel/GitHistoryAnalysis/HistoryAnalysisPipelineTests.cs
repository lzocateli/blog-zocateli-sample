using System.Runtime.CompilerServices;
using BlogSamples.AsyncParallel.GitHistoryAnalysis;

namespace BlogSamples.Tests.AsyncParallel.GitHistoryAnalysis;

public sealed class HistoryAnalysisPipelineTests
{
    [Fact]
    public async Task AnalyzeAsync_CanalLimitadoNaoPerdeRegistros()
    {
        const int count = 10_000;
        var pipeline = new HistoryAnalysisPipeline(
            [new HighChurnAuditRule(100)],
            new HistoryAnalysisOptions(MaxDegreeOfParallelism: 4, ChannelCapacity: 1));

        var result = await pipeline.AnalyzeAsync(HistoryFixtureGenerator.GenerateAsync(count));

        Assert.Equal(count, result.RecordCount);
        Assert.Equal(Enumerable.Range(0, count).Sum(index => (long)(index % 101)), result.TotalAdditions);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public async Task AnalyzeAsync_SequencialEParaleloProduzemResultadoEquivalente()
    {
        var rules = new IAuditRule[] { new RequiredTrailerAuditRule("Reviewed-by"), new HighChurnAuditRule(90) };
        var sequential = await new HistoryAnalysisPipeline(rules, new(1, 8))
            .AnalyzeAsync(HistoryFixtureGenerator.GenerateAsync(2_000));
        var parallel = await new HistoryAnalysisPipeline(rules, new(4, 32))
            .AnalyzeAsync(HistoryFixtureGenerator.GenerateAsync(2_000));

        Assert.Equal(sequential.RecordCount, parallel.RecordCount);
        Assert.Equal(sequential.TotalAdditions, parallel.TotalAdditions);
        Assert.Equal(sequential.TotalDeletions, parallel.TotalDeletions);
        Assert.Equal(sequential.ChangesByAuthor, parallel.ChangesByAuthor);
        Assert.Equal(sequential.ChangesByDirectory, parallel.ChangesByDirectory);
        Assert.Equal(sequential.Findings, parallel.Findings);
    }

    [Fact]
    public async Task AnalyzeAsync_RegraComFalhaEncerraProdutorEPropagaExcecao()
    {
        var pipeline = new HistoryAnalysisPipeline([new ThrowingRule()], new(2, 1));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.AnalyzeAsync(HistoryFixtureGenerator.GenerateAsync(10_000)));

        Assert.Equal("Falha controlada", exception.Message);
    }

    [Fact]
    public async Task WriteAsync_MesmoRelatorioProduzJsonIdentico()
    {
        var analysis = await new HistoryAnalysisPipeline([], new(2, 4))
            .AnalyzeAsync(HistoryFixtureGenerator.GenerateAsync(100));
        var context = new AuditReportContext(
            "repo", "main~10..main", ["main"], "git version 2.50.0",
            DateTimeOffset.Parse("2026-07-21T12:00:00Z"), "config-hash");
        var report = new AuditReport(context, analysis);

        await using var first = new MemoryStream();
        await using var second = new MemoryStream();
        var writer = new AuditReportWriter();
        await writer.WriteAsync(first, report);
        await writer.WriteAsync(second, report);

        Assert.Equal(first.ToArray(), second.ToArray());
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task PerformanceComparison_ProcessaQuinhentosMilRegistros()
    {
        var measurements = await HistoryPerformanceComparison.RunAsync();

        Assert.Equal(3, measurements.Count);
        Assert.All(measurements, measurement =>
            Assert.Equal(HistoryFixtureGenerator.DefaultRecordCount, measurement.RecordCount));
        Assert.Single(measurements.Select(measurement => measurement.ChurnChecksum).Distinct());
        foreach (var measurement in measurements)
        {
            Console.WriteLine(
                $"{measurement.Strategy}: {measurement.Elapsed.TotalMilliseconds:F0} ms; " +
                $"{measurement.RecordsPerSecond:F0} registros/s; " +
                $"{measurement.AllocatedBytes / 1024d / 1024d:F1} MiB alocados; " +
                $"{measurement.ApproximatePeakWorkingSetBytes / 1024d / 1024d:F1} MiB pico aproximado");
        }
    }

    private sealed class ThrowingRule : IAuditRule
    {
        public IEnumerable<AuditFinding> Evaluate(FileChangeRecord record) =>
            throw new InvalidOperationException("Falha controlada");
    }
}