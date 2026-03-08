// ==========================================================================
// Artigo: Arquitetura de Software: GoF, Padrões e Microserviços
// URL: /posts/2025/arquitetura-software-gof-padroes-cloud-microservicos/
// Padrões Estruturais: Adapter, Decorator, Facade, Proxy
// ==========================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BlogSamples.DesignPatterns.Structural;

// -----------------------------------------------------------------------
// Adapter
// Adapta a interface de terceiro para a interface esperada pelo sistema
// -----------------------------------------------------------------------

public record DadosPagamento(string Token, decimal Valor, string Moeda);
public record ResultadoPagamento(bool Sucesso, string TransacaoId);

public interface IGatewayPagamento
{
    Task<ResultadoPagamento> ProcessarAsync(DadosPagamento dados);
}

public class StripeResult
{
    public string Status { get; set; } = "succeeded";
    public string Id { get; set; } = Guid.NewGuid().ToString();
}

public class StripeClient
{
    public StripeResult Charge(string token, long amountCents, string currency)
        => new();
}

public class StripeAdapter(StripeClient stripe) : IGatewayPagamento
{
    public async Task<ResultadoPagamento> ProcessarAsync(DadosPagamento dados)
    {
        var resultado = await Task.Run(() =>
            stripe.Charge(dados.Token, (long)(dados.Valor * 100), dados.Moeda));

        return new ResultadoPagamento(
            Sucesso: resultado.Status == "succeeded",
            TransacaoId: resultado.Id);
    }
}

// -----------------------------------------------------------------------
// Decorator
// Compor comportamentos sem explodir a hierarquia de herança
// -----------------------------------------------------------------------

public class PedidoEntity
{
    public int Id { get; set; }
    public string ClienteId { get; set; } = string.Empty;
}

public interface IRepositorioPedidos
{
    Task<PedidoEntity?> ObterAsync(int id);
    Task SalvarAsync(PedidoEntity pedido);
}

public interface ICache
{
    Task<T?> GetAsync<T>(string chave) where T : class;
    Task SetAsync<T>(string chave, T valor, TimeSpan expiracao) where T : class;
}

// Decorator 1: adiciona cache
public class PedidosComCache(IRepositorioPedidos inner, ICache cache)
    : IRepositorioPedidos
{
    public async Task<PedidoEntity?> ObterAsync(int id)
    {
        var cached = await cache.GetAsync<PedidoEntity>($"pedido:{id}");
        if (cached is not null) return cached;

        var pedido = await inner.ObterAsync(id);
        if (pedido is not null)
            await cache.SetAsync($"pedido:{id}", pedido, TimeSpan.FromMinutes(5));
        return pedido;
    }

    public Task SalvarAsync(PedidoEntity pedido) => inner.SalvarAsync(pedido);
}

// Decorator 2: adiciona logging
public class PedidosComLogging(IRepositorioPedidos inner, ILogger logger)
    : IRepositorioPedidos
{
    public async Task<PedidoEntity?> ObterAsync(int id)
    {
        logger.LogInformation("Buscando pedido {Id}", id);
        var result = await inner.ObterAsync(id);
        logger.LogInformation("Pedido {Id}: {Status}", id, result is null ? "não encontrado" : "encontrado");
        return result;
    }

    public Task SalvarAsync(PedidoEntity pedido) => inner.SalvarAsync(pedido);
}

// Composition root — empilha os decorators:
// IRepositorioPedidos repo =
//     new PedidosComLogging(
//         new PedidosComCache(
//             new PedidosEfCore(dbContext), redisCache),
//         logger);

// -----------------------------------------------------------------------
// Facade
// Um único ponto de entrada para o caso de uso
// -----------------------------------------------------------------------

public interface IEstoqueService
{
    Task ReservarItensAsync(int pedidoId);
    Task LiberarReservaAsync(int pedidoId);
}

public record ResultadoCobranca(bool Aprovada, string Motivo, string TransacaoId);

public interface IFinanceiroService
{
    Task<ResultadoCobranca> CobrarAsync(int pedidoId);
}

public record ResultadoEnvio(DateTime PrevisaoEntrega, string CodigoRastreio);

public interface ILogisticaService
{
    Task<ResultadoEnvio> AgendarEnvioAsync(int pedidoId);
}

public interface INotificacaoService
{
    Task NotificarClienteAsync(int pedidoId, DateTime previsaoEntrega);
}

public interface IAuditService
{
    Task RegistrarAsync(int pedidoId, string acao, string? detalhe = null);
}

public record ResultadoFinalizacao(bool Sucesso, string? TransacaoId, string? CodigoRastreio, string? Motivo)
{
    public static ResultadoFinalizacao FalhaNoPagamento(string motivo) =>
        new(false, null, null, motivo);
    public static ResultadoFinalizacao Concluido(string transacaoId, string codigoRastreio) =>
        new(true, transacaoId, codigoRastreio, null);
}

public class ServicoFinalizacaoPedido(
    IEstoqueService estoque,
    IFinanceiroService financeiro,
    ILogisticaService logistica,
    INotificacaoService notificacao,
    IAuditService auditoria)
{
    public async Task<ResultadoFinalizacao> FinalizarAsync(int pedidoId)
    {
        await estoque.ReservarItensAsync(pedidoId);
        var cobranca = await financeiro.CobrarAsync(pedidoId);

        if (!cobranca.Aprovada)
        {
            await estoque.LiberarReservaAsync(pedidoId);
            return ResultadoFinalizacao.FalhaNoPagamento(cobranca.Motivo);
        }

        var envio = await logistica.AgendarEnvioAsync(pedidoId);
        await notificacao.NotificarClienteAsync(pedidoId, envio.PrevisaoEntrega);
        await auditoria.RegistrarAsync(pedidoId, "PEDIDO_FINALIZADO");

        return ResultadoFinalizacao.Concluido(cobranca.TransacaoId, envio.CodigoRastreio);
    }
}

// -----------------------------------------------------------------------
// Proxy
// Proxy de proteção: verifica permissões antes de delegar
// -----------------------------------------------------------------------

public class RepositorioPedidosProtegido(
    IRepositorioPedidos inner,
    IHttpContextAccessor httpContext) : IRepositorioPedidos
{
    public async Task<PedidoEntity?> ObterAsync(int id)
    {
        var usuario = httpContext.HttpContext?.User;

        if (usuario?.IsInRole("Admin") != true)
        {
            var pedido = await inner.ObterAsync(id);
            var userId = usuario?.FindFirst("sub")?.Value;
            return pedido?.ClienteId.ToString() == userId ? pedido : null;
        }

        return await inner.ObterAsync(id);
    }

    public Task SalvarAsync(PedidoEntity pedido) => inner.SalvarAsync(pedido);
}
