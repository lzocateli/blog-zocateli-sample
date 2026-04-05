// -----------------------------------------------------------------------
// Artigo: Design de APIs REST: Verbos HTTP e Parameter Binding no ASP.NET Core
// Models e interfaces de suporte
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace BlogSamples.ApiDesign.Models;

public record PedidoDto(int Id, string ClienteId, decimal Valor, DateTime DataCriacao, string Status);
public record ItemPedidoDto(int Id, int ProdutoId, int Quantidade, decimal PrecoUnitario);

public record CriarPedidoRequest(
    [Required] int ClienteId,
    [Required, MinLength(1)] List<CriarItemPedidoRequest> Itens,
    string? Observacoes,
    DateOnly? DataEntregaPrevista);

public record CriarItemPedidoRequest(
    [Required] int ProdutoId,
    [Required, Range(1, 9999)] int Quantidade,
    decimal? DescontoUnitario);

public record SubstituirPedidoRequest(int Id, string ClienteId, string Status, List<CriarItemPedidoRequest> Itens);

// Merge Patch: campos nulos = não alterar
public record AtualizarPedidoRequest(
    string? Status,
    string? MotivoCancelamento,
    decimal? Desconto);

public record PagedResult<T>(IReadOnlyList<T> Dados, int PaginaAtual, int TamanhoPagina, long Total);

public enum DeletarResultado { Ok, NaoEncontrado, ConflitoDependencias }

public interface IPedidoService
{
    Task<PagedResult<PedidoDto>> ListarAsync(int pagina, int tamanhoPagina, string? status, CancellationToken ct);
    Task<PedidoDto?> ObterPorIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<ItemPedidoDto>> ListarItensAsync(int pedidoId, CancellationToken ct);
    Task<PedidoDto> CriarAsync(CriarPedidoRequest request, CancellationToken ct);
    Task<PedidoDto?> SubstituirAsync(int id, SubstituirPedidoRequest request, CancellationToken ct);
    Task<PedidoDto?> AtualizarParcialAsync(int id, AtualizarPedidoRequest request, CancellationToken ct);
    Task<DeletarResultado> RemoverAsync(int id, CancellationToken ct);
    Task<bool> RemoverItemAsync(int pedidoId, int itemId, CancellationToken ct);
}
