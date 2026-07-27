using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace BlogSamples.Messaging.EventDriven;

public interface IEventDrivenBrokerPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken ct);
}

public sealed class ServiceBusEventDrivenPublisher(
    ServiceBusClient client,
    IOptions<EventDrivenOptions> options) : IEventDrivenBrokerPublisher, IAsyncDisposable
{
    private readonly EventDrivenOptions _options = options.Value;
    private readonly ServiceBusSender _sender = client.CreateSender(options.Value.TopicName);

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        var sbMessage = new ServiceBusMessage(BinaryData.FromString(message.Payload))
        {
            ContentType = "application/json",
            MessageId = message.Id.ToString(),
            Subject = message.EventType
        };

        await _sender.SendMessageAsync(sbMessage, ct);
    }

    public ValueTask DisposeAsync()
    {
        return _sender.DisposeAsync();
    }
}

public sealed class OutboxPublisherWorker(
    EventDrivenOutboxStore store,
    IEventDrivenBrokerPublisher publisher,
    IOptions<EventDrivenOptions> options,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private readonly EventDrivenOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("EventDriven desativado; worker de outbox não iniciado");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingMessages = await store.LockPendingMessagesAsync(stoppingToken);

                foreach (var message in pendingMessages)
                {
                    try
                    {
                        await publisher.PublishAsync(message, stoppingToken);
                        await store.MarkAsPublishedAsync(message.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Falha ao publicar outbox {OutboxId}, tentativa atual {Attempt}",
                            message.Id,
                            message.AttemptCount + 1);
                        await store.MarkAsFailedAsync(message.Id, message.AttemptCount, ex, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha no ciclo do publisher do outbox");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_options.PublisherIntervalMs), stoppingToken);
        }
    }
}