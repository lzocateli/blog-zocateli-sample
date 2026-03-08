// ==========================================================================
// Artigo: .NET Worker: Background Service para Alto Volume
// URL: /posts/2026/dotnet-worker-background-service-processamento-alto-volume/
// Graceful Shutdown, DI patterns (Scoped vs Singleton), configuração
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlogSamples.Workers;

// -----------------------------------------------------------------------
// Graceful Shutdown com CancellationToken
// -----------------------------------------------------------------------

public interface IProcessadorRelatorio
{
    Task GerarRelatorioAsync(CancellationToken ct);
}

/// <summary>
/// Worker com tratamento correto de graceful shutdown.
/// O CancellationToken é propagado para TODAS as operações async.
/// </summary>
public class WorkerGraceful(
    ILogger<WorkerGraceful> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker iniciando...");

        stoppingToken.Register(() =>
            logger.LogWarning("Shutdown solicitado — finalizando operação atual"));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processador = scope.ServiceProvider
                    .GetRequiredService<IProcessadorRelatorio>();

                await processador.GerarRelatorioAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker finalizado de forma graciosa");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no processamento — tentando novamente em 30s");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}

// -----------------------------------------------------------------------
// Anti-padrões de DI em Workers
// -----------------------------------------------------------------------

// ❌ ERRADO — DbContext é Scoped, Worker é Singleton
// public class WorkerErrado(AppDbContext dbContext) : BackgroundService
// → InvalidOperationException: Cannot consume scoped service from singleton

// ✅ CORRETO — cria escopo manual via IServiceScopeFactory
// using (var scope = scopeFactory.CreateScope())
// {
//     var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//     await servico.ProcessarPendentesAsync(stoppingToken);
// }

// ⚠️ PERIGOSO — HttpClient é Transient, injetado em Singleton
// → vazamento de memória (connection pool esgotado) + DNS stale
// ✅ use IHttpClientFactory — Singleton-safe

// -----------------------------------------------------------------------
// Configuração de HostOptions para Workers
// -----------------------------------------------------------------------

// builder.Services.Configure<HostOptions>(options =>
// {
//     options.ShutdownTimeout = TimeSpan.FromMinutes(2); // tempo de graceful shutdown
//     options.BackgroundServiceExceptionBehavior =
//         BackgroundServiceExceptionBehavior.StopHost; // padrão no .NET 8
// });

// -----------------------------------------------------------------------
// Múltiplos Workers no mesmo processo
// -----------------------------------------------------------------------

// builder.Services.AddHostedService<ImportacaoWorker>();
// builder.Services.AddHostedService<NotificacaoWorker>();
// builder.Services.AddHostedService<LimpezaWorker>();
// builder.Services.AddHostedService<SincronizacaoWorker>();
