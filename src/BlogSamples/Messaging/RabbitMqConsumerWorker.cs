// ==========================================================================
// Artigo: Gargalo em Banco de Dados: Mensageria e Paginação
// URL: /posts/2026/gargalo-banco-dados-efcore-mensageria-paginacao/
// ==========================================================================

using System.Text.Json;
using EFCore.BulkExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BlogSamples.Messaging;

/// <summary>
/// Consumer RabbitMQ em lote (BackgroundService).
/// Acumula mensagens em lote e grava via BulkInsert.
/// Usa PeriodicTimer para flush periódico.
/// </summary>
public class RabbitMqConsumerWorker(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsumerWorker> logger) : BackgroundService
{
    private const string QueueName = "pedidos-queue";
    private const int TamanhoLote = 500;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: QueueName, durable: true,
            exclusive: false, autoDelete: false,
            cancellationToken: ct);

        // Backpressure: limita mensagens em voo para não sobrecarregar a memória
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: (ushort)(TamanhoLote + 100),
            global: false,
            cancellationToken: ct);

        var lote = new List<(ulong Tag, PedidoCriadoMessage Msg)>();

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var msg = JsonSerializer.Deserialize<PedidoCriadoMessage>(ea.Body.Span)!;
            lote.Add((ea.DeliveryTag, msg));

            if (lote.Count >= TamanhoLote)
                await ProcessarLoteAsync(channel, lote, ct);
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        // Timer de flush: força o processamento mesmo que o lote não esteja cheio
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(ct))
        {
            if (lote.Count > 0)
                await ProcessarLoteAsync(channel, lote, ct);
        }
    }

    private async Task ProcessarLoteAsync(
        IChannel channel,
        List<(ulong Tag, PedidoCriadoMessage Msg)> lote,
        CancellationToken ct)
    {
        var snapshot = lote.ToList();
        lote.Clear();

        var pedidos = snapshot.Select(x => new Pedido
        {
            Id          = x.Msg.Id,
            ClienteId   = x.Msg.ClienteId,
            Valor       = x.Msg.Valor,
            DataCriacao = x.Msg.DataCriacao
        }).ToList();

        using var scope    = scopeFactory.CreateScope();
        var dbContext       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bulkConfig      = new BulkConfig { BatchSize = 500 };

        await dbContext.BulkInsertAsync(pedidos, bulkConfig, cancellationToken: ct);

        // ACK múltiplo: confirma todo o lote de uma só vez
        await channel.BasicAckAsync(
            deliveryTag: snapshot[^1].Tag,
            multiple: true,
            cancellationToken: ct);

        logger.LogInformation("Lote de {Count} pedidos gravado", pedidos.Count);
    }
}

// --- Program.cs snippet ---
// builder.Services.AddSingleton<IConnection>(_ =>
// {
//     var factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" };
//     return factory.CreateConnectionAsync().GetAwaiter().GetResult();
// });
// builder.Services.AddScoped<PedidoProducer>();
// builder.Services.AddHostedService<RabbitMqConsumerWorker>();
