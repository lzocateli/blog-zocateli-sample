// Artigo: Channels no .NET: filas, backpressure e cancelamento
// URL: https://zocate.li/posts/2026/system-threading-channels/
// Pipeline limitado em memoria, com conclusao normal e falhas coordenadas.

using System.Runtime.ExceptionServices;
using System.Threading.Channels;

namespace BlogSamples.AsyncParallel.Channels;

public static class PipelineChannels
{
    public static async Task ExecutarAsync<T>(
        IAsyncEnumerable<T> fonte,
        Func<T, CancellationToken, ValueTask> processar,
        int capacidade = 4,
        int consumidores = 2,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fonte);
        ArgumentNullException.ThrowIfNull(processar);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacidade);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(consumidores);

        using var parada = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var canal = Channel.CreateBounded<T>(new BoundedChannelOptions(capacidade)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = consumidores == 1,
            AllowSynchronousContinuations = false
        });
        Exception? falha = null;

        var tarefasConsumidoras = Enumerable.Range(0, consumidores)
            .Select(_ => ConsumirAsync())
            .ToArray();
        var produtor = ProduzirAsync();

        try
        {
            await Task.WhenAll(tarefasConsumidoras.Append(produtor));
        }
        catch
        {
            if (falha is not null)
            {
                ExceptionDispatchInfo.Capture(falha).Throw();
            }

            throw;
        }

        void RegistrarFalha(Exception exception)
        {
            Interlocked.CompareExchange(ref falha, exception, null);
            canal.Writer.TryComplete(exception);
            parada.Cancel();
        }

        async Task ProduzirAsync()
        {
            try
            {
                await foreach (var item in fonte.WithCancellation(parada.Token))
                {
                    await canal.Writer.WriteAsync(item, parada.Token);
                }
            }
            catch (Exception exception)
            {
                RegistrarFalha(exception);
                throw;
            }
            finally
            {
                canal.Writer.TryComplete(falha);
            }
        }

        async Task ConsumirAsync()
        {
            try
            {
                await foreach (var item in canal.Reader.ReadAllAsync(parada.Token))
                {
                    parada.Token.ThrowIfCancellationRequested();
                    await processar(item, parada.Token);
                }
            }
            catch (Exception exception)
            {
                RegistrarFalha(exception);
                throw;
            }
        }
    }
}
