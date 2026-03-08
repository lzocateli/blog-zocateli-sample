// ==========================================================================
// Artigo: Gargalo em Banco de Dados: Mensageria e Paginação
// URL: /posts/2026/gargalo-banco-dados-efcore-mensageria-paginacao/
// ==========================================================================

using Azure.Messaging.ServiceBus;
using EFCore.BulkExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlogSamples.Messaging;

/// <summary>
/// Producer Azure Service Bus — envia mensagens em lote nativo.
/// O ServiceBusMessageBatch respeita o limite de tamanho automaticamente.
/// </summary>
public class PedidoServiceBusProducer(ServiceBusSender sender)
{
    public async Task PublicarLoteAsync(
        IEnumerable<PedidoCriadoMessage> mensagens,
        CancellationToken ct = default)
    {
        using var batch = await sender.CreateMessageBatchAsync(ct);

        foreach (var msg in mensagens)
        {
            var sbMsg = new ServiceBusMessage(
                BinaryData.FromObjectAsJson(msg))
            {
                ContentType = "application/json",
                MessageId   = msg.Id.ToString()
            };

            if (!batch.TryAddMessage(sbMsg))
                throw new InvalidOperationException(
                    $"Mensagem {msg.Id} excede o limite do lote");
        }

        await sender.SendMessagesAsync(batch, ct);
    }
}

/// <summary>
/// Consumer Azure Service Bus com BackgroundService.
/// Usa ServiceBusProcessor para processar mensagens.
/// </summary>
public class PedidoServiceBusWorker(
    ServiceBusProcessor processor,
    IServiceScopeFactory scopeFactory,
    ILogger<PedidoServiceBusWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        processor.ProcessMessageAsync += async args =>
        {
            var msg = args.Message.Body
                          .ToObjectFromJson<PedidoCriadoMessage>()
                      ?? throw new InvalidOperationException("Mensagem inválida");

            using var scope   = scopeFactory.CreateScope();
            var dbContext      = scope.ServiceProvider
                                      .GetRequiredService<AppDbContext>();
            var pedido         = new Pedido
            {
                Id          = msg.Id,
                ClienteId   = msg.ClienteId,
                Valor       = msg.Valor,
                DataCriacao = msg.DataCriacao
            };

            await dbContext.BulkInsertAsync(
                new List<Pedido> { pedido },
                cancellationToken: ct);

            // Confirma o processamento — remove da fila
            await args.CompleteMessageAsync(args.Message, ct);
        };

        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception,
                "Erro ao processar mensagem do Service Bus");
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(ct);
        await Task.Delay(Timeout.Infinite, ct);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        await processor.StopProcessingAsync(ct);
        await base.StopAsync(ct);
    }
}

// --- Program.cs snippet ---
// builder.Services.AddSingleton(provider =>
// {
//     var client = new ServiceBusClient("Endpoint=sb://meu-namespace.servicebus.windows.net/;...");
//     return client.CreateSender("pedidos-queue");
// });
// builder.Services.AddSingleton(provider =>
// {
//     var client = new ServiceBusClient("Endpoint=sb://meu-namespace.servicebus.windows.net/;...");
//     return client.CreateProcessor("pedidos-queue", new ServiceBusProcessorOptions
//     {
//         MaxConcurrentCalls   = 4,
//         AutoCompleteMessages = false
//     });
// });
// builder.Services.AddHostedService<PedidoServiceBusWorker>();
