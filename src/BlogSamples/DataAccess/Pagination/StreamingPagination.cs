// ==========================================================================
// Artigo: Paginação em APIs REST com C# e EF Core 8
// URL: /posts/2026/paginacao-api-rest-csharp-efcore-sqlserver-oracle-postgres/
// Streaming: PostgreSQL Server-Side Cursor e EF Core IAsyncEnumerable
// ==========================================================================

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace BlogSamples.DataAccess.Pagination;

/// <summary>
/// Server-side cursor com PostgreSQL — DECLARE/FETCH.
/// Ideal para exportação de grandes volumes sem carregar em memória.
/// </summary>
public class PedidoCursorService(Messaging.AppDbContext context)
{
    public async IAsyncEnumerable<PedidoDto> StreamAsync(
        int blocoPorFetch = 500,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var transaction = await context.Database
            .BeginTransactionAsync(ct);

        await context.Database.ExecuteSqlRawAsync(
            "DECLARE pedidos_cursor NO SCROLL CURSOR FOR " +
            "SELECT p.\"Id\", p.\"ClienteId\", p.\"Valor\", p.\"DataCriacao\", p.\"Status\" " +
            "FROM \"Pedidos\" p ORDER BY p.\"DataCriacao\", p.\"Id\"",
            ct);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var bloco = await context.Database
                    .SqlQuery<PedidoDto>(
                        $"FETCH {blocoPorFetch} FROM pedidos_cursor")
                    .ToListAsync(ct);

                if (bloco.Count == 0) break;

                foreach (var item in bloco)
                    yield return item;

                if (bloco.Count < blocoPorFetch) break;
            }
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync(
                "CLOSE pedidos_cursor", CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }
    }
}

/// <summary>
/// Streaming com IAsyncEnumerable do EF Core 8.
/// Internamente usa DataReader que consome linha por linha.
/// </summary>
public class PedidoStreamService(Messaging.AppDbContext context)
{
    public async IAsyncEnumerable<PedidoDto> StreamComEfCoreAsync(
        DateTime? dataInicio = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var query = context.Pedidos
            .AsNoTracking()
            .Where(p => dataInicio == null || p.DataCriacao >= dataInicio)
            .OrderBy(p => p.DataCriacao)
            .ThenBy(p => p.Id)
            .Select(p => new PedidoDto(p.Id, p.ClienteId, p.Valor, p.DataCriacao, p.Status));

        await foreach (var item in query.AsAsyncEnumerable().WithCancellation(ct))
        {
            yield return item;
        }
    }
}
