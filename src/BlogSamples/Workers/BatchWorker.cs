// ==========================================================================
// Artigo: .NET Worker: Background Service para Alto Volume
// URL: /posts/2026/dotnet-worker-background-service-processamento-alto-volume/
// Batch Workers: processamento em lote com keyset e paralelismo controlado
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlogSamples.Workers;

public class WorkerConfig
{
    public int MaxParallelism { get; set; } = 4;
}

public enum StatusPedidoWorker { Pendente, Processado }
public enum StatusFila { Aguardando, Processando, Concluido }

public class PedidoWorker
{
    public long Id { get; set; }
    public StatusPedidoWorker Status { get; set; }
    public DateTimeOffset? ProcessadoEm { get; set; }
}

public class ItemFila
{
    public long Id { get; set; }
    public StatusFila Status { get; set; }
    public DateTime CriadoEm { get; set; }
}

public interface IProcessadorItem
{
    Task ProcessarAsync(ItemFila item, CancellationToken ct);
}

/// <summary>
/// Worker que processa registros pendentes em lote com paginação Keyset.
/// Performance constante independente do volume.
/// </summary>
public class ProcessamentoLoteWorker(
    ILogger<ProcessamentoLoteWorker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const int TamanhoPagina = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processados = await ProcessarLoteAsync(stoppingToken);

            if (processados == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task<int> ProcessarLoteAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Messaging.AppDbContext>();

        var pendentes = await dbContext.Pedidos
            .Where(p => p.Status == "Pendente")
            .OrderBy(p => p.Id)
            .Take(TamanhoPagina)
            .ToListAsync(ct);

        if (pendentes.Count == 0) return 0;

        foreach (var pedido in pendentes)
        {
            pedido.Status = "Processado";
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Processados {Count} pedidos", pendentes.Count);

        return pendentes.Count;
    }
}

/// <summary>
/// Worker com paralelismo controlado via SemaphoreSlim.
/// Cada item processa em seu próprio escopo de DI.
/// </summary>
public class ProcessamentoParaleloWorker(
    ILogger<ProcessamentoParaleloWorker> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerConfig> options) : BackgroundService
{
    private readonly int _maxParallelism = options.Value.MaxParallelism;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<Messaging.AppDbContext>();

            // Simulação: buscar itens pendentes
            var itens = await dbContext.Pedidos
                .Where(p => p.Status == "Pendente")
                .OrderBy(p => p.DataCriacao)
                .Take(100)
                .ToListAsync(stoppingToken);

            if (itens.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                continue;
            }

            // Processar com paralelismo controlado
            using var semaphore = new SemaphoreSlim(_maxParallelism);
            var tasks = itens.Select(async item =>
            {
                await semaphore.WaitAsync(stoppingToken);
                try
                {
                    using var itemScope = scopeFactory.CreateScope();
                    // Processar item individual em escopo dedicado
                    item.Status = "Processado";
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            await dbContext.SaveChangesAsync(stoppingToken);
            logger.LogInformation("Lote processado: {Count} itens", itens.Count);
        }
    }
}

/// <summary>
/// Worker de migração de dados — processa todo o dataset e encerra.
/// Usa paginação Keyset para performance constante.
/// </summary>
public class MigracaoWorker(
    ILogger<MigracaoWorker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const int TamanhoPagina = 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Guid ultimoId = Guid.Empty;
        var totalProcessados = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<Messaging.AppDbContext>();

            var lote = await dbContext.Pedidos
                .Where(p => p.Id.CompareTo(ultimoId) > 0)
                .OrderBy(p => p.Id)
                .Take(TamanhoPagina)
                .ToListAsync(stoppingToken);

            if (lote.Count == 0)
            {
                logger.LogInformation(
                    "Migração concluída. Total: {Total}", totalProcessados);
                return;
            }

            // Processar lote de migração
            totalProcessados += lote.Count;
            ultimoId = lote[^1].Id;

            logger.LogInformation(
                "Lote processado: {Count} | Total: {Total} | Último ID: {Id}",
                lote.Count, totalProcessados, ultimoId);
        }
    }
}
