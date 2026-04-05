// ==========================================================================
// Artigo: EFCore.BulkExtensions: Operações em Massa Profissionais no .NET
// URL: /posts/2026/efcore-bulkextensions-operacoes-massa-dotnet/
// Worker: consome Azure Service Bus em lote + persiste com BulkInsertOrUpdate
// ==========================================================================

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EFCore.BulkExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlogSamples.DataAccess.BulkOperations;

/// <summary>
/// Worker que consome mensagens do Azure Service Bus em lotes
/// e persiste no banco usando BulkInsertOrUpdateAsync.
/// Combina o padrão de mensageria com operações bulk para
/// alto desempenho em cenários de ingestão de dados.
/// </summary>
public class RegistroAppBulkProcessor(
    ServiceBusClient serviceBusClient,
    IServiceScopeFactory scopeFactory,
    ILogger<RegistroAppBulkProcessor> logger) : BackgroundService
{
    private const string QueueName = "registro-app-sync";
    private const int TamanhoLote = 1000;
    private const int TimeoutSegundos = 30;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var receiver = serviceBusClient.CreateReceiver(QueueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = TamanhoLote
        });

        logger.LogInformation(
            "RegistroAppBulkProcessor iniciado — fila: {Queue}, lote: {Batch}",
            QueueName, TamanhoLote);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessarLoteAsync(receiver, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no ciclo de processamento. Aguardando 5s antes de retry");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }

        await receiver.DisposeAsync();
    }

    private async Task ProcessarLoteAsync(
        ServiceBusReceiver receiver,
        CancellationToken ct)
    {
        // Recebe até TamanhoLote mensagens de uma vez
        var mensagens = await receiver.ReceiveMessagesAsync(
            TamanhoLote,
            TimeSpan.FromSeconds(TimeoutSegundos),
            ct);

        if (mensagens.Count == 0)
            return;

        logger.LogInformation("Recebidas {Count} mensagens do Service Bus", mensagens.Count);

        // Converte mensagens em entidades de domínio
        var registros = new List<RegistroApp>(mensagens.Count);
        var mensagensValidas = new List<ServiceBusReceivedMessage>(mensagens.Count);

        foreach (var msg in mensagens)
        {
            var dto = JsonSerializer.Deserialize<RegistroAppMessage>(msg.Body);
            if (dto is null)
            {
                logger.LogWarning("Mensagem {Id} com corpo inválido — será descartada", msg.MessageId);
                await receiver.DeadLetterMessageAsync(msg,
                    "InvalidBody",
                    "Não foi possível desserializar a mensagem",
                    ct);
                continue;
            }

            registros.Add(new RegistroApp
            {
                NomeProjeto = dto.NomeProjeto,
                Repositorio = dto.Repositorio,
                Linguagem = dto.Linguagem,
                Plataforma = dto.Plataforma,
                TipoAplicacao = dto.TipoAplicacao,
                VersaoFramework = dto.VersaoFramework,
                Legado = dto.Legado
            });

            mensagensValidas.Add(msg);
        }

        if (registros.Count == 0)
            return;

        // Persiste em massa usando BulkInsertOrUpdate
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BulkDbContext>();

        var bulkConfig = new BulkConfig
        {
            SetOutputIdentity = true,
            PreserveInsertOrder = true,
            UpdateByProperties =
            [
                nameof(RegistroApp.NomeProjeto),
                nameof(RegistroApp.Repositorio)
            ],
            PropertiesToExcludeOnUpdate =
            [
                nameof(RegistroApp.Id),
                nameof(RegistroApp.CriadoEm)
            ],
            TrackingEntities = false,
            BatchSize = 1000
        };

        await dbContext.BulkInsertOrUpdateAsync(registros, bulkConfig, cancellationToken: ct);

        logger.LogInformation("BulkInsertOrUpdate: {Count} registros persistidos", registros.Count);

        // Confirma todas as mensagens processadas com sucesso
        var completeTasks = mensagensValidas
            .Select(msg => receiver.CompleteMessageAsync(msg, ct));

        await Task.WhenAll(completeTasks);

        logger.LogInformation("Lote concluído: {Count} mensagens confirmadas", mensagensValidas.Count);
    }
}
