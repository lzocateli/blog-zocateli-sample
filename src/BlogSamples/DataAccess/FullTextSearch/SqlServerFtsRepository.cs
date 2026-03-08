// ==========================================================================
// Artigo: Full-Text Search API REST com C#
// URL: /posts/2025/full-text-search-api-rest-csharp-sqlserver-oracle-postgres/
// Repositório SQL Server — CONTAINS, CONTAINSTABLE, prefix wildcards
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace BlogSamples.DataAccess.FullTextSearch;

/// <summary>
/// Repositório com Full-Text Search para SQL Server.
/// Usa CONTAINS com wildcards de prefixo para busca parcial.
/// </summary>
public class ClienteRepositorioSqlServer(FtsDbContext db) : IClienteRepository
{
    public async Task<(IReadOnlyList<Cliente> itens, int total)> BuscarAsync(
        string? q, int pagina, int tamanho, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return await BuscarSemFiltroAsync(pagina, tamanho, ct);

        var termo = FormatarTermoSqlServer(q);
        var offset = (pagina - 1) * tamanho;

        // CONTAINS com prefixo — suporta busca parcial no início das palavras
        var sql = """
            SELECT Id, Nome, CpfCnpj, Email, Cidade
            FROM Clientes
            WHERE CONTAINS((Nome, Email, Cidade), {0})
            ORDER BY Nome
            OFFSET {1} ROWS FETCH NEXT {2} ROWS ONLY
            """;

        var countSql = """
            SELECT COUNT(*)
            FROM Clientes
            WHERE CONTAINS((Nome, Email, Cidade), {0})
            """;

        var itens = await db.Clientes
            .FromSqlRaw(sql, termo, offset, tamanho)
            .AsNoTracking()
            .ToListAsync(ct);

        var total = await db.Database
            .SqlQueryRaw<int>(countSql, termo)
            .FirstAsync(ct);

        return (itens, total);
    }

    private static string FormatarTermoSqlServer(string q)
    {
        // "João Si" → '"João*" AND "Si*"'
        var palavras = q.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => $"\"{EscaparSqlServer(p)}*\"");

        return string.Join(" AND ", palavras);
    }

    private static string EscaparSqlServer(string palavra)
        => palavra.Replace("\"", "\"\"").Replace("'", "''");

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

// --- CONTAINSTABLE com ranking por relevância ---
// var rankSql = """
//     SELECT c.Id, c.Nome, c.CpfCnpj, c.Email, c.Cidade
//     FROM Clientes c
//     INNER JOIN CONTAINSTABLE(Clientes, (Nome, Email, Cidade), {0}) AS ft
//         ON c.Id = ft.[KEY]
//     ORDER BY ft.[RANK] DESC, c.Nome
//     OFFSET {1} ROWS FETCH NEXT {2} ROWS ONLY
//     """;
