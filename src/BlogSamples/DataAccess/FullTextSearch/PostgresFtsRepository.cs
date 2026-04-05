// ==========================================================================
// Artigo: Full-Text Search API REST com C#
// URL: /posts/2025/full-text-search-api-rest-csharp-sqlserver-oracle-postgres/
// Repositório PostgreSQL — tsvector/tsquery, GIN index, websearch_to_tsquery
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace BlogSamples.DataAccess.FullTextSearch;

/// <summary>
/// Repositório com Full-Text Search para PostgreSQL.
/// Usa coluna tsvector com índice GIN e websearch_to_tsquery.
/// </summary>
public class ClienteRepositorioPostgres(FtsDbContext db) : IClienteRepository
{
    public async Task<(IReadOnlyList<Cliente> itens, int total)> BuscarAsync(
        string? q, int pagina, int tamanho, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return await BuscarSemFiltroAsync(pagina, tamanho, ct);

        var offset = (pagina - 1) * tamanho;

        // websearch_to_tsquery: aceita entrada humana diretamente — mais seguro
        var sql = """
            SELECT id, nome, cpf_cnpj, email, cidade
            FROM clientes
            WHERE busca_fts @@ websearch_to_tsquery('portuguese', {0})
            ORDER BY ts_rank(busca_fts, websearch_to_tsquery('portuguese', {0})) DESC,
                     nome
            LIMIT {1} OFFSET {2}
            """;

        var countSql = """
            SELECT COUNT(*)
            FROM clientes
            WHERE busca_fts @@ websearch_to_tsquery('portuguese', {0})
            """;

        var itens = await db.Clientes
            .FromSqlRaw(sql, q.Trim(), tamanho, offset)
            .AsNoTracking()
            .ToListAsync(ct);

        var total = await db.Database
            .SqlQueryRaw<int>(countSql, q.Trim())
            .FirstAsync(ct);

        return (itens, total);
    }

    // --- Alternativa: busca com prefixo usando to_tsquery ---
    // private static string FormatarTermoPostgres(string q)
    // {
    //     // "João Si" → "'João':* & 'Si':*"
    //     var palavras = q.Trim()
    //         .Split(' ', StringSplitOptions.RemoveEmptyEntries)
    //         .Select(p => $"'{EscaparPostgres(p)}':*");
    //     return string.Join(" & ", palavras);
    // }
    // private static string EscaparPostgres(string p)
    //     => p.Replace("'", "''").Replace("\\", "\\\\");

    // --- Alternativa: FTS via LINQ com EF.Functions ---
    // var itens = await db.Clientes
    //     .Where(c => EF.Functions.ToTsVector("portuguese", c.Nome + " " + c.Email + " " + c.Cidade)
    //                     .Matches(EF.Functions.WebSearchToTsQuery("portuguese", q)))
    //     .OrderBy(c => c.Nome)
    //     .Skip(offset).Take(tamanho).AsNoTracking().ToListAsync(ct);

    private async Task<(IReadOnlyList<Cliente>, int)> BuscarSemFiltroAsync(
        int pagina, int tamanho, CancellationToken ct)
    {
        var offset = (pagina - 1) * tamanho;
        var itens = await db.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .Skip(offset)
            .Take(tamanho)
            .ToListAsync(ct);

        var total = await db.Clientes.CountAsync(ct);
        return (itens, total);
    }
}
