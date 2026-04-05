// ==========================================================================
// Artigo: Paginação em APIs REST com C# e EF Core 8
// URL: /posts/2026/paginacao-api-rest-csharp-efcore-sqlserver-oracle-postgres/
// Serviço de paginação Keyset (WHERE cursor)
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace BlogSamples.DataAccess.Pagination;

/// <summary>
/// Paginação Keyset — performance constante O(1) independente da página.
/// Ideal para grandes volumes de dados.
/// </summary>
public class PedidoKeysetService(Messaging.AppDbContext context)
{
    public async Task<KeysetResultado<PedidoDto>> ListarAsync(
        string?           token,
        int               limite = 20,
        CancellationToken ct     = default)
    {
        limite = Math.Clamp(limite, 1, 100);
        var cursor = KeysetCursor.Decode(token);

        var query = context.Pedidos
            .AsNoTracking()
            .Where(p =>
                cursor == null ||
                p.DataCriacao > cursor.UltimaData ||
                (p.DataCriacao == cursor.UltimaData &&
                 p.Id.CompareTo(cursor.UltimoId) > 0))
            .OrderBy(p => p.DataCriacao)
            .ThenBy(p => p.Id)
            .Select(p => new PedidoDto(p.Id, p.ClienteId, p.Valor, p.DataCriacao, p.Status));

        // Busca limite+1 para detectar se há próxima página
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
