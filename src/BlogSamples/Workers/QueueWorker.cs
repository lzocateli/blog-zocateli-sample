// ==========================================================================
// Artigo: .NET Worker: Background Service para Alto Volume
// URL: /posts/2026/dotnet-worker-background-service-processamento-alto-volume/
// Queue Worker: consumindo mensagens RabbitMQ com BackgroundService
// ==========================================================================

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BlogSamples.Workers;

public record PedidoEvento(Guid Id, string ClienteId, decimal Valor);

public interface IProcessadorPedido
{
    Task ProcessarAsync(PedidoEvento pedido, CancellationToken ct);
}

/// <summary>
/// Worker que consome mensagens de uma fila RabbitMQ.
/// Cada mensagem é processada individualmente com ACK/NACK manual.
/// </summary>
public class FilaPedidosWorker(
    ILogger<FilaPedidosWorker> logger,
    IServiceScopeFactory scopeFactory,
    IConnection rabbitConnection) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = await rabbitConnection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: "pedidos-processamento",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processador = scope.ServiceProvider
                    .GetRequiredService<IProcessadorPedido>();

                var pedido = JsonSerializer.Deserialize<PedidoEvento>(
                    ea.Body.Span);

                await processador.ProcessarAsync(pedido!, stoppingToken);

                await channel.BasicAckAsync(ea.DeliveryTag, false,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar pedido");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true,
                    stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: "pedidos-processamento",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
