// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Persistência determinística das medições brutas em CSV.
// -----------------------------------------------------------------------

using System.Globalization;

namespace BlogSamples.NativeAot.DecisionLab.Harness;

internal static class CsvWriter
{
    public static async Task WritePublishAsync(string path, IEnumerable<PublishMeasurement> rows)
    {
        var lines = new List<string>
        {
            "profile,category,repetition,duration_ms,uncompressed_bytes,compressed_bytes,succeeded,error",
        };

        lines.AddRange(rows.Select(row => string.Join(',',
            Escape(row.Profile),
            Escape(row.Category),
            row.Repetition.ToString(CultureInfo.InvariantCulture),
            row.DurationMilliseconds.ToString("F3", CultureInfo.InvariantCulture),
            row.UncompressedBytes.ToString(CultureInfo.InvariantCulture),
            row.CompressedBytes.ToString(CultureInfo.InvariantCulture),
            row.Succeeded.ToString(CultureInfo.InvariantCulture),
            Escape(row.Error ?? string.Empty))));

        await File.WriteAllLinesAsync(path, lines);
    }

    public static async Task WriteStartupAsync(string path, IEnumerable<StartupMeasurement> rows)
    {
        var lines = new List<string>
        {
            "profile,repetition,execution_order,duration_ms,succeeded,error",
        };

        lines.AddRange(rows.Select(row => string.Join(',',
            Escape(row.Profile),
            row.Repetition.ToString(CultureInfo.InvariantCulture),
            row.ExecutionOrder.ToString(CultureInfo.InvariantCulture),
            row.DurationMilliseconds.ToString("F3", CultureInfo.InvariantCulture),
            row.Succeeded.ToString(CultureInfo.InvariantCulture),
            Escape(row.Error ?? string.Empty))));

        await File.WriteAllLinesAsync(path, lines);
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}