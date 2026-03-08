// ==========================================================================
// Artigo: Arquitetura de Software: GoF, Padrões e Microserviços
// URL: /posts/2025/arquitetura-software-gof-padroes-cloud-microservicos/
// Padrões Cloud: Circuit Breaker (Polly), Adapter anti-lock-in
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace BlogSamples.DesignPatterns.Cloud;

// -----------------------------------------------------------------------
// Circuit Breaker com Polly
// -----------------------------------------------------------------------
// var pipeline = new ResiliencePipelineBuilder()
//     .AddCircuitBreaker(new CircuitBreakerStrategyOptions
//     {
//         FailureRatio = 0.5,
//         SamplingDuration = TimeSpan.FromSeconds(30),
//         BreakDuration = TimeSpan.FromSeconds(15),
//         OnOpened = args =>
//         {
//             logger.LogWarning("Circuit breaker ABERTO para serviço de pagamentos");
//             return ValueTask.CompletedTask;
//         }
//     })
//     .Build();

// -----------------------------------------------------------------------
// Adapter como anti-lock-in
// Abstrair o cloud provider para poder trocar sem tocar no serviço
// -----------------------------------------------------------------------

public interface IArmazenamento
{
    Task SalvarAsync(string container, string id, object dados);
}

public interface IFilaMensagens
{
    Task PublicarAsync(string fila, object mensagem);
}

// ❌ Lock-in direto ao Azure:
// public class PedidoService
// {
//     private readonly BlobServiceClient _blob;
//     private readonly ServiceBusClient _bus;
// }

// ✅ Com abstração (Adapter + Facade):
public class PedidoServiceAbstraido(IArmazenamento storage, IFilaMensagens fila)
{
    public async Task ProcessarAsync(Structural.PedidoEntity pedido)
    {
        await storage.SalvarAsync("pedidos", pedido.Id.ToString(), pedido);
        await fila.PublicarAsync("pedidos", new { PedidoId = pedido.Id });
    }
}

// Implementações podem ser swapped sem tocar no serviço:
// services.AddSingleton<IArmazenamento, AzureBlobStorage>();
// services.AddSingleton<IArmazenamento, S3Storage>();
// services.AddSingleton<IArmazenamento, LocalFileSystem>(); // dev/test

// -----------------------------------------------------------------------
// Anti-padrões: Lei de Demeter, Overengineering, Singleton abuse
// -----------------------------------------------------------------------

// ❌ Viola Demeter — "train wreck code"
// var cidade = pedido.Cliente.Endereco.Cidade.Nome;
// ✅ Respeita Demeter:
// var cidade = pedido.ObterCidadeEntrega();

// ❌ Factory para uma única implementação que não vai mudar:
// public interface ICalculadorImposto { decimal Calcular(decimal valor); }
// public class CalculadorImpostoFactory { ... }
// ✅ Quando há apenas uma implementação:
// public static decimal CalcularImposto(decimal valor) => valor * 0.12m;

// ❌ Singleton como acesso global:
// var db = DatabaseSingleton.Instancia;
// ✅ Singleton gerenciado pelo contêiner de DI:
// services.AddSingleton<IDatabaseContext, DatabaseContext>();
