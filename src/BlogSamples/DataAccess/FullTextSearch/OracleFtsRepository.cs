// ==========================================================================
// Artigo: Full-Text Search API REST com C#
// URL: /posts/2025/full-text-search-api-rest-csharp-sqlserver-oracle-postgres/
// Repositório Oracle — CONTAINS, SCORE, CTX Domain Index
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace BlogSamples.DataAccess.FullTextSearch;

/// <summary>
/// Repositório com Full-Text Search para Oracle (Oracle Text / CTX).
/// Usa CONTAINS com SCORE para ranking por relevância.
/// </summary>
public class ClienteRepositorioOracle(FtsDbContext db) : IClienteRepository
{
    public async Task<(IReadOnlyList<Cliente> itens, int total)> BuscarAsync(
        string? q, int pagina, int tamanho, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return await BuscarSemFiltroAsync(pagina, tamanho, ct);

        var termo = FormatarTermoOracle(q);
        var offset = (pagina - 1) * tamanho;

        // CONTAINS retorna score 0-100; > 0 significa que encontrou
        // SCORE(1) é o score da primeira chamada CONTAINS na query
        var sql = $"""
            SELECT Id, Nome, CpfCnpj, Email, Cidade
            FROM (
                SELECT c.Id, c.Nome, c.CpfCnpj, c.Email, c.Cidade,
                       SCORE(1) AS relevancia,
                       ROW_NUMBER() OVER (ORDER BY SCORE(1) DESC, c.Nome) AS rn
                FROM Clientes c
                WHERE CONTAINS(c.Nome, :termo, 1) > 0
            )
            WHERE rn > :offset AND rn <= :limite
            ORDER BY rn
            """;

        var countSql = """
            SELECT COUNT(*)
            FROM Clientes
            WHERE CONTAINS(Nome, :termo, 1) > 0
            """;

        // Nota: OracleParameter requer Oracle.EntityFrameworkCore
        // var itens = await db.Clientes
        //     .FromSqlRaw(sql, new OracleParameter("termo", termo),
        //                      new OracleParameter("offset", offset),
        //                      new OracleParameter("limite", offset + tamanho))
        //     .AsNoTracking()
        //     .ToListAsync(ct);

        // Exemplo simplificado sem parametrização Oracle-específica:
        var itens = await db.Clientes
            .FromSqlRaw(sql,
                new Microsoft.Data.SqlClient.SqlParameter("termo", termo),
                new Microsoft.Data.SqlClient.SqlParameter("offset", offset),
                new Microsoft.Data.SqlClient.SqlParameter("limite", offset + tamanho))
            .AsNoTracking()
            .ToListAsync(ct);

        var total = await db.Database
            .SqlQueryRaw<int>(countSql,
                new Microsoft.Data.SqlClient.SqlParameter("termo", termo))
            .FirstAsync(ct);

        return (itens, total);
    }

    private static string FormatarTermoOracle(string q)
    {
        // "João Si" → "João% AND Si%"
        var palavras = q.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => $"{EscaparOracle(p)}%");

        return string.Join(" AND ", palavras);
    }

    private static string EscaparOracle(string p)
        => p.Replace("'", "''").Replace("%", "\\%").Replace("_", "\\_");

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

// --- DbContext para Full-Text Search ---

public class FtsDbContext(DbContextOptions<FtsDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
}

// --- Program.cs snippet ---
// var banco = builder.Configuration["DatabaseProvider"]; // "sqlserver" | "postgres" | "oracle"
// builder.Services.AddScoped<IClienteService, ClienteService>();
// builder.Services.AddScoped<IClienteRepository>(sp =>
// {
//     var db = sp.GetRequiredService<FtsDbContext>();
//     return banco switch
//     {
//         "postgres" => new ClienteRepositorioPostgres(db),
//         "oracle"   => new ClienteRepositorioOracle(db),
//         _          => new ClienteRepositorioSqlServer(db)
//     };
// });
