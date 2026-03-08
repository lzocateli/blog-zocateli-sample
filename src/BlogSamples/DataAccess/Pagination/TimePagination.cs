// ==========================================================================
// Artigo: Paginação em APIs REST com C# e EF Core 8
// URL: /posts/2026/paginacao-api-rest-csharp-efcore-sqlserver-oracle-postgres/
// Paginação temporal (Date range + keyset)
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace BlogSamples.DataAccess.Pagination;

/// <summary>
/// Paginação temporal — janela de tempo fixa com cursor interno.
/// Ideal para dashboards com filtro de data.
/// </summary>
public class PedidoTimeService(Messaging.AppDbContext context)
{
    public async Task<KeysetResultado<PedidoDto>> ListarPorJanelaAsync(
        TimePaginacaoRequest req,
        CancellationToken    ct = default)
    {
        var limite = Math.Clamp(req.Limite, 1, 200);

        var query = context.Pedidos
            .AsNoTracking()
            .Where(p => p.DataCriacao >= req.DataInicio &&
                        p.DataCriacao <  req.DataFim)
            .Where(p =>
                req.UltimaDataVista == null ||
                p.DataCriacao > req.UltimaDataVista ||
                (p.DataCriacao == req.UltimaDataVista &&
                 p.Id.CompareTo(req.UltimoIdVisto!.Value) > 0))
            .OrderBy(p => p.DataCriacao)
            .ThenBy(p => p.Id)
            .Select(p => new PedidoDto(p.Id, p.ClienteId, p.Valor, p.DataCriacao, p.Status));

        var dados = await query.Take(limite + 1).ToListAsync(ct);
        var temProxima = dados.Count > limite;
        if (temProxima) dados.RemoveAt(dados.Count - 1);

        string? proximoToken = null;
        if (temProxima && dados.Count > 0)
        {
            var ultimo = dados[^1];
            proximoToken = new KeysetCursor(ultimo.Id, ultimo.DataCriacao).Encode();
        }

        return new KeysetResultado<PedidoDto>(dados, temProxima, proximoToken);
    }
}
