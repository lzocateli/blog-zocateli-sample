// ==========================================================================
// Artigo: .NET Worker: Background Service para Alto Volume
// URL: /posts/2026/dotnet-worker-background-service-processamento-alto-volume/
// Timer Workers: BackgroundService, IHostedService, PeriodicTimer
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace BlogSamples.Workers;

/// <summary>
/// Worker básico com BackgroundService e loop com Task.Delay.
/// </summary>
public class MeuWorker(ILogger<MeuWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker executando em: {Time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

/// <summary>
/// IHostedService com Timer — callback em thread do ThreadPool.
/// </summary>
public class MeuServicoHosted : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(
            callback: ExecutarTarefa,
            state: null,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromMinutes(5));

        return Task.CompletedTask;
    }

    private void ExecutarTarefa(object? state)
    {
        // Lógica executada a cada 5 minutos
        // ⚠️ CUIDADO: este callback roda numa thread do ThreadPool
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}

/// <summary>
/// Worker com IServiceScopeFactory — resolve serviços Scoped corretamente.
/// </summary>
public class SincronizacaoWorker(
    ILogger<SincronizacaoWorker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var servico = scope.ServiceProvider
                    .GetRequiredService<ISincronizacaoService>();

                await servico.SincronizarAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erro na sincronização periódica");
            }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}

public interface ISincronizacaoService
{
    Task SincronizarAsync(CancellationToken ct);
}
