// ==========================================================================
// Artigo: Arquitetura de Software: GoF, Padrões e Microserviços
// URL: /posts/2025/arquitetura-software-gof-padroes-cloud-microservicos/
// Padrões Comportamentais: Strategy, Observer, Command, CQRS
// ==========================================================================

namespace BlogSamples.DesignPatterns.Behavioral;

// -----------------------------------------------------------------------
// Strategy
// Calcular frete com diferentes transportadoras
// -----------------------------------------------------------------------

public record Endereco(string Rua, string Cidade, string Estado);

public interface ICalculadorFrete
{
    decimal Calcular(Endereco origem, Endereco destino, decimal pesoKg);
}

public class FreteCorreios : ICalculadorFrete
{
    public decimal Calcular(Endereco origem, Endereco destino, decimal pesoKg)
        => pesoKg * 3.5m + 8.0m;
}

public class FreteTransportadora : ICalculadorFrete
{
    public decimal Calcular(Endereco origem, Endereco destino, decimal pesoKg)
        => pesoKg * 2.8m + 15.0m;
}

public class FreteRetiradaLoja : ICalculadorFrete
{
    public decimal Calcular(Endereco origem, Endereco destino, decimal pesoKg)
        => 0m;
}

// -----------------------------------------------------------------------
// Observer
// Base para sistemas de eventos, reactive programming, pub/sub
// -----------------------------------------------------------------------

public enum StatusPedido { Pendente, Aprovado, Enviado, Entregue, Cancelado }

public class PedidoObservado
{
    public int Id { get; set; }
    public StatusPedido Status { get; set; }
}

public interface IObservadorPedido
{
    Task AoAtualizarAsync(PedidoObservado pedido);
}

public interface IRepositorioPedidoObservado
{
    Task<PedidoObservado> ObterAsync(int id);
    Task SalvarAsync(PedidoObservado pedido);
}

public class ServicoPedidos(IRepositorioPedidoObservado repo)
{
    private readonly List<IObservadorPedido> _observadores = [];

    public void Inscrever(IObservadorPedido obs) => _observadores.Add(obs);
    public void Cancelar(IObservadorPedido obs) => _observadores.Remove(obs);

    public async Task AtualizarStatusAsync(int id, StatusPedido novoStatus)
    {
        var pedido = await repo.ObterAsync(id);
        pedido.Status = novoStatus;
        await repo.SalvarAsync(pedido);

        foreach (var obs in _observadores)
            await obs.AoAtualizarAsync(pedido);
    }
}

// Uso:
// servico.Inscrever(new NotificadorEmailCliente(emailService));
// servico.Inscrever(new AtualizadorEstoque(estoqueService));
// servico.Inscrever(new RegistradorAuditoria(auditService));

// -----------------------------------------------------------------------
// Command
// No coração de CQRS, event sourcing e undo/redo
// -----------------------------------------------------------------------

public interface IComando<TResultado> { }

public interface IHandlerComando<in TComando, TResultado>
    where TComando : IComando<TResultado>
{
    Task<TResultado> ExecutarAsync(TComando cmd);
}

public record ResultadoCancelamento(bool Sucesso, string? Motivo)
{
    public static ResultadoCancelamento Falha(string motivo) => new(false, motivo);
    public static ResultadoCancelamento Ok() => new(true, null);
}

public record CancelarPedidoComando(int PedidoId, string Motivo)
    : IComando<ResultadoCancelamento>;

public interface IEventBus
{
    Task PublicarAsync<T>(T evento);
}

public record PedidoCanceladoEvent(int PedidoId, string Motivo);

public class PedidoNaoEncontradoException(int pedidoId)
    : Exception($"Pedido {pedidoId} não encontrado");

public class PedidoCancelavel
{
    public int Id { get; set; }
    public StatusPedido Status { get; set; }

    public bool PodeCancelar() => Status is StatusPedido.Pendente or StatusPedido.Aprovado;
    public void Cancelar(string motivo) => Status = StatusPedido.Cancelado;
}

public interface IRepositorioPedidoCancelavel
{
    Task<PedidoCancelavel?> ObterAsync(int id);
    Task SalvarAsync(PedidoCancelavel pedido);
}

public class CancelarPedidoHandler(
    IRepositorioPedidoCancelavel repo,
    IEventBus eventBus,
    Structural.IAuditService auditoria)
    : IHandlerComando<CancelarPedidoComando, ResultadoCancelamento>
{
    public async Task<ResultadoCancelamento> ExecutarAsync(CancelarPedidoComando cmd)
    {
        var pedido = await repo.ObterAsync(cmd.PedidoId)
            ?? throw new PedidoNaoEncontradoException(cmd.PedidoId);

        if (!pedido.PodeCancelar())
            return ResultadoCancelamento.Falha("Status não permite cancelamento");

        pedido.Cancelar(cmd.Motivo);
        await repo.SalvarAsync(pedido);

        await eventBus.PublicarAsync(new PedidoCanceladoEvent(pedido.Id, cmd.Motivo));
        await auditoria.RegistrarAsync(pedido.Id, "PEDIDO_CANCELADO", cmd.Motivo);

        return ResultadoCancelamento.Ok();
    }
}

// -----------------------------------------------------------------------
// CQRS
// Segregar leitura de escrita permite otimizações independentes
// -----------------------------------------------------------------------

public interface ICommand<TResult> { }
public interface IQuery<TResult> { }

public record CriarPedidoCommand(int ClienteId, List<ItemDto> Itens)
    : ICommand<PedidoCriadoResult>;

public record ItemDto(int ProdutoId, int Quantidade);
public record PedidoCriadoResult(int PedidoId);

public record PedidoResumoDto(Guid Id, string ClienteId, decimal Valor, DateTime DataCriacao, string Status);

public record PagedResult<T>(IReadOnlyList<T> Dados, int PaginaAtual, int TotalPaginas);

public record ListarPedidosQuery(int ClienteId, int Pagina)
    : IQuery<PagedResult<PedidoResumoDto>>;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}
