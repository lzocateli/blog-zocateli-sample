// ==========================================================================
// Artigo: Paginação em APIs REST com C# e EF Core 8
// URL: /posts/2026/paginacao-api-rest-csharp-efcore-sqlserver-oracle-postgres/
// Serviço de paginação Offset (Skip/Take)
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace BlogSamples.DataAccess.Pagination;

/// <summary>
/// Paginação Offset — ideal para listas pequenas/médias com contagem total.
/// </summary>
public class PedidoOffsetService(Messaging.AppDbContext context)
{
    public async Task<PaginaResultado<PedidoDto>> ListarAsync(
        PaginacaoRequest req,
        CancellationToken ct = default)
    {
        var query = context.Pedidos
            .AsNoTracking()
            .OrderBy(p => p.DataCriacao)
            .ThenBy(p => p.Id);

        var total = await query.LongCountAsync(ct);

        var dados = await query
            .Skip(req.OffsetSeguro)
            .Take(req.TamanhoSeguro)
            .Select(p => new PedidoDto(p.Id, p.ClienteId, p.Valor, p.DataCriacao, p.Status))
            .ToListAsync(ct);

        var totalPaginas = (int)Math.Ceiling(total / (double)req.TamanhoSeguro);

        return new PaginaResultado<PedidoDto>(
            Dados:          dados,
            PaginaAtual:    req.Pagina,
            TamanhoPagina:  req.TamanhoSeguro,
            TotalRegistros: total,
            TotalPaginas:   totalPaginas,
            TemProxima:     req.Pagina < totalPaginas,
            TemAnterior:    req.Pagina > 1);
    }
}
