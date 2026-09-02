// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Carga determinística de CPU e alocação usada em todas as medições.
// -----------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BlogSamples.NativeAot.DecisionLab.Workload;

public static class WorkloadProcessor
{
    public static WorkloadResult Process(WorkloadRequest request)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(request.ItemCount, 128);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.ItemCount, 65_536);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Iterations, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.Iterations, 100);

        int[] values = ArrayPool<int>.Shared.Rent(request.ItemCount);

        try
        {
            var random = new Random(request.Seed);
            long checksum = 0;

            for (int iteration = 0; iteration < request.Iterations; iteration++)
            {
                for (int index = 0; index < request.ItemCount; index++)
                {
                    values[index] = random.Next(1, 1_000_000) ^ (iteration * 397);
                }

                Array.Sort(values, 0, request.ItemCount);

                for (int index = 0; index < request.ItemCount; index++)
                {
                    checksum = unchecked((checksum * 31) + values[index]);
                }
            }

            double average = values.AsSpan(0, request.ItemCount).ToArray().Average();
            int p95 = values[(int)Math.Floor((request.ItemCount - 1) * 0.95)];
            string digestInput = string.Create(
                CultureInfo.InvariantCulture,
                $"{request.Seed}:{request.ItemCount}:{request.Iterations}:{checksum}:{average:F4}:{p95}");
            string digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(digestInput)));

            return new WorkloadResult(request.ItemCount, request.Iterations, checksum, average, p95, digest);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(values);
        }
    }
}