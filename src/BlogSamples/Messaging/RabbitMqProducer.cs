// ==========================================================================
// Artigo: Gargalo em Banco de Dados: Mensageria e Paginação
// URL: /posts/2026/gargalo-banco-dados-efcore-mensageria-paginacao/
// ==========================================================================

using System.Text.Json;
using RabbitMQ.Client;

namespace BlogSamples.Messaging;

/// <summary>
/// Mensagem publicada quando um pedido é criado.
/// </summary>
public record PedidoCriadoMessage(
    Guid Id,
    string ClienteId,
    decimal Valor,
    DateTime DataCriacao);

/// <summary>
/// Producer RabbitMQ — publica mensagens na fila de pedidos.
/// </summary>
public class PedidoProducer(IConnection connection)
{
    private const string QueueName = "pedidos-queue";

    public async Task PublicarAsync(
        PedidoCriadoMessage mensagem,
        CancellationToken ct = default)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        var json = JsonSerializer.SerializeToUtf8Bytes(mensagem);
        var props = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            mandatory: false,
            basicProperties: props,
            body: json,
            cancellationToken: ct);
    }
}
