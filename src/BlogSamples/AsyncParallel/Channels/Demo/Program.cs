// Artigo: Channels no .NET: filas, backpressure e cancelamento
// URL: https://zocate.li/posts/2026/system-threading-channels/
// Demonstracao console sem API, broker ou banco de dados.

using System.Runtime.CompilerServices;
using BlogSamples.AsyncParallel.Channels;

var processados = 0;
await PipelineChannels.ExecutarAsync(
    ProduzirAsync(),
    async (item, cancellationToken) =>
    {
        await Task.Delay(5, cancellationToken);
        Interlocked.Increment(ref processados);
        Console.WriteLine($"Processado: {item}");
    },
    capacidade: 4,
    consumidores: 2);

Console.WriteLine($"Total processado: {processados}");

static async IAsyncEnumerable<int> ProduzirAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    for (var item = 1; item <= 20; item++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return item;
        await Task.Yield();
    }
}
